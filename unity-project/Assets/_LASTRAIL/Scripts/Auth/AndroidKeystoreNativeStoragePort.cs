using System;
using System.Text;
using UnityEngine;

namespace LastRail.Auth
{
    public sealed class AndroidKeystoreNativeStoragePort : IAndroidKeystoreNativeStoragePort
    {
        private const string Provider = "android_keystore_first_party_bridge";
        private const string KeyAlias = "lastrail_game_api_auth_tokens_v1";
        private const string PreferencesName = "lastrail_secure_auth_tokens";
        private const string PayloadKey = "auth_payload_v1";

        public bool IsAndroidRuntime
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public bool IsKeystoreAvailable
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    EnsureSecretKey();
                    return true;
                }
                catch
                {
                    return false;
                }
#else
                return false;
#endif
            }
        }

        public string ProviderName => Provider;
        public bool UsedPlayerPrefsFallback => false;

        public AndroidKeystoreEncryptedTokenPayload Encrypt(string plaintext)
        {
            if (string.IsNullOrWhiteSpace(plaintext))
            {
                throw new ArgumentException("Value cannot be empty.", nameof(plaintext));
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            var secretKey = EnsureSecretKey();
            using (var cipherClass = new AndroidJavaClass("javax.crypto.Cipher"))
            using (var cipher = cipherClass.CallStatic<AndroidJavaObject>("getInstance", "AES/GCM/NoPadding"))
            {
                cipher.Call("init", 1, secretKey);
                var bytes = Encoding.UTF8.GetBytes(plaintext);
                var encrypted = cipher.Call<byte[]>("doFinal", bytes);
                var iv = cipher.Call<byte[]>("getIV");
                return new AndroidKeystoreEncryptedTokenPayload(
                    Convert.ToBase64String(encrypted),
                    Convert.ToBase64String(iv));
            }
#else
            throw new PlatformNotSupportedException("Android Keystore encryption requires an Android device runtime.");
#endif
        }

        public string Decrypt(AndroidKeystoreEncryptedTokenPayload encrypted)
        {
            if (encrypted == null)
            {
                return null;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            var secretKey = EnsureSecretKey();
            using (var spec = new AndroidJavaObject(
                       "javax.crypto.spec.GCMParameterSpec",
                       128,
                       Convert.FromBase64String(encrypted.IvBase64)))
            using (var cipherClass = new AndroidJavaClass("javax.crypto.Cipher"))
            using (var cipher = cipherClass.CallStatic<AndroidJavaObject>("getInstance", "AES/GCM/NoPadding"))
            {
                cipher.Call("init", 2, secretKey, spec);
                var decrypted = cipher.Call<byte[]>("doFinal", Convert.FromBase64String(encrypted.CipherTextBase64));
                return Encoding.UTF8.GetString(decrypted);
            }
#else
            throw new PlatformNotSupportedException("Android Keystore decryption requires an Android device runtime.");
#endif
        }

        public bool Save(AndroidKeystoreEncryptedTokenPayload encrypted)
        {
            if (encrypted == null)
            {
                return false;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            return CommitPreference(PayloadKey, AndroidKeystoreEncryptedTokenPayloadJsonAdapter.ToJson(encrypted));
#else
            return false;
#endif
        }

        public AndroidKeystoreEncryptedTokenPayload Load()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var json = ReadPreference(PayloadKey);
            if (json != null && string.IsNullOrWhiteSpace(json)) throw new FormatException("Invalid encrypted token payload.");
            return AndroidKeystoreEncryptedTokenPayloadJsonAdapter.FromJson(json);
#else
            return null;
#endif
        }

        public bool Clear()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return ClearPreference(PayloadKey);
#else
            return false;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject EnsureSecretKey()
        {
            using (var keyStoreClass = new AndroidJavaClass("java.security.KeyStore"))
            using (var keyStore = keyStoreClass.CallStatic<AndroidJavaObject>("getInstance", "AndroidKeyStore"))
            {
                keyStore.Call("load", null);
                if (!keyStore.Call<bool>("containsAlias", KeyAlias))
                {
                    GenerateSecretKey();
                }

                return keyStore.Call<AndroidJavaObject>("getKey", KeyAlias, null);
            }
        }

        private static void GenerateSecretKey()
        {
            using (var keyGeneratorClass = new AndroidJavaClass("javax.crypto.KeyGenerator"))
            using (var keyGenerator = keyGeneratorClass.CallStatic<AndroidJavaObject>("getInstance", "AES", "AndroidKeyStore"))
            using (var builder = new AndroidJavaObject(
                       "android.security.keystore.KeyGenParameterSpec$Builder",
                       KeyAlias,
                       3))
            {
                builder.Call<AndroidJavaObject>("setBlockModes", new[] { "GCM" });
                builder.Call<AndroidJavaObject>("setEncryptionPaddings", new[] { "NoPadding" });
                builder.Call<AndroidJavaObject>("setRandomizedEncryptionRequired", true);
                keyGenerator.Call("init", builder.Call<AndroidJavaObject>("build"));
                keyGenerator.Call<AndroidJavaObject>("generateKey");
            }
        }

        private static bool CommitPreference(string key, string value)
        {
            using (var preferences = GetPreferences())
            using (var editor = preferences.Call<AndroidJavaObject>("edit"))
            {
                editor.Call<AndroidJavaObject>("putString", key, value);
                return editor.Call<bool>("commit");
            }
        }

        private static string ReadPreference(string key)
        {
            using (var preferences = GetPreferences())
            {
                return preferences.Call<string>("getString", key, null);
            }
        }

        private static bool ClearPreference(string key)
        {
            using (var preferences = GetPreferences())
            using (var editor = preferences.Call<AndroidJavaObject>("edit"))
            {
                editor.Call<AndroidJavaObject>("remove", key);
                return editor.Call<bool>("commit");
            }
        }

        private static AndroidJavaObject GetPreferences()
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                return activity.Call<AndroidJavaObject>("getSharedPreferences", PreferencesName, 0);
            }
        }
#endif
    }
}
