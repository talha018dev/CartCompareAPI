# Containerization and Deployment Learning Plan

## Goal and working method

Learn containerization, build a CI/CD pipeline, publish CartCompareAPI for other people to use, add traffic/performance monitoring, and run controlled load tests. Complete the numbered steps in order. Each step has a **checkpoint**: do not move on until you can demonstrate it. Keep a short log of commands, decisions, measurements, and costs in your own notes or pull requests.

**Proposed learning stack:** Docker and Docker Compose locally; GitHub Actions for CI/CD; Azure Container Apps for the public API; managed PostgreSQL (Neon is a low-cost starting candidate); Azure's built-in logs/metrics first, then OpenTelemetry/Application Insights; k6 for load tests. Provider plans, quotas, supported .NET versions, and prices change: verify them before creating paid resources. The architecture is portable, so choosing another container host later does not invalidate the early steps.

This document is a plan, not a claim that these features already exist.

## Current repository baseline (2026-09-17)

- The project targets .NET 10, uses EF Core with PostgreSQL, and references Microsoft.Playwright.
- `Program.cs` calls `InitialiseDatabaseAsync()` on every API startup. That method migrates, initializes brands, imports bundled Shwapno data, and canonicalizes pending products. The intended replacement is an explicit ingestion workflow; startup must not ingest or canonicalize.
- The Playwright browser client launches Chromium with `Headless = false` and writes collected JSON under the application content directory. The intended workflow returns collected products in memory and imports them directly, without a runtime JSON handoff.
- `appsettings.json` contains a PostgreSQL credential. The deployed app must not use it; treat the exposed credential as compromised and rotate it.
- CORS is configured only for loopback origins in Development. CRUD write/delete endpoints appear to have no authentication.
- No Dockerfile or GitHub Actions workflow was found at this baseline.

## Target shape

```text
GitHub push / pull request
    -> GitHub Actions: build + test + container build
    -> image registry
    -> Azure Container Apps: public HTTPS API
    -> managed PostgreSQL

API stdout/stderr -> platform logs
API + platform metrics/traces -> monitoring
explicit ingestion request/job -> Playwright -> in-memory products -> import commit -> canonicalize
separate deployment task -> migrate
k6 -> isolated test deployment and database
```

Do not add a queue or dedicated worker merely to complete this plan. Introduce one later if ingestion needs scheduling, retries, or separate scaling.

## Step 1 - Establish a reproducible baseline

**Learn:** what the app requires to build, start, and serve a request.

- Record `dotnet --info`, then run `dotnet restore`, `dotnet build`, and `dotnet test` from the repository root.
- Run the API against a disposable local PostgreSQL database. Exercise at least one read endpoint and one write endpoint; record current behavior and test count.
- Inventory configuration, external services, startup side effects, files written at runtime, and endpoints that must not be publicly writable.
- Decide what data is disposable and what must be backed up before later migrations.

**Checkpoint:** A fresh local checkout can be built/tested, and its dependencies and startup side effects are documented.

## Step 2 - Secure configuration and public access

**Learn:** configuration precedence, secrets, and the difference between public reads and public writes.

- Rotate the PostgreSQL password currently present in `CartCompareAPI/appsettings.json`. Remove the credential from tracked configuration; use a local ignored file, user secrets, or environment variables for development. Check whether the old credential is present in Git history and assume it was exposed if the repository was shared. Do not rewrite shared Git history without coordinating with collaborators.
- Supply the production connection string through the hosting platform's secret store as `ConnectionStrings__DefaultConnection`. Do not bake secrets into the image or workflow YAML.
- Choose which endpoints may be public. Add authentication/authorization to mutating endpoints, or disable them in the public deployment until authentication is ready. Do not rely on CORS as access control.
- Configure an explicit production CORS origin only if a browser frontend needs to call the API. Do not use an unrestricted origin by default.
- Add basic request validation, safe error responses, and rate limiting for exposed endpoints.

**Checkpoint:** No live credential is tracked; anonymous users cannot create, edit, delete, or trigger browser ingestion unless intentionally authorized.

## Step 3 - Rebuild ingestion incrementally

**Learn:** method contracts, dependency injection, orchestration, transaction boundaries, HTTP errors, authorization, cancellation, and container-safe browser automation.

The desired result is:

```text
authorized POST request
    -> Playwright returns an in-memory product collection
    -> importer commits StoreProducts
    -> canonicalization runs after the commit
    -> endpoint returns a summary for every phase
```

Complete and test each substep separately. Do not implement the whole pipeline in one change.

### 3.1 Record current behavior

- Run the full tests and record the passing count.
- Call the current ingestion endpoint once in a disposable environment.
- Follow the code from `ShwapnoController` to `ShwapnoBrowserClient`, then separately inspect the startup call to `InitialiseDatabaseAsync`.
- Write down the current behavior: the endpoint writes JSON, while startup later reads JSON, imports it, and canonicalizes listings.

**Checkpoint:** You can explain the existing flow and identify every file involved without changing code.

### 3.2 Make Playwright return products in memory

- Change the browser method return type from `Task` to
  `Task<IReadOnlyCollection<ShwapnoProduct>>`.
- Keep the existing SKU-keyed dictionary and return `dictionary.Values.ToArray()` after collection finishes.
- Remove only the JSON serialization and file-writing block from the browser client.
- Initially keep the concrete `ShwapnoBrowserClient` dependency; introduce an interface only after you understand why a test substitute is useful.
- Add a cancellation token parameter and check it inside the scrolling loop.
- Ensure asynchronous response callbacks finish before returning. A useful design is to retain their tasks and await them; an `async void` event callback can otherwise still be running when the method returns.
- Log the page navigation HTTP status and the number of captured product API responses. Represent an unknown total as `int?`, not `int.MaxValue`.

**Checkpoint:** Calling the browser method returns products, creates no runtime JSON file, and gives a clear error if no product response was captured.

### 3.3 Learn and test headless behavior

- Add `Ingestion:Headless` configuration and read it in the browser client.
- Use visible mode locally first so you can observe location dialogs, bot challenges, or consent pages.
- Try headless mode separately and record whether Shwapno returns a different HTTP status or fails to call its product API.
- Do not mistake `0 of 2147483647` for a real count; log `unknown` until an API response supplies `TotalItems`.

**Checkpoint:** Both browser modes are intentional and a failed scrape explains whether navigation failed, no API response appeared, or collection stopped early.

### 3.4 Change the importer to accept the collection

- Change `ShwapnoDairyImporter.ImportAsync` to accept the returned
  `IReadOnlyCollection<ShwapnoProduct>` plus a cancellation token.
- Remove `ShwapnoJsonReader` from this live path. Keep JSON files only as optional test fixtures or an explicitly named offline-import feature.
- Validate that the collection is non-empty and each product has the required SKU and price before opening the transaction.
- Keep all mapping and database writes in the existing transaction.
- Pass the cancellation token to catalog initialization, EF queries, `SaveChangesAsync`, and `CommitAsync`.
- Update the importer test to construct a `ShwapnoProduct` and pass it directly. Do not involve Playwright in an importer unit test.

**Checkpoint:** The importer test proves an in-memory product becomes an unlinked `StoreProduct` and initial `PriceHistory` without reading a file.

### 3.5 Return an import summary

- Add a small immutable result such as `ShwapnoImportSummary` with `Received`, `Created`, and `Updated` counts.
- Count each outcome in the importer and return the summary only after the transaction commits.
- Test new and existing SKU paths and verify the counts.

**Checkpoint:** The caller can tell what the importer committed without querying the database again.

### 3.6 Finish the batch canonicalization service

- Complete `StoreProductCanonicalizationService`, which is currently a stub.
- Give it enough context to select the imported store and category.
- Query only pending listings (`ProductId == null`) in deterministic order.
- Call `IStoreProductCanonicalizer` for each listing and return matched, created, unresolved, and failed counts.
- Save at an intentional boundary. Ensure a product created earlier in a run can be found by a later equivalent listing.
- Add focused tests before registering the service in dependency injection.

**Checkpoint:** A direct service test canonicalizes pending listings and returns accurate counts.

### 3.7 Add the orchestration service without using the controller yet

- Create `ShwapnoIngestionOrchestrator` with one `IngestAsync` method.
- It must execute exactly: scrape -> import and commit -> canonicalize.
- Return `ShwapnoIngestionResult` containing separate scrape, import, and canonicalization summaries.
- Do not catch every exception initially. Let failures remain visible while learning the flow.
- Write tests with fake/stub collaborators proving:
  - import receives the products returned by scraping;
  - canonicalization starts after import succeeds;
  - canonicalization is not called when scraping or import fails.
- At this point, decide whether interfaces such as `IShwapnoProductSource`, `IShwapnoDairyImporter`, and `IShwapnoIngestionOrchestrator` improve the tests. Add only the interfaces that have a real substitute or alternative implementation.

**Checkpoint:** The orchestrator tests prove call order and failure behavior without launching a browser or using a real database.

### 3.8 Remove ingestion from application startup

- Remove JSON import and canonicalization from `DatabaseInitialization`.
- For now, leave migrations and idempotent reference/brand initialization if needed locally; plan to make migrations a deployment job before scaling to multiple replicas.
- Start the application twice and verify neither start scrapes, imports, nor canonicalizes anything.

**Checkpoint:** API startup is quick and repeatable and has no ingestion side effects.

### 3.9 Connect a minimal POST endpoint

- Change the ingestion action from `GET` to `POST` because it changes state.
- Inject the orchestrator, call `IngestAsync`, and return `Ok(result)`.
- Pass `HttpContext.RequestAborted` or the action cancellation token through the whole workflow.
- Keep the controller minimal at this stage; test one successful request before adding authorization or elaborate error mapping.

**Checkpoint:** One POST call produces the three summaries and the database changes occur in the intended order.

### 3.10 Add specific validation errors

- Introduce narrow exceptions or result types for conditions callers can correct, for example unsupported category and invalid scraped input.
- Avoid a broad `catch (ArgumentException)` that labels every lower-level validation failure as a category problem.
- Map unsupported category to `400`, invalid/incomplete source data to `422`, and unexpected failures to `500`.
- Always log unexpected exceptions with their stack trace. In production responses, return a trace ID rather than internal SQL, paths, or connection details.

**Checkpoint:** Deliberately cause each error and verify its status, response body, log entry, and trace ID.

### 3.11 Extract exception handling from the controller

- After the mappings work, create a specifically named
  `ShwapnoIngestionExceptionHandler` implementing `IExceptionHandler`.
- Make it return `false` for exceptions it does not own. Add a separate global fallback handler for unexpected application errors if needed.
- Register handlers with `AddExceptionHandler<T>()` and enable the middleware with `UseExceptionHandler()`.
- Remember that exception handlers are singletons: do not inject scoped services such as `AppDbContext` directly into them.
- Remove the equivalent `try/catch` blocks from the controller only after handler tests pass.

**Checkpoint:** The controller contains the success path while the same error responses still work centrally.

### 3.12 Authorize the ingestion endpoint

- Begin with a long random API key supplied through `Ingestion__ApiKey`, never a committed value.
- Read the caller's `X-Ingestion-Key` request header and compare it with
  `CryptographicOperations.FixedTimeEquals` over equal-length UTF-8 byte arrays.
- First implement and test the check where it is easiest to understand. Then move it into a narrowly named authorization filter so the controller stays focused.
- Return `503` when server-side key configuration is absent and `401` when the supplied credential is absent or wrong. Include trace IDs consistently without logging the key.
- Use HTTPS. Later, replace the custom key with a normal ASP.NET authentication scheme/policy if the application gains users or multiple administrative actions.

**Checkpoint:** Missing configuration disables ingestion, a wrong key cannot call the orchestrator, and a correct key can.

### 3.13 Prevent overlapping ingestion

- Add a concurrency guard for one process and return `409 Conflict` when an ingestion is already running.
- Use `try/finally` so the guard is always released.
- Understand the limitation: an in-memory `SemaphoreSlim` protects only one application replica. Before running multiple replicas, use a database/distributed lock or a job queue.
- Add a concurrency test.

**Checkpoint:** Two simultaneous requests cannot start two browser/import workflows in one process.

### 3.14 Decide how canonicalization failure is reported

- Preserve the committed import if canonicalization fails; do not pretend one transaction covers both phases.
- Choose and document one API contract: either return a partial-success result with canonicalization failure details, or return an error plus a job/run ID that allows inspection and retry.
- Log the exception and leave unresolved listings retryable.
- Add a test forcing canonicalization failure and verify imported rows remain.

**Checkpoint:** A canonicalization failure never erases a successful import and the caller can see exactly which phase failed.

### 3.15 Decide when the HTTP request is too long

- Measure real scrape/import/canonicalization duration.
- Keep synchronous `200 OK` only while it reliably fits client and hosting timeouts.
- If it becomes long-running, introduce a background job deliberately: POST returns `202 Accepted` plus a job ID/status URL, and a worker performs ingestion.
- Do not fake a queued implementation behind a contract that promises final summaries; change the contract to return a start/job result.

**Checkpoint:** The chosen synchronous or background contract accurately describes when work is complete.

### Step 3 completion checkpoint

Starting or restarting the API performs no ingestion or canonicalization. One authorized POST request passes Playwright results directly in memory, commits the import, then canonicalizes pending listings. Tests cover ordering, cancellation, validation, concurrency, and phase failure behavior.

## Step 4 - Add health checks and production-safe logging

**Learn:** liveness versus readiness and structured logs.

- Add `/health/live` (process works) and `/health/ready` (required dependencies are reachable, with a short timeout). Keep liveness independent of PostgreSQL so a brief database outage does not cause restart loops.
- Replace operational `Console.WriteLine` calls with `ILogger` and structured properties. Never log credentials, tokens, full connection strings, or sensitive response bodies.
- Emit logs to stdout/stderr. Do not depend on files inside a container for durable logs.
- Run a local failure exercise: stop PostgreSQL and verify readiness fails while liveness still responds.

**Checkpoint:** Health endpoints and useful structured startup/request/error logs work locally.

## Step 5 - Build and run the API container

**Learn:** image layers, build stages, ports, environment variables, and ephemeral filesystems.

- Add a multi-stage `Dockerfile` and `.dockerignore`; build with the .NET 10 SDK and run with an appropriate .NET 10 runtime base.
- If the image runs Playwright, install the matching Chromium browser and Linux dependencies. Test headless mode: the current `Headless = false` is not suitable for a typical headless cloud container. Keep browser installation out of the API image if only a separate ingestion job needs it.
- Do not require bundled Shwapno JSON data for live ingestion. Include only explicitly labeled fixture/offline-import files that are still intentionally used.
- Set a predictable HTTP listening port and pass configuration with environment variables. Do not publish a database port to the internet.
- Run as a non-root user where feasible. Inspect image size and scan the image for known vulnerabilities.
- Verify a live ingestion passes products in memory and creates no intermediate JSON file. If a future audit/debug export is required, design it as a separate optional object-storage feature rather than the ingestion handoff.

**Checkpoint:** `docker build` succeeds; `docker run` serves the API and health endpoint using an external PostgreSQL connection; no credentials are inside the image.

## Step 6 - Learn local multi-container operation

**Learn:** Compose networking, service names, volumes, and resource limits.

- Add a local-only Compose file with API and PostgreSQL services, a named PostgreSQL volume, health checks, and environment-variable configuration. Keep real passwords in an ignored local environment file.
- Verify the API connects to the Compose service name, not `localhost` inside its container.
- Recreate the API container and confirm database data persists. Then intentionally recreate a disposable database volume in a safe test environment to learn the difference. Never use that exercise against valuable data.
- Set trial CPU and memory limits. Observe `docker stats` while serving read requests and while running a browser job separately.

**Checkpoint:** One documented Compose command starts a usable local stack; a container restart does not erase the database.

## Step 7 - Build continuous integration (CI)

**Learn:** automated checks on every proposed change.

- Add a GitHub Actions workflow for pull requests and `main` pushes: checkout, .NET setup, restore, build, test, and Docker image build.
- If tests require PostgreSQL, use a disposable CI service container or Testcontainers. Keep test data isolated from production.
- Pin actions to maintained versions, grant minimal workflow permissions, and use dependency/image scanning when the basic workflow is stable.
- Deliberately submit a failing test in a temporary branch and verify CI blocks it; then remove the failure.

**Checkpoint:** CI passes on a clean commit and fails on a broken test or build. No cloud deployment occurs yet.

## Step 8 - Provision a managed test database

**Learn:** managed Postgres, TLS, connection strings, migrations, and backups.

- Compare current Neon and Azure PostgreSQL limits/costs; choose a region reasonably close to the API and expected users. Start with a **test** database, separate from local and eventual production data.
- Create a restricted database user and store the connection string as a secret. Confirm TLS requirements and connection-pooling behavior with Npgsql.
- Apply migrations through the explicit migration command from Step 3. Import only disposable seed data.
- Practice exporting and restoring a small database. Check the actual backup/restore guarantees of the selected plan; a free plan may not provide production-grade backups.
- Set cost/budget alerts before enabling any pay-as-you-go resources.

**Checkpoint:** The container can use the managed database and a documented backup/restore exercise succeeds.

## Step 9 - Deploy manually and verify public access

**Learn:** registry, image tags, ingress, revisions, and HTTPS.

- Verify current Azure Container Apps region availability, quotas, free grants, and charges. Create a resource group, container environment, image registry or supported image source, and Container App.
- Push an image tagged with its Git commit SHA. Configure secrets, memory/CPU, public ingress, target port, and health probes. Start with one known-small configuration and measure before adjusting it.
- Set minimum replicas to zero for cheap learning if cold starts are acceptable; otherwise choose one and review its idle cost. Cap maximum replicas to limit surprise load-test costs.
- Apply the migration once, then deploy the API. Verify HTTPS, a public read endpoint, health checks, logs, and database connectivity from outside your network.
- Check that unauthorized writes and ingestion cannot be triggered publicly. Add a custom domain only after the provider URL works.

**Checkpoint:** Another person can call a documented public read endpoint over HTTPS; a redeploy/rollback can be demonstrated; no production secret appears in logs or image history.

## Step 10 - Automate continuous deployment (CD)

**Learn:** promotion, identity federation, rollout verification, and rollback.

- Extend GitHub Actions so a successful protected-branch merge builds, tests, tags, and pushes the image, then updates the Container App.
- Prefer GitHub-to-Azure OpenID Connect/workload identity over a long-lived cloud password. Scope cloud permissions to this deployment.
- Keep migrations as an explicit pre-deployment or release job, with a reviewed rollback/restore plan for destructive schema changes. Do not blindly run migration from each replica.
- After deployment, call readiness and a representative read endpoint. If verification fails, stop promotion and use the previous known-good revision/image; remember that rolling back code does not automatically roll back database schema.
- Require approval before production deployment if you later add a staging environment or collaborators.

**Checkpoint:** A safe commit reaches the public URL without manual image-push/deploy commands, and you can identify exactly which commit is running.

## Step 11 - Add traffic analytics and observability

**Learn:** logs versus metrics versus traces versus product analytics.

- Begin with Container Apps log streaming and built-in CPU, memory, restart, replica, and network metrics. Set log retention/spend limits; telemetry can cost money even when compute is cheap.
- Instrument ASP.NET requests and outgoing database calls with OpenTelemetry. Export to Application Insights/Azure Monitor or another compatible backend. Do not duplicate all logs into multiple paid systems without a reason.
- Create a dashboard for request rate, status-code/error rate, p50/p95/p99 latency, CPU, memory, replica count, database connections/slow queries, and import outcomes.
- Add a few explicitly defined business events (for example product searches or comparisons) only after deciding what questions you want answered. Avoid personal data in event dimensions.
- Add basic alerts for sustained errors, failed readiness, restarts, and resource saturation. Trigger one safe test alert to verify delivery.

**Checkpoint:** You can trace a test request, find its logs, see traffic/latency graphs, and receive an alert for a controlled failure.

## Step 12 - Establish a performance baseline

**Learn:** percentiles, bottlenecks, and capacity measurement.

- Create a staging/test deployment with its **own** database and representative, non-sensitive data. Keep its configuration and resource limits recorded.
- Write k6 scripts for real read-heavy user flows; add write flows only against disposable test data. Measure browser ingestion as a separate workload if it becomes part of the system.
- Run a tiny smoke test first. Then run a moderate fixed load and record requests/second, p50/p95/p99 latency, error rate, CPU, peak memory, database connections, and replica count.
- Repeat a test under the same conditions. Investigate query performance, indexes, connection pooling, and allocations before assuming the answer is more CPU/RAM.
- Derive a starting resource size from **observed peak usage plus headroom**, not from code size alone. Record the memory limit at which the process is killed and the traffic level where latency/errors become unacceptable.

**Checkpoint:** A reproducible baseline report includes workload, environment size, dataset, results, and an initial capacity estimate.

## Step 13 - Stress, spike, and soak test safely

**Learn:** failure behavior and scaling limits.

- Obtain permission for every target you test; test only your own staging environment and database. Confirm provider load-testing rules, quotas, rate limits, maximum replicas, and budget alerts first.
- Increase load in stages to find the first constraint; stop when errors, latency, or spending cross the thresholds you set in advance.
- Run a short spike test and a longer steady/soak test. Watch the API, PostgreSQL, telemetry volume, and load-generator machine simultaneously.
- Compare minimum replicas zero versus one to understand cold starts. Keep cold-start measurements separate from warm-request latency.
- Write down the first bottleneck, one change made, and a before/after retest. Do not present virtual-user count alone as capacity; include achieved requests/second and error rate.

**Checkpoint:** A report names the breaking point, limiting component, observed costs, and a justified next configuration or code change.

## Step 14 - Operate and evolve

**Learn:** keeping a deployment healthy after the first release.

- Document how to rotate secrets, restore the database, roll back an image, inspect logs, and respond to an unhealthy deployment.
- Rebuild images regularly for .NET/browser/security updates; rerun CI and smoke tests after dependency upgrades.
- Review monthly spend, usage quotas, storage growth, backup success, and alert noise.
- If ingestion becomes scheduled or resource-heavy, split it into a separate container job/worker. Add a queue only when retries, backpressure, or parallel workers require it. Give the worker its own CPU/memory limits and failure alerts.
- Consider moving from free-tier services to paid plans when reliability, backups, traffic, or retention requirements exceed their limits.

**Checkpoint:** Someone following the runbook can diagnose an outage and restore service without relying on undocumented knowledge.

## Suggested evidence to keep

For each step, save: the commit or pull request, one screenshot or command output proving the checkpoint, one thing learned, one problem encountered, and any recurring cost. Never include secrets in screenshots or notes.

## Primary references to check while implementing

- [Azure Container Apps overview](https://learn.microsoft.com/en-us/azure/container-apps/overview) and [observability](https://learn.microsoft.com/en-us/azure/container-apps/observability)
- [Azure Container Apps pricing](https://azure.microsoft.com/en-us/pricing/details/container-apps/) and [Neon pricing](https://neon.com/pricing) (recheck before provisioning)
- [GitHub Actions documentation](https://docs.github.com/en/actions) and [GitHub OIDC with Azure](https://docs.github.com/en/actions/how-tos/security-for-github-actions/security-hardening-your-deployments/configuring-openid-connect-in-azure)
- [Playwright for .NET Docker guidance](https://playwright.dev/dotnet/docs/docker)
- [ASP.NET Core health checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [Grafana k6 API load-testing guide](https://grafana.com/docs/k6/latest/testing-guides/api-load-testing/)
