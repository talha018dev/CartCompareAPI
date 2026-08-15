namespace CartCompareAPI.Features.Shared;

public sealed record CrudResult(bool Found, bool IsConflict, string? Error)
{
    public static CrudResult Success() => new(true, false, null);
    public static CrudResult NotFound() => new(false, false, null);
    public static CrudResult Conflict(string error) => new(true, true, error);
    public static CrudResult Invalid(string error) => new(true, false, error);
}

public sealed record CrudResult<T>(T? Value, bool Found, bool IsConflict, string? Error)
{
    public static CrudResult<T> Success(T value) => new(value, true, false, null);
    public static CrudResult<T> Invalid(string error) => new(default, true, false, error);
}
