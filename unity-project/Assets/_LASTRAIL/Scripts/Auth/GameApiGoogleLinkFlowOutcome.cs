namespace LastRail.Auth
{
    public enum GameApiGoogleLinkFlowOutcome
    {
        Linked,
        AlreadyLinked,
        Cancelled,
        ProviderError,
        Conflict,
        AuthRequired,
        Rejected,
        NetworkError
    }
}
