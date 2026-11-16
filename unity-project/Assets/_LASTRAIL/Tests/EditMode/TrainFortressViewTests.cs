using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;
using LastRail.Resources;
using LastRail.Run;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace LastRail.Tests.EditMode {
 public sealed class TrainFortressViewTests {
  sealed class Wire:IRunApiHttpTransport {public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r)=>Task.FromResult(r.EndpointPath=="/v1/account/state"?RunApiHttpResponse.Success(200,AccountStateReadTests.Json()):RunApiHttpResponse.NetworkError("fixture"));}
  [OneTimeSetUp] public void Import(){AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("LastRail.Editor.TrainFortressImport")).First(t=>t!=null).GetMethod("Run").Invoke(null,null);}
  [TestCase(false)][TestCase(true)] public async Task ClosingReturnsTrainLeasesAndAllowsReentry(bool delayed){
   var installed=typeof(PreparedContent).GetField("current",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);var previous=installed.GetValue(null);
   var pending=new System.Collections.Generic.List<Action>();int loads=0,releases=0;
   using var assets=new PreparedContent.Session(key=>{
    var asset=AssetDatabase.LoadAssetAtPath<Object>(RemoteContentCatalog.AssetPath(key));Assert.NotNull(asset,key);
    bool train=key.StartsWith("TrainFortress/");if(train)loads++;
    var lease=new PreparedContent.AssetLease(asset,()=>{if(train)releases++;});
    if(!train||!delayed)return Task.FromResult(lease);
    var completion=new TaskCompletionSource<PreparedContent.AssetLease>();pending.Add(()=>completion.SetResult(lease));return completion.Task;
   });
   var root=new GameObject("TrainLifetime",typeof(RectTransform));Task opening=null;
   try{
    var pages=LobbyDestinationPages.Create(root.transform,AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"),()=>{});
    using var common=await assets.PrepareAsync(RemoteContentCatalog.Select(new[]{"LobbyDeck/","DestinationPages/TabIcon"}).Select(e=>e.Key));installed.SetValue(null,assets);
    var open=typeof(LobbyDestinationPages).GetMethod("OpenPreparedPageAsync",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
    for(int cycle=0;cycle<2;cycle++){
     opening=(Task)open.Invoke(pages,new object[]{LobbyPage.Train,false});
     if(delayed){Assert.IsFalse(opening.IsCompleted);pages.Close();foreach(var complete in pending)complete();pending.Clear();await opening;Assert.IsFalse(pages.IsOpen);}
     else{await opening;Assert.NotNull(root.GetComponentInChildren<TrainOverviewView>());Assert.AreEqual(cycle*2,releases);pages.Close();}
     Assert.AreEqual((cycle+1)*2,loads);Assert.AreEqual(loads,releases);
    }
   }finally{foreach(var complete in pending)complete();if(opening!=null)await opening;Object.DestroyImmediate(root);installed.SetValue(null,previous);}
  }
  [Test] public async Task OverviewUsesRemoteSeparateAssetsAndPreservesReentry(){
   var root=new GameObject("TrainReview",typeof(RectTransform),typeof(Canvas));var cameraRoot=new GameObject("TrainCamera",typeof(Camera));
   var auth=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(new Wire(),null,()=>auth,()=>1);await state.RefreshAccountAsync();
   try{
    ((RectTransform)root.transform).sizeDelta=new Vector2(1600,900);var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var pages=LobbyDestinationPages.Create(root.transform,font,()=>{});pages.BindAccount(state);
    pages.Open(LobbyPage.Train);await Task.Yield();Assert.IsTrue(pages.IsOpen);var view=root.GetComponentInChildren<TrainOverviewView>();Assert.NotNull(view);
    Assert.AreEqual(5,view.GetComponentsInChildren<Button>().Count(x=>x.name.StartsWith("TrainModule")));Assert.IsFalse(view.GetComponentsInChildren<Button>().Single(x=>x.name=="UpgradeTrain").interactable);
    Assert.AreEqual("Lv.2",view.GetComponentsInChildren<TMP_Text>().Single(x=>x.name=="StatValue0").text);Assert.That(view.GetComponentsInChildren<TMP_Text>().Where(x=>x.name.StartsWith("StatValue")&&x.name!="StatValue0").All(x=>x.text=="—"));
    Assert.IsFalse(RemoteContentCatalog.Find("TrainFortress/train-overview").Local);Assert.IsFalse(RemoteContentCatalog.Find("TrainFortress/depot-background").Local);
    var stale=view.GetComponentsInChildren<Button>().Single(x=>x.name=="ManageTrainLoadout").onClick;pages.Close();stale.Invoke();Assert.IsFalse(pages.IsOpen);pages.Open(LobbyPage.Train);await Task.Yield();Assert.NotNull(root.GetComponentInChildren<TrainOverviewView>());
    var camera=cameraRoot.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=450;camera.aspect=1600f/900;camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
    foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;Canvas.ForceUpdateCanvases();
    var target=new RenderTexture(1600,900,24);var old=RenderTexture.active;camera.targetTexture=target;try{camera.Render();RenderTexture.active=target;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);try{image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Path.GetFullPath("../QA/ResourceIntake-20261110/connection/train-fortress-v01/unity-train.png"),image.EncodeToPNG());}finally{Object.DestroyImmediate(image);}}finally{camera.targetTexture=null;RenderTexture.active=old;target.Release();Object.DestroyImmediate(target);}
   }finally{Object.DestroyImmediate(root);Object.DestroyImmediate(cameraRoot);}
  }
 }
}
