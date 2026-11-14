using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastRail.Resources;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;
namespace LastRail.Editor {
 public static class LocalPackedContentBuild {
  static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Library/LPC"));
  public static string RequireSettings(){
   var pointer=Path.Combine(Root,"current.txt");
   if(!File.Exists(pointer))throw new InvalidOperationException("로컬 리소스 빌드가 없습니다. LAST RAIL/Resources/Build Local Bundles를 실행하세요.");
   var path=Path.GetFullPath(File.ReadAllText(pointer));
   if(!path.StartsWith(Root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||Path.GetFileName(path)!="settings.json"||!File.Exists(path))throw new InvalidDataException("Local built settings missing or outside build root.");
   return path;
  }
  [MenuItem("LAST RAIL/Resources/Build Local Bundles")]
  public static void Build(){
   if(EditorApplication.isPlayingOrWillChangePlaymode||EditorUserBuildSettings.activeBuildTarget!=BuildTarget.StandaloneWindows64)throw new InvalidOperationException("Stop Play mode and select Windows64 before local content build.");
   LocalPackedContentServer.Stop();TrainFortressImport.Run();
   var source=AddressableAssetSettingsDefaultObject.Settings;
   if(source==null||source.InitializationObjects.Count!=0)throw new InvalidOperationException("Local settings missing or contain initialization hooks.");
   var owned=new List<UnityEngine.Object>();var output=Path.Combine(Root,Guid.NewGuid().ToString("N").Substring(0,8));Directory.CreateDirectory(output);
   try{
    var settings=DevContentBuild.CloneSettings(source,owned);var profile=settings.profileSettings.GetProfileId("LOCAL_TEST");if(string.IsNullOrEmpty(profile))throw new InvalidOperationException("LOCAL_TEST profile missing.");settings.activeProfileId=profile;
    settings.profileSettings.SetValue(profile,AddressableAssetSettings.kRemoteBuildPath,Path.Combine(output,"remote").Replace('\\','/'));
    settings.profileSettings.SetValue(profile,AddressableAssetSettings.kRemoteLoadPath,EditorProjectContentBridge.LocalRoot);
    settings.profileSettings.SetValue(profile,AddressableAssetSettings.kLocalBuildPath,"[UnityEngine.AddressableAssets.Addressables.BuildPath]/[BuildTarget]");
    settings.profileSettings.SetValue(profile,AddressableAssetSettings.kLocalLoadPath,"{UnityEngine.AddressableAssets.Addressables.RuntimePath}/[BuildTarget]");
    settings.ContentStateBuildPath=Path.Combine(output,"state");Directory.CreateDirectory(settings.ContentStateBuildPath);
    settings.buildSettings.bundleBuildPath=Path.Combine(output,"intermediate");settings.BuildRemoteCatalog=true;settings.DisableCatalogUpdateOnStartup=true;
    foreach(var group in settings.groups.Where(g=>g!=null)){
     var schema=group.GetSchema<BundledAssetGroupSchema>();if(schema==null)continue;
     schema.BuildPath.SetVariableByName(settings,AddressableAssetSettings.kRemoteBuildPath);schema.LoadPath.SetVariableByName(settings,AddressableAssetSettings.kRemoteLoadPath);
     var provider=schema.AssetBundleProviderType;provider.Value=typeof(CacheOnlyAssetBundleProvider);schema.AssetBundleProviderType=provider;
    }
    var runtime=Path.GetFullPath(Addressables.BuildPath);
    DevContentBuild.WithRuntimeOutput(Path.GetDirectoryName(Application.dataPath),runtime,output,()=>{
     var result=settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(new AddressablesDataBuilderInput(settings));
     if(result==null||!string.IsNullOrEmpty(result.Error))throw new InvalidOperationException(result?.Error??"No content build result");
     AddressableResourceBuilder.PrepareInstalledCatalogSettings();
     var path=Path.Combine(runtime,"settings.json");var text=File.ReadAllText(path).Replace("{UnityEngine.AddressableAssets.Addressables.RuntimePath}",Path.Combine(output,"runtime-output").Replace('\\','/'));File.WriteAllText(path,text);
    });
    File.WriteAllText(Path.Combine(Root,"current.txt"),Path.Combine(output,"runtime-output/settings.json"));
    Debug.Log("LOCAL_PACKED_CONTENT_BUILT "+output);
   }finally{foreach(var item in owned.AsEnumerable().Reverse())UnityEngine.Object.DestroyImmediate(item);}
  }
 }
}
