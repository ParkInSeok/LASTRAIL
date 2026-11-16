using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Resources;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
namespace LastRail.Editor {
 public sealed class EditorProjectContent:IEditorProjectContent {
  static readonly SemaphoreSlim Initialization=new SemaphoreSlim(1,1);
  static Task<IResourceLocator> initialized;
  public string SettingsPath {get;private set;}
  [InitializeOnLoadMethod] static void Register()=>EditorProjectContentBridge.Factory=()=>new EditorProjectContent();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset(){initialized=null;Register();}
  public async Task<IResourceLocator> InitializeAsync(CancellationToken cancel){
   await Initialization.WaitAsync(cancel);
   try{
    SettingsPath=LocalPackedContentBuild.RequireSettings();
    LocalPackedContentServer.Start(Path.GetDirectoryName(Path.GetDirectoryName(SettingsPath)));
    if(initialized==null)initialized=EditorProjectContentBridge.InitializeBuiltAsync(SettingsPath,CancellationToken.None);
    var locator=await initialized;cancel.ThrowIfCancellationRequested();
    if(locator==null||locator.GetType().FullName.Contains("AddressableAssetSettingsLocator"))throw new InvalidOperationException("Built catalog required; project-asset loading is forbidden.");
    if(!locator.Locate(AddressableContentBackend.MainSceneKey,typeof(UnityEngine.ResourceManagement.ResourceProviders.SceneInstance),out var main)||main.Count!=1||!locator.Locate(AddressableContentBackend.LobbyKey,typeof(GameObject),out var lobby)||lobby.Count!=1)throw new InvalidOperationException("Built main scene or lobby is missing or ambiguous.");
    return locator;
   }finally{Initialization.Release();}
  }
 }
}
