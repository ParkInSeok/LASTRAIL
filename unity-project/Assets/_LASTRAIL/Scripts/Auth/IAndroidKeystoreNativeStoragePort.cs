namespace LastRail.Auth
{
    public interface IAndroidKeystoreNativeStoragePort
    {
        bool IsAndroidRuntime { get; }
        bool IsKeystoreAvailable { get; }
        string ProviderName { get; }
        bool UsedPlayerPrefsFallback { get; }

        AndroidKeystoreEncryptedTokenPayload Encrypt(string plaintext);
        string Decrypt(AndroidKeystoreEncryptedTokenPayload encrypted);
        bool Save(AndroidKeystoreEncryptedTokenPayload encrypted);
        AndroidKeystoreEncryptedTokenPayload Load();
        bool Clear();
    }
}
