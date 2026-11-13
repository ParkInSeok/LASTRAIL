using LastRail.Auth;
using LastRail.Submissions;

namespace LastRail.Run
{
    public sealed class AndroidEncryptedRunFilePort : IAndroidKeystoreNativeStoragePort
    {
        private readonly AndroidEncryptedReceiptFilePort file;
        public AndroidEncryptedRunFilePort(IAndroidKeystoreNativeStoragePort crypto, string path)
        { file = new AndroidEncryptedReceiptFilePort(crypto, path, "lastrail_run_operations_v1:"); }
        public bool IsAndroidRuntime => file.IsAndroidRuntime;
        public bool IsKeystoreAvailable => file.IsKeystoreAvailable;
        public bool UsedPlayerPrefsFallback => file.UsedPlayerPrefsFallback;
        public string ProviderName => "android_keystore_run_file_v1";
        public AndroidKeystoreEncryptedTokenPayload Encrypt(string value) => file.Encrypt(value);
        public string Decrypt(AndroidKeystoreEncryptedTokenPayload value) => file.Decrypt(value);
        public AndroidKeystoreEncryptedTokenPayload Load() => file.Load();
        public bool Save(AndroidKeystoreEncryptedTokenPayload value) => file.Save(value);
        public bool Clear() => file.Clear();
    }
}
