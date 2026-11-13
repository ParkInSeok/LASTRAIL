namespace LastRail.Auth
{
    public enum GameApiGoogleAuthFlowOutcome
    {
        SignedIn,
        Cancelled,
        ProviderError,
        Conflict,
        Rejected,
        NetworkError
    }
}
