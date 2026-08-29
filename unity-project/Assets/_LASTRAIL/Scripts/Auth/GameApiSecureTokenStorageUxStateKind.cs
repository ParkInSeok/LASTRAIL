namespace LastRail.Auth
{
    public enum GameApiSecureTokenStorageUxStateKind
    {
        Ready = 1,
        StorageUnavailable = 2,
        SaveFailed = 3,
        RefreshWriteFailed = 4,
        Cleared = 5
    }
}
