namespace LastRail.Auth
{
    public enum GameApiRefreshAuthClientOutcome
    {
        Refreshed,
        RefreshTokenReused,
        Rejected,
        NetworkError,
        RateLimited,
        ServiceUnavailable,
        InvalidResponse,
        UnknownError,
        AuthRejected
    }
}
