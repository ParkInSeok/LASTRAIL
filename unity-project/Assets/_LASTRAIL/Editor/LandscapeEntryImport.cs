#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using LastRail.Resources;
using LastRail.Presentation.MissionShell;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace LastRail.Editor
{
    public static class LandscapeEntryImport
    {
        const string Output="../QA/ResourceIntake-20261110/connection/landscape-entry-v01/";
        public static void Run()
        {
            AssetDatabase.Refresh();
            var settings=AddressableAssetSettingsDefaultObject.Settings;
            RegisterPublicEntry(settings);
            var startupImporter=(TextureImporter)AssetImporter.GetAtPath("Assets/_LASTRAIL/Resources/StartupVideo/landscape-first-frame-v01.png");
            startupImporter.npotScale=TextureImporterNPOTScale.None;startupImporter.SaveAndReimport();
            var group=settings.FindGroup("remote_resources")??throw new InvalidOperationException("Remote group missing.");
            foreach(var key in new[]{"login-map-detail-v02","login-route-emissive-mask-v01","SharedRailwayPath","RailwayNode"})
            {
                string extension=key.StartsWith("login-")?".png":".shader";
                string old="Assets/_LASTRAIL/Resources/StartupMotion/"+key+extension,path="Assets/_LASTRAIL/Content/RemoteResources/StartupMotion/"+key+extension;
                if(!File.Exists(path)){var error=AssetDatabase.MoveAsset(old,path);if(!string.IsNullOrEmpty(error))throw new IOException(error);}
                var entry=settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path),group);entry.address=old;entry.SetLabel("remote_resources",true,true);entry.SetLabel(AddressableContentBackend.RequiredLabel,true,true);
            }
            foreach(var key in new[]{"cabin-landscape-v01","icons-landscape-v01","frame-landscape-v01"})
            {
                string path="Assets/_LASTRAIL/Content/RemoteResources/LobbyDeck/"+key+".png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.npotScale=TextureImporterNPOTScale.None;importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;
                importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=4096;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
                var entry=settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path),group);
                entry.address=path;entry.SetLabel("remote_resources",true,true);entry.SetLabel(AddressableContentBackend.RequiredLabel,true,true);
            }
            EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
            CaptureLobby();
            CaptureEntry(false);CaptureEntry(true);
        }
        public static void RetireSuperseded()
        {
            var settings=AddressableAssetSettingsDefaultObject.Settings;
            var paths=new[]{
                "Assets/_LASTRAIL/Content/RemoteResources/LobbyDeck/cabin.png",
                "Assets/_LASTRAIL/Content/RemoteResources/LobbyDeck/landscape.png",
                "Assets/_LASTRAIL/Content/RemoteResources/LobbyDeck/target-atlas.png",
                "Assets/_LASTRAIL/Resources/StartupVideo/flow-first-frame.jpg",
                "Assets/StreamingAssets/Startup/lastrail-opening-flow-4s-2x-v01.mp4",
                "Assets/_LASTRAIL/Resources/StartupMotion/login-cabin-frame-v03.png",
                "Assets/_LASTRAIL/Resources/StartupMotion/login-hanging-props-v01.png",
                "Assets/_LASTRAIL/Resources/StartupMotion/login-illumination-mask-v01.png",
                "Assets/_LASTRAIL/Resources/StartupMotion/login-outside-far-loop-v02.png",
                "Assets/_LASTRAIL/Resources/StartupMotion/login-outside-near-loop-v01.png"
            };
            var consumers=AssetDatabase.FindAssets("t:Scene t:Prefab t:Material t:ScriptableObject",new[]{"Assets/_LASTRAIL"})
                .Select(AssetDatabase.GUIDToAssetPath).Where(p=>!p.Contains("/Editor/")&&!p.Contains("/Tests/")).ToArray();
            foreach(var consumer in consumers)
            {
                var references=AssetDatabase.GetDependencies(consumer,true).Intersect(paths).ToArray();
                if(references.Length>0)throw new InvalidOperationException("Superseded artwork is still referenced by "+consumer+": "+string.Join(",",references));
            }
            var journal=new Newtonsoft.Json.Linq.JArray();
            foreach(var path in paths)
            {
                if(!File.Exists(path))throw new FileNotFoundException("Retirement must run once against reviewed source",path);
                journal.Add(new Newtonsoft.Json.Linq.JObject{["path"]=path,["guid"]=AssetDatabase.AssetPathToGUID(path),["sha256"]=Digest(path),["metaSha256"]=Digest(path+".meta")});
            }
            File.WriteAllText(Output+"retired-runtime-assets.json",journal.ToString());
            foreach(var path in paths)
            {
                settings.RemoveAssetEntry(AssetDatabase.AssetPathToGUID(path));
                if(!AssetDatabase.DeleteAsset(path))throw new IOException("Failed to retire "+path);
            }
            EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
        }
        static string Digest(string path)
        {
            using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();
        }
        static void CaptureEntry(bool startup)
        {
            var previous=PreparedContent.EditorAsset;PreparedContent.EditorAsset=(path,type)=>AssetDatabase.LoadAssetAtPath(path,type);
            var root=new GameObject("Entry review",typeof(RectTransform));var cameraNode=new GameObject("Entry camera",typeof(Camera));
            var camera=cameraNode.GetComponent<Camera>();var target=new RenderTexture(1600,900,24);var old=RenderTexture.active;
            try
            {
                var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                Canvas canvas;
                if(startup)
                {
                    LastRail.Presentation.Startup.StartupIntroOverlay.ResetEditorCaptureJourney();
                    var overlay=LastRail.Presentation.Startup.StartupIntroOverlay.Create(root.transform,font,()=>false,()=>ContentStage.Config,()=>{});
                    var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                    overlay.GetType().GetField("presentationSeconds",flags).SetValue(overlay,1f);
                    overlay.GetType().GetField("lastProgressAt",flags).SetValue(overlay,Time.realtimeSinceStartupAsDouble);
                    overlay.GetType().GetMethod("Update",flags).Invoke(overlay,null);canvas=overlay.GetComponent<Canvas>();
                }
                else{var panel=root.AddComponent<PlayerEntryPanel>();panel.Initialize(font);panel.ShowLogin(true,false);canvas=panel.GetComponent<Canvas>();}
                camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.targetTexture=target;
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);
                try{pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();File.WriteAllBytes(Output+(startup?"startup":"login")+"-independent-review.png",pixels.EncodeToPNG());}finally{Object.DestroyImmediate(pixels);}
            }
            finally{PreparedContent.EditorAsset=previous;RenderTexture.active=old;Object.DestroyImmediate(root);Object.DestroyImmediate(cameraNode);target.Release();Object.DestroyImmediate(target);LastRail.Presentation.Startup.StartupIntroOverlay.ResetEditorCaptureJourney();}
        }
        static void RegisterPublicEntry(AddressableAssetSettings settings)
        {
            var group=settings.FindGroup(PublicEntryTexture.Label)??settings.CreateGroup(PublicEntryTexture.Label,false,false,false,null,typeof(BundledAssetGroupSchema),typeof(ContentUpdateGroupSchema));
            var schema=group.GetSchema<BundledAssetGroupSchema>();schema.BuildPath.SetVariableByName(settings,AddressableAssetSettings.kRemoteBuildPath);schema.LoadPath.SetVariableByName(settings,AddressableAssetSettings.kRemoteLoadPath);
            schema.BundleMode=BundledAssetGroupSchema.BundlePackingMode.PackTogether;schema.UseAssetBundleCache=true;schema.UseAssetBundleCrc=true;schema.Timeout=30;schema.RetryCount=0;
            var provider=schema.AssetBundleProviderType;provider.Value=typeof(CacheOnlyAssetBundleProvider);schema.AssetBundleProviderType=provider;
            foreach(var key in new[]{"login-background","primary","secondary","profile"})
            {
                var path=PublicEntryTexture.Path(key);var old="Assets/_LASTRAIL/Resources/EntryUi/"+key+".png";
                if(!File.Exists(path)&&File.Exists(old)){var error=AssetDatabase.MoveAsset(old,path);if(!string.IsNullOrEmpty(error))throw new IOException(error);}
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(importer==null)throw new InvalidOperationException("Public entry artwork missing.");
                importer.npotScale=TextureImporterNPOTScale.None;importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
                var entry=settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path),group);entry.address=path;entry.SetLabel(PublicEntryTexture.Label,true,true);entry.SetLabel(AddressableContentBackend.RequiredLabel,true,true);
            }
            EditorUtility.SetDirty(schema);EditorUtility.SetDirty(group);
        }
        sealed class ReadOnlyJournal:IAccountActionJournal
        {
            public PendingAccountAction Read(string id)=>null;
            public bool Save(PendingAccountAction action)=>throw new InvalidOperationException("Read-only portrait verification.");
            public bool Remove(PendingAccountAction action)=>throw new InvalidOperationException("Read-only portrait verification.");
        }
        public static async void CaptureDevPortraits()
        {
            try
            {
                var wire=new InstalledAdmissionTransport(new UnityWebRequestRunApiHttpTransport(new UnityWebRequestRunApiHttpAdapter("https://dev-api.lastrail.app",30)),new AppBuildSnapshot("0.1.0",42),"windows","qa",()=>"pc-resources-20261111-v01");
                var session=await new GameApiPlayerEntryClient(wire).GuestReplayAsync(File.ReadAllText("D:/Project/LASTRAIL/LocalTemp/DevLogin/landscape-guest-replay.key").Trim());
                if(session==null||session.AccountId!=File.ReadAllText("D:/Project/LASTRAIL/LocalTemp/DevLogin/landscape-account-id.txt").Trim())throw new InvalidOperationException("DEV QA account mismatch.");
                using(var state=new AccountScreenRuntime(wire,new ReadOnlyJournal(),()=>session,()=>1))
                {
                    await state.EnsureAccountAsync();
                    if(state.Account?.ActiveOperators.Count!=5)throw new InvalidOperationException("Actual DEV active formation missing.");
                    Capture(state,"dev-portraits-local-art.png");
                    File.WriteAllText(Output+"dev-portraits-local-art.json","{\"actualDevAuthentication\":true,\"actualDevAccountRead\":true,\"portraits\":5,\"reentryAndUnbind\":true,\"resources\":\"Editor local assets; remote bundle verification pending\"}");
                }
                EditorApplication.Exit(0);
            }
            catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
        }
        public static void CaptureLobby()=>Capture(null,"lobby-independent-review.png");
        static void Capture(AccountScreenRuntime state,string file)
        {
            var previous=PreparedContent.EditorAsset;
            PreparedContent.EditorAsset=(path,type)=>AssetDatabase.LoadAssetAtPath(path,type);
            var cameraNode=new GameObject("Landscape review camera",typeof(Camera));var camera=cameraNode.GetComponent<Camera>();
            var target=new RenderTexture(1600,900,24);var old=RenderTexture.active;
            var root=new GameObject("Landscape lobby review",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
            try
            {
                camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=1<<31;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.targetTexture=target;
                var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);
                var rect=(RectTransform)safe.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
                Canvas.ForceUpdateCanvases();
                var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var deck=LobbyCommandDeck.Attach(root,font,state==null?"지휘관":"QA가로검수1111",0,Array.Empty<MissionShellMissionRowState>(),null,null);deck.BindAccount(state);deck.SetReducedMotion(true);
                if(state!=null)
                {
                    void Check(){if(deck.GetComponentsInChildren<RawImage>().Count(x=>x.name.StartsWith("SquadPortrait")&&x.texture!=null)!=5||deck.GetComponentsInChildren<TMP_Text>().Any(x=>x.name=="EmptySlot"))throw new InvalidOperationException("DEV portraits or plus visibility incorrect.");}
                    Check();deck.gameObject.SetActive(false);deck.gameObject.SetActive(true);Check();
                    deck.BindAccount(null);if(deck.GetComponentsInChildren<RawImage>().Any(x=>x.name.StartsWith("SquadPortrait")&&x.texture!=null))throw new InvalidOperationException("Old portrait reference survived unbind.");
                    deck.BindAccount(state);Check();
                }
                foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);
                try{pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();File.WriteAllBytes(Output+file,pixels.EncodeToPNG());}
                finally{Object.DestroyImmediate(pixels);}
            }
            finally{PreparedContent.EditorAsset=previous;RenderTexture.active=old;Object.DestroyImmediate(root);Object.DestroyImmediate(cameraNode);target.Release();Object.DestroyImmediate(target);}
        }
    }
}
#endif
