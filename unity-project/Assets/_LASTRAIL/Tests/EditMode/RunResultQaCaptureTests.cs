using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Diagnostics;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Tests.EditMode { public sealed partial class FormationLocalHttpTests {
 [Test,Explicit("Native Unity rendered QA of historical local HTTP results and lobby only; not actual combat.")]
 public async Task CaptureNativeResultRecoveryToLobby(){
  var(wire,session)=await Setup();Func<string,Task<RunApiHttpResponse>> control=p=>wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/__fixture/"+p,null,null),session.AccessToken));
  using var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);await PrepareResultFixture(state,control);
  var journal=new EncryptedRunOperationJournal(TestSecurePort());using var runs=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>session,()=>1,()=>new AppBuildSnapshot("1",100));state.BindRunRecovery(runs);
  Assert.That(await journal.PutAsync(PendingRunOperation.Finish(session.AccountId,SavedFixtureFinish("run_old"),new AppBuildSnapshot("1",100),DateTimeOffset.UtcNow)),Is.True);await runs.RestoreAsync();
  var root=new GameObject("ResultVideo",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var safeRect=(RectTransform)safe.transform;safeRect.anchorMin=Vector2.zero;safeRect.anchorMax=Vector2.one;safeRect.sizeDelta=Vector2.zero;
  var folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../QA/ScreenIntegration/ResultRecoveryVideo-20260911"));Directory.CreateDirectory(folder);
  var cameraObject=new GameObject("ResultVideoCamera",typeof(Camera));var target=new RenderTexture(720,1280,24);var pixels=new Texture2D(720,1280,TextureFormat.RGB24,false);var previous=RenderTexture.active;int frame=0;
  try{
   var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var deck=LobbyCommandDeck.Attach(root,font,"LOCAL QA",1,new MissionShellMissionRowState[0],null,null);deck.BindAccount(state);
   var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=640;camera.aspect=720f/1280;camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=1<<31;camera.targetTexture=target;var canvas=root.GetComponent<Canvas>();if(canvas==null)canvas=root.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
   var labelRoot=new GameObject("QaScope",typeof(RectTransform),typeof(TextMeshProUGUI));labelRoot.transform.SetParent(root.transform,false);var labelRect=(RectTransform)labelRoot.transform;labelRect.anchorMin=labelRect.anchorMax=new Vector2(0,1);labelRect.pivot=new Vector2(0,1);labelRect.anchoredPosition=new Vector2(14,-8);labelRect.sizeDelta=new Vector2(690,36);var label=labelRoot.GetComponent<TMP_Text>();label.font=font;label.fontSize=17;label.color=new Color(1,.8f,.3f);label.text="LOCAL QA · 결과 복구 → 보상 수령 → 로비 / 전투 미포함";
   async Task Record(int count){for(int i=0;i<count;i++){deck.Motion.Evaluate(1f/24);labelRoot.transform.SetAsLastSibling();foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,1280),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,(frame++).ToString("D5")+".png"),pixels.EncodeToPNG());await Task.Delay(1);}}
   async Task Settle(){for(int i=0;state.Busy||state.RunResultsLoading||state.AccountStatus==AccountStateReadStatus.Loading;i++){Assert.That(i,Is.LessThan(240));await Task.Delay(25);}}
   await Record(36);deck.OpenRunLifecycle("run_old");await Settle();await Record(48);
   root.GetComponentsInChildren<Button>().Single(b=>b.name=="RecoverRunFinish").onClick.Invoke();await Settle();Assert.That(state.RunRewardPreview,Is.Not.Null,state.RunResultNotice);await Record(96);
   var claimButton=root.GetComponentsInChildren<Button>().Single(b=>b.name=="ClaimRunReward");Assert.That(claimButton.interactable,Is.True);claimButton.onClick.Invoke();await Settle();await state.RefreshSharedAsync();Assert.That(state.VerifiedRunReceipt,Is.Not.Null,state.RunResultNotice);await Record(96);
   root.GetComponentsInChildren<Button>().Single(b=>b.name=="CloseLobbyDialog").onClick.Invoke();await Settle();Assert.That(deck.IsDialogOpen,Is.False);await Record(72);
   var metrics=JObject.Parse((await control("result-metrics")).BodyJson);Assert.That((int)metrics["claims"],Is.EqualTo(1));Assert.That((int)metrics["finishes"],Is.EqualTo(1));
   File.WriteAllText(Path.Combine(folder,"capture.json"),new JObject{["frames"]=frame,["fps"]=24,["width"]=720,["height"]=1280,["scope"]="NATIVE_EDITOR_RENDER_SEQUENCE_LOCAL_HTTP_RESULTS_ONLY",["actualCombat"]=false,["actualHttp"]=true,["claims"]=1,["finishes"]=1,["journalRowsRetained"]=(await journal.ReadAsync(session.AccountId)).Count}.ToString());
  }finally{RenderTexture.active=previous;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(root);}
 }
} }
