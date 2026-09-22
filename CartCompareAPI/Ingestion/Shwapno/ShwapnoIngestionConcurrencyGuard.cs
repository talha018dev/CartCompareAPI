using System;

namespace CartCompareAPI.Ingestion.Shwapno;

public class ShwapnoIngestionConcurrencyGuard : IDisposable
{


    private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1,1);

    public Task<bool> TryAcquireAsync(CancellationToken cancellationToken)
    {
        return semaphore.WaitAsync(0, cancellationToken);
    }

    public void Release()
    {
        semaphore.Release();
    }

    public void Dispose()
    {
        semaphore.Dispose();
    }
}
