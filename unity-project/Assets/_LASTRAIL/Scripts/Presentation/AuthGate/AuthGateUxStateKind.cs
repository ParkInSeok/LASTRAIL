namespace LastRail.Presentation.AuthGate
{
    public enum AuthGateUxStateKind
    {
        StartOptions = 1,
        GuestReady = 2,
        GoogleSignedIn = 3,
        GoogleLinked = 4,
        GoogleAlreadyLinked = 5,
        Restored = 6,
        AuthRequired = 7,
        ReLoginRequired = 8,
        AccountRecoveryRequired = 9,
        Cancelled = 10,
        ProviderError = 11,
        Rejected = 12,
        RetryableNetworkError = 13
    }
}
