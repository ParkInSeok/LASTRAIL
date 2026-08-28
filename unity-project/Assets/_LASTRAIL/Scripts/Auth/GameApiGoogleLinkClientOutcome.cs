namespace LastRail.Auth
{
    public enum GameApiGoogleLinkClientOutcome
    {
        Linked,
        AlreadyLinked,
        Conflict,
        AuthRequired,
        Rejected,
        NetworkError
    }
}
