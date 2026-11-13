using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace LastRail.Resources
{
    // Login decoration reads cached bundles only before account admission.
    // Gameplay resources continue through PreparedContent and the approved server catalog.
    public sealed class PublicEntryTexture:MonoBehaviour
    {
        public const string Label="public_entry_ui";
        public const string Root="Assets/_LASTRAIL/Content/PublicEntry/";
        string key;Action<Texture2D> render;int generation;
        AsyncOperationHandle<Texture2D>? owned;
#if UNITY_EDITOR
        public static Func<string,AsyncOperationHandle<Texture2D>> EditorLoad;
#endif
        public static string Path(string key)
        {
            switch(key){case "login-background":case "primary":case "secondary":case "profile":return Root+key+".png";default:throw new ArgumentException("Unknown public entry artwork.",nameof(key));}
        }
        public void Bind(string value,Action<Texture2D> callback)
        {
            Path(value);Release();key=value;render=callback??throw new ArgumentNullException(nameof(callback));
            if(isActiveAndEnabled)Load();
        }
        void OnEnable(){if(key!=null)Load();}
        void OnDisable()=>Release();
        void OnDestroy(){Release();render=null;}
        void Release()
        {
            ++generation;render?.Invoke(null);
            if(owned.HasValue){var handle=owned.Value;owned=null;if(handle.IsDone)Addressables.Release(handle);else handle.Completed+=completed=>Addressables.Release(completed);}
        }
        async void Load()
        {
            int stamp=++generation;
            try
            {
#if UNITY_EDITOR
                if(EditorLoad==null&&PreparedContent.EditorAsset!=null){render(PreparedContent.EditorAsset(Path(key),typeof(Texture2D)) as Texture2D);return;}
                if(EditorLoad==null&&!await IsCachedAsync(CancellationToken.None)){if(stamp==generation)render(null);return;}
#else
                if(!await IsCachedAsync(CancellationToken.None)){if(stamp==generation)render(null);return;}
#endif
                if(stamp!=generation||!isActiveAndEnabled)return;
#if UNITY_EDITOR
                var handle=EditorLoad!=null?EditorLoad(Path(key)):Addressables.LoadAssetAsync<Texture2D>(Path(key));
#else
                var handle=Addressables.LoadAssetAsync<Texture2D>(Path(key));
#endif
                owned=handle;await handle.Task;
                if(stamp!=generation||!isActiveAndEnabled)return;
                if(handle.Status!=AsyncOperationStatus.Succeeded||handle.Result==null)throw new InvalidOperationException("Entry artwork unavailable.");
                render(handle.Result);
            }
            catch(Exception)
            {
                if(stamp!=generation)return;
                Release();Debug.LogWarning("Public login artwork unavailable; initial UI remains usable.");
            }
        }
        public static async Task<bool> IsCachedAsync(CancellationToken cancel)
        {
#if UNITY_EDITOR
            if(PreparedContent.EditorAsset!=null)return true;
#endif
            await PreparedContent.InitializePublicEntryAsync(cancel);
            var handle=Addressables.GetDownloadSizeAsync(Label);
            try
            {
                await handle.Task;cancel.ThrowIfCancellationRequested();
                if(handle.Status!=AsyncOperationStatus.Succeeded)throw new InvalidOperationException("Login cache inspection failed.");
                return handle.Result==0;
            }
            finally{Addressables.Release(handle);}
        }
    }
}
