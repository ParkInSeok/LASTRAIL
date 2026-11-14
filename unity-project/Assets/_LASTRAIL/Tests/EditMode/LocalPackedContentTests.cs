using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Resources;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.TestTools;
using System.Linq;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace LastRail.Tests.EditMode {
 public sealed class LocalPackedContentTests {
  [Test] public void MissingBuiltSettingsNeverUseProjectAssets(){
   Assert.ThrowsAsync<InvalidOperationException>(async()=>await EditorProjectContentBridge.InitializeBuiltAsync(Path.GetFullPath("Library/LocalPackedContent/missing/runtime-output/settings.json"),CancellationToken.None));
  }
  [UnityTest] public IEnumerator BuiltOnlyDownloadCacheAndRelease(){
   yield return new EnterPlayMode();var task=Verify();while(!task.IsCompleted)yield return null;var error=task.Exception;yield return new ExitPlayMode();if(error!=null)throw error;
  }
  static async Task Verify(){
   Assert.IsNull(PreparedContent.EditorAsset,"Play mode must not use the authoring adapter.");
   var prior=Caching.currentCacheForWriting;var directory=Path.GetFullPath("../LocalTemp/local-packed-cache-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);var cache=Caching.AddCache(directory);Caching.currentCacheForWriting=cache;
   var policy=new DownloadAccessPolicy();policy.SetNetwork(ContentNetwork.Wifi);
   using var backend=new AddressableContentBackend(null,true,policy,source:ContentResourceSource.EditorLocal);
   var gate=typeof(ClientContentGate).GetMethod("Approved",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"test","test","https://unused.invalid/catalog.bin","sha256:"+new string('0',64),false});
   typeof(AddressableContentBackend).GetField("approvedGate",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(backend,gate);
   using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(8));
   try{
    using(var publicOwner=PreparedContent.Install(backend)){await PublicEntryTexture.IsCachedAsync(timeout.Token);}
    await backend.InitializeAsync(timeout.Token);await backend.UpdateCatalogAsync(timeout.Token);
    var bytes=await backend.RequiredBytesAsync(timeout.Token);Assert.GreaterOrEqual(bytes,0);Debug.Log("LOCAL_PACKED_TEST bytes="+bytes);
    var key=RemoteContentCatalog.Find("TrainFortress/train-overview").Address;
    var size=Addressables.GetDownloadSizeAsync(key);await size.Task;try{if(size.Result>0)Assert.Throws<InvalidOperationException>(()=>backend.ResolveApprovedTexture(key));}finally{Addressables.Release(size);}
    policy.SetPaused(true);using(var cancelled=new CancellationTokenSource()){cancelled.Cancel();var pending=backend.DownloadAsync((a,b)=>{},cancelled.Token);bool stopped=false;try{await pending;}catch(OperationCanceledException){stopped=true;}Assert.IsTrue(stopped);}policy.SetPaused(false);
    await backend.DownloadAsync((a,b)=>{},timeout.Token);Assert.AreEqual(0,await backend.RequiredBytesAsync(timeout.Token));Debug.Log("LOCAL_PACKED_TEST cache ready");
    using(var resources=PreparedContent.Install(backend)){
     await VerifyOperatorScreen(timeout.Token);
     for(int repeat=0;repeat<2;repeat++){
      using(var portraits=await PreparedContent.PrepareAsync(new[]{"FormationPlan/portrait-reina","FormationPlan/portrait-kyle","FormationPlan/portrait-eve","FormationPlan/portrait-sera","FormationPlan/portrait-nyx"},timeout.Token)){
       foreach(var id in new[]{"op_reina","op_kyle","op_eve","op_sera","op_nyx"}){
        var face=LastRail.Presentation.MissionShell.FormationPlanView.Portrait(id);Assert.NotNull(face,id);Assert.Greater(face.width,100,id);
       }
      }
      Assert.Throws<InvalidOperationException>(()=>LastRail.Presentation.MissionShell.FormationPlanView.Portrait("op_nyx"));
     }
     var scope=await PreparedContent.PrepareAsync(new[]{"TrainFortress/"},timeout.Token);var train=PreparedContent.Load<Texture2D>("TrainFortress/train-overview");Assert.NotNull(train);Assert.Greater(train.width,1000);scope.Dispose();
     Assert.Throws<InvalidOperationException>(()=>PreparedContent.Load<Texture2D>("TrainFortress/train-overview"));
     var again=await PreparedContent.PrepareAsync(new[]{"TrainFortress/"},timeout.Token);Assert.NotNull(PreparedContent.Load<Texture2D>("TrainFortress/train-overview"));again.Dispose();
    }
    // A cached reopen must succeed without a running origin server.
    Type.GetType("LastRail.Editor.LocalPackedContentServer, LastRail.Editor").GetMethod("Stop").Invoke(null,null);
    await backend.DownloadAsync((a,b)=>{},timeout.Token);Assert.AreEqual(0,await backend.RequiredBytesAsync(timeout.Token));Debug.Log("LOCAL_PACKED_TEST cache ready");
    var handle=Addressables.LoadAssetAsync<Texture2D>(backend.ResolveApprovedTexture(key));await handle.Task;Assert.NotNull(handle.Result);Addressables.Release(handle);Assert.IsFalse(handle.IsValid());
    File.WriteAllText(Path.GetFullPath("../QA/ResourceIntake-20261110/connection/train-fortress-v01/local-packed-result.json"),"{\"initialMissingBytes\":"+bytes+",\"cachedRemainingBytes\":0,\"originStoppedCacheLoad\":true,\"cancelReentryRelease\":true}");
   }finally{Caching.currentCacheForWriting=prior;Caching.RemoveCache(cache);}
  }
  sealed class OperatorWire:IRunApiHttpTransport {
   public int Writes;
   public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){
    if(r.Method!="GET")Writes++;
    var json=JObject.Parse(FormationContractTests.S03Json());var row=(JObject)json["operators"][0];
    json["operators"]=new JArray(new[]{"op_reina","op_kyle","op_eve","op_sera","op_nyx"}.Select(id=>{var item=(JObject)row.DeepClone();item["operatorId"]=id;return item;}));
    return Task.FromResult(RunApiHttpResponse.Success(200,json.ToString()));
   }
  }
  static async Task VerifyOperatorScreen(CancellationToken token){
   using var shared=await PreparedContent.PrepareAsync(new[]{"LobbyDeck/","OperatorIdentity/","NumberedFormation/","ApprovedUi/","DestinationPages/tab-states-v01","DestinationPages/TabIcon"},token);
   var wire=new OperatorWire();var auth=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,null,()=>auth,()=>1);await state.EnsureAccountAsync();
   var root=new GameObject("OperatorReview",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));var cameraNode=new GameObject("OperatorCamera",typeof(Camera));var events=new GameObject("OperatorEvents",typeof(EventSystem));
   LobbyDestinationPages pages=null;
   try{
    ((RectTransform)root.transform).sizeDelta=new Vector2(1600,900);
    pages=LobbyDestinationPages.Create(root.transform,PreparedContent.Load<TMP_FontAsset>("LobbyDeck/serif-v07"),()=>{});pages.BindAccount(state);
    var open=typeof(LobbyDestinationPages).GetMethod("OpenPreparedPageAsync",BindingFlags.NonPublic|BindingFlags.Instance);
    await (Task)open.Invoke(pages,new object[]{LobbyPage.Operators,false});Assert.IsTrue(pages.IsOpen);
    var camera=cameraNode.GetComponent<Camera>();camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
    var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
    var buttons=root.GetComponentsInChildren<Button>();
    foreach(var b in buttons.Where(b=>b.name.StartsWith("SelectOwnedOperator")))b.onClick.Invoke();
    Assert.AreEqual("닉스",root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="SelectedName").text);buttons.Single(b=>b.name=="SelectOwnedOperator0").onClick.Invoke();
    foreach(int width in new[]{1600,1200,2100}){
     ((RectTransform)root.transform).sizeDelta=new Vector2(width,900);camera.orthographicSize=450;camera.aspect=width/900f;
     var target=new RenderTexture(width,900,24);var previous=RenderTexture.active;camera.targetTexture=target;
     try{
      typeof(LobbyDestinationPages).GetMethod("Fit",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(pages,null);
      foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;await Task.Yield();Canvas.ForceUpdateCanvases();camera.Render();
      if(width==1600){RenderTexture.active=target;var image=new Texture2D(width,900,TextureFormat.RGB24,false);try{image.ReadPixels(new Rect(0,0,width,900),0,0);image.Apply();File.WriteAllBytes(Path.GetFullPath("../QA/ResourceIntake-20261110/connection/train-fortress-v01/unity-operators-built.png"),image.EncodeToPNG());}finally{UnityEngine.Object.Destroy(image);}}
      foreach(var b in buttons.Where(b=>b.name.StartsWith("SelectOwnedOperator")||b.name=="ManageTrainingCore"||b.name=="OpenFormation")){
       var rect=(RectTransform)b.transform;var point=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center));Assert.That(point.x,Is.InRange(0,width),b.name);Assert.That(point.y,Is.InRange(0,900),b.name);
       var hits=new System.Collections.Generic.List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(new PointerEventData(events.GetComponent<EventSystem>()){position=point},hits);
       Assert.IsNotEmpty(hits,b.name+" point="+point+" depth="+b.GetComponent<Image>().depth+" pixel="+camera.pixelRect+" active="+b.gameObject.activeInHierarchy);Assert.AreSame(b,hits[0].gameObject.GetComponentInParent<Button>(),b.name);
      }
     }finally{camera.targetTexture=null;RenderTexture.active=previous;target.Release();UnityEngine.Object.Destroy(target);}
    }
    var retired=buttons.Single(b=>b.name=="GrowOperator0").onClick;pages.Close();retired.Invoke();await (Task)open.Invoke(pages,new object[]{LobbyPage.Operators,false});Assert.IsTrue(pages.IsOpen);retired.Invoke();Assert.AreEqual(0,wire.Writes);pages.Close();
    Assert.Throws<InvalidOperationException>(()=>FormationPlanView.Portrait("op_nyx"));
   }finally{if(pages!=null)pages.Close();root.SetActive(false);UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(cameraNode);UnityEngine.Object.Destroy(events);}
  }
 }
}
