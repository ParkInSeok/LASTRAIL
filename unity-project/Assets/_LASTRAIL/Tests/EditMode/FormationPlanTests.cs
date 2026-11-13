using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LastRail.Resources;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine.EventSystems;
namespace LastRail.Tests.EditMode {
 public class FormationPlanTests {
  static readonly string[] Ids={"op_kyle","op_reina","op_eve","op_nyx","op_sera"};
  sealed class Journal:IAccountActionJournal {public PendingAccountAction Read(string id)=>null;public bool Save(PendingAccountAction a)=>true;public bool Remove(PendingAccountAction a)=>true;}
  sealed class Wire:IRunApiHttpTransport {
   public int Reads;
   public int Writes;public TaskCompletionSource<RunApiHttpResponse> Pending;
   public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){if(r.Method=="POST"){Writes++;return Pending.Task;}Reads++;var j=JObject.Parse(FormationContractTests.S03Json());var original=(JObject)j["operators"][0];j["operators"]=new JArray(Ids.Select(id=>{var row=(JObject)original.DeepClone();row["operatorId"]=id;return row;}));j["trainingCoreSlots"]=new JArray();return Task.FromResult(RunApiHttpResponse.Success(r.EndpointPath=="/v1/account/state"?200:404,r.EndpointPath=="/v1/account/state"?j.ToString():"{}"));}
  }
  static Button Button(GameObject root,string name)=>root.GetComponentsInChildren<Button>(true).Single(b=>b.name==name);
  [Test] public void OtherOperatorsKeepTheirExistingCropAndMiraRemainsSeparateFromInitialFive() {
   var texture=new Texture2D(16,32);
   try{Assert.AreEqual(OperatorVisualCatalog.Crop(texture,151,151),FormationPlanView.PortraitCrop("op_vex",texture));Assert.NotNull(RemoteContentCatalog.Find("FormationPlan/portrait-nyx"));Assert.IsNull(RemoteContentCatalog.Find("FormationPlan/portrait-mira"));Assert.NotNull(RemoteContentCatalog.Find("OperatorPortraits/mira"));}
   finally{UnityEngine.Object.DestroyImmediate(texture);}
  }
  [Test] public void IndependentAssetsAreRemoteRequiredAndExcludedFromBootstrap() {
   var settings=UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
   var assets=RemoteContentCatalog.Select(new[]{"FormationPlan/"}).ToArray();Assert.AreEqual(7,assets.Length);
   var bootstrap=AssetDatabase.GetDependencies("Assets/_LASTRAIL/Scenes/ResourceBootstrap.unity",true);
   foreach(var asset in assets){Assert.IsFalse(asset.Local);Assert.IsFalse(bootstrap.Contains(asset.Path));var entry=settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(asset.Path));Assert.NotNull(entry,asset.Key);Assert.AreEqual(asset.Address,entry.address);Assert.IsTrue(entry.labels.Contains(AddressableContentBackend.RequiredLabel));Assert.AreEqual("remote_resources",entry.parentGroup.Name);Assert.IsFalse(((TextureImporter)AssetImporter.GetAtPath(asset.Path)).isReadable);}
  }
  [Test] public async Task PageCancelsLateLoadReentersAndReturnsEveryOwnedHandle() {
   var current=typeof(PreparedContent).GetField("current",BindingFlags.Static|BindingFlags.NonPublic);var previous=current.GetValue(null);
   var pending=new Dictionary<string,TaskCompletionSource<PreparedContent.AssetLease>>();int loads=0,releases=0;bool delayed=true;
   using var session=new PreparedContent.Session(key=>{loads++;var t=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RemoteContentCatalog.AssetPath(key));Assert.NotNull(t,key);if(!delayed)return Task.FromResult(new PreparedContent.AssetLease(t,()=>releases++));var done=new TaskCompletionSource<PreparedContent.AssetLease>();pending.Add(key,done);return done.Task;});
   var root=new GameObject("ResourceLifecycle",typeof(RectTransform));var wire=new Wire();var auth=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,new Journal(),()=>auth,()=>1);await state.RefreshAccountAsync();
   // Creation borrows the existing lobby chrome, before switching to a tracked test resource owner.
   var pages=LobbyDestinationPages.Create(root.transform,AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"),()=>{});pages.BindAccount(state);
   try {
    current.SetValue(null,session);pages.Open(LobbyPage.Formation);Assert.IsFalse(pages.IsOpen);pages.Close();
    foreach(var pair in pending)pair.Value.SetResult(new PreparedContent.AssetLease(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RemoteContentCatalog.AssetPath(pair.Key)),()=>releases++));
    await Task.Yield();Assert.IsFalse(pages.IsOpen);Assert.AreEqual(loads,releases);
    delayed=false;
    // The parent lobby owns identity data and legacy fallback portraits in production.
    using(var shared=await session.PrepareAsync(RemoteContentCatalog.Select(new[]{"OperatorIdentity/","NumberedFormation/","LobbyDeck/","ApprovedUi/","DestinationPages/tab-states-v01"}).Where(e=>!e.Local).Select(e=>e.Key))) {
     int baseLoads=loads,baseReleases=releases;
     for(int repeat=0;repeat<3;repeat++) {pages.Open(LobbyPage.Formation);await Task.Yield();Assert.IsTrue(pages.IsOpen);Assert.AreEqual(5,root.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("FormationSlot")));var stale=Button(root,"FormationSlot4").onClick;pages.Close();stale.Invoke();Assert.IsTrue(state.Formation.Selection.All(x=>x==null));Assert.AreEqual(loads-baseLoads,releases-baseReleases);}
    }
    Assert.AreEqual(loads,releases);
   }finally{current.SetValue(null,previous);UnityEngine.Object.DestroyImmediate(root);}
  }
  [Test] public async Task RenderActualPageAndKeepDraftAcrossCloseReentry() {
   var root=new GameObject("FormationReview",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
   var cameraNode=new GameObject("FormationReviewCamera",typeof(Camera));var events=new GameObject("FormationReviewEvents",typeof(EventSystem));var camera=cameraNode.GetComponent<Camera>();var canvas=root.GetComponent<Canvas>();
   var wire=new Wire();var auth=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,new Journal(),()=>auth,()=>1);await state.RefreshAccountAsync();for(int i=0;i<4;i++)state.Formation.Select(i,Ids[i]);
   try {
    var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var pages=LobbyDestinationPages.Create(root.transform,font,()=>{});pages.BindAccount(state);pages.Open(LobbyPage.Formation);await Task.Yield();
    Button(root,"AssignOwned0").onClick.Invoke();Button(root,"FormationSlot4").onClick.Invoke();Assert.AreEqual("op_kyle",state.Formation.Selection[4]);Assert.IsNull(state.Formation.Selection[0]);
    var expected=state.Formation.Selection;var staleName=root.GetComponentInChildren<TMP_InputField>().onEndEdit;var expectedName=state.Formation.Name;pages.Close();staleName.Invoke("stale name");Assert.AreEqual(expectedName,state.Formation.Name);pages.Open(LobbyPage.Formation);await Task.Yield();CollectionAssert.AreEqual(expected,state.Formation.Selection);
    state.Formation.Reset();for(int i=0;i<4;i++)state.Formation.Select(i,Ids[i]);pages.RefreshAccountView(state);Button(root,"FormationSlot1").onClick.Invoke();
    camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
    canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
    var output=Path.GetFullPath("../QA/ResourceIntake-20261110/connection/formation-left-navigation-v01");Directory.CreateDirectory(output);
    foreach(var size in new[]{new Vector2Int(1600,900),new Vector2Int(1200,900),new Vector2Int(2100,900)}) {
     ((RectTransform)root.transform).sizeDelta=new Vector2(size.x,size.y);camera.orthographicSize=size.y/2f;camera.aspect=(float)size.x/size.y;
     var target=new RenderTexture(size.x,size.y,24);var old=RenderTexture.active;camera.targetTexture=target;
     try {foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;Canvas.ForceUpdateCanvases();typeof(LobbyDestinationPages).GetMethod("Fit",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pages,null);Canvas.ForceUpdateCanvases();camera.Render();
      foreach(string name in new[]{"FormationSlot0","FormationSlot4","AssignOwned0","SaveFormation","PageNav0","PageNav1","PageNav2","PageNav3","PageNav4","NewPreset","Back"}) {var button=Button(root,name);var rect=(RectTransform)button.transform;var point=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center));Assert.That(point.x,Is.InRange(0,size.x),name);Assert.That(point.y,Is.InRange(0,size.y),name);var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(new PointerEventData(events.GetComponent<EventSystem>()){position=point},hits);Assert.IsNotEmpty(hits,name);Assert.AreSame(button,hits[0].gameObject.GetComponentInParent<Button>(),name+" is obstructed at "+size);}
      RenderTexture.active=target;var pixels=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);try{pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);pixels.Apply();File.WriteAllBytes(output+"/unity-"+size.x+".png",pixels.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(pixels);}}
     finally{camera.targetTexture=null;RenderTexture.active=old;target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
   }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraNode);UnityEngine.Object.DestroyImmediate(events);}
  }
  [Test] public async Task SaveDoubleClickAndLateFailureCannotRebuildReenteredPage() {
   var wire=new Wire{Pending=new TaskCompletionSource<RunApiHttpResponse>()};var auth=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,new Journal(),()=>auth,()=>1);await state.RefreshAccountAsync();for(int i=0;i<5;i++)state.Formation.Select(i,Ids[i]);
   var root=new GameObject("LateSave",typeof(RectTransform));
   try {var pages=LobbyDestinationPages.Create(root.transform,AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"),()=>{});pages.BindAccount(state);pages.Open(LobbyPage.Formation);await Task.Yield();var button=Button(root,"SaveFormation");Assert.IsTrue(button.interactable);button.onClick.Invoke();button.onClick.Invoke();Assert.AreEqual(1,wire.Writes);pages.Close();pages.Open(LobbyPage.Formation);await Task.Yield();var reopened=Button(root,"FormationSlot0");wire.Pending.SetResult(RunApiHttpResponse.Success(503,"{}"));await Task.Yield();Assert.AreSame(reopened,Button(root,"FormationSlot0"));Assert.NotNull(state.Pending,"Unknown result must retain original save for recovery");Assert.AreEqual(1,wire.Writes);}
   finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  [Test] public void LivePageHasFiveIndependentSlotsAndEmptyPlus() {
   var root=new GameObject("PlanTest",typeof(RectTransform));
   try {
    var type=typeof(LobbyDestinationPages).Assembly.GetType("LastRail.Presentation.MissionShell.FormationPlanView");
    Assert.NotNull(type,"Approved five-slot view is missing");
    var view=(Component)type.GetMethod("Create").Invoke(null,new object[]{root.transform,null});
    int calls=0;Action<int> callback=i=>calls++;
    type.GetMethod("Bind").Invoke(view,new object[]{callback});type.GetMethod("Bind").Invoke(view,new object[]{callback});
    type.GetMethod("Render").Invoke(view,new object[]{new string[]{"op_kyle","op_reina","op_eve","op_nyx",null},1,true});
    Assert.AreEqual(5,root.GetComponentsInChildren<Button>().Length);
    var slot=view.transform.Find("FormationSlot4");
    Assert.IsTrue(slot.Find("EmptyPlus").gameObject.activeSelf);Assert.IsFalse(slot.Find("Portrait").GetComponent<RawImage>().enabled);
    Assert.AreEqual("5",slot.Find("Number").GetComponent<TMP_Text>().text);
    Assert.NotNull(slot.Find("Frame"));Assert.NotNull(view.transform.Find("TrainPlanBackground"));
    slot.Find("Frame").gameObject.SetActive(false);slot.Find("Number").gameObject.SetActive(false);
    Assert.IsTrue(slot.Find("EmptyPlus").gameObject.activeSelf);Assert.IsTrue(view.transform.Find("TrainPlanBackground").gameObject.activeSelf);
    slot.GetComponent<Button>().onClick.Invoke();Assert.AreEqual(1,calls);
    view.gameObject.SetActive(false);slot.GetComponent<Button>().onClick.Invoke();Assert.AreEqual(1,calls);
    view.gameObject.SetActive(true);type.GetMethod("Bind").Invoke(view,new object[]{callback});
    slot.GetComponent<Button>().onClick.Invoke();Assert.AreEqual(2,calls);
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
 }
}
