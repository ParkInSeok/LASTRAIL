namespace LastRail.Auth
{
    public enum SecureStorageWriteFailure { None, LogicalLimit, DiskFull, AccessDenied, Unavailable, Unknown }
    public interface ISecureStorageWriteDiagnostics
    {
        SecureStorageWriteFailure LastWriteFailure { get; }
    }
}
