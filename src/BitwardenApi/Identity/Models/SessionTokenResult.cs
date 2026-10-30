namespace BitwardenApi.Identity.Models;


public abstract record SessionTokenResult<T>
{
    private SessionTokenResult() { }

    public sealed record Success(T Value) : SessionTokenResult<T>;
    public sealed record Rejected(SessionTokenRejection Error) : SessionTokenResult<T>;
}

