using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
 public sealed class AdsOffHostQaTests
 {
  sealed class EmptyRuns:IRunOperationJournal
  {
   public Task<IReadOnlyList<PendingRunOperation>> ReadAsync(string a)=>Task.FromResult<IReadOnlyList<PendingRunOperation>>(Array.Empty<PendingRunOperation>());
   public Task<bool> PutAsync(PendingRunOperation o)=>throw new InvalidOperationException("No grants in visual QA");
   public Task<bool> ConfirmAsync(PendingRunOperation o,string j)=>throw new InvalidOperationException("No grants in visual QA");
   public Task<bool> RemoveConfirmedAsync(string a,PendingRunKind k,string key)=>Task.FromResult(false);
  }
  sealed class Wire:IRunApiHttpTransport
  {
   internal AdSkipHostQaTests.Wire Inner;internal string State;internal TaskCompletionSource<RunApiHttpResponse> Loading=new();
   internal Wire(bool combat,string state){State=state;Inner=new AdSkipHostQaTests.Wire(combat,true){Entitlement=AdSkipUseTests.Sample(state=="owned"?"07-owned-available":"03-configured-unowned")};}
   public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r)
   {
    if(r.EndpointPath=="/v1/runtime-policy")return State=="loading"?Loading.Task:Task.FromResult(State=="unknown"?RunApiHttpResponse.NetworkError("TEST_ONLY"):RunApiHttpResponse.Success(200,AdRuntimePolicyTests.Policy(false)));
    Assert.That(r.Method,Is.EqualTo("GET"),"Display inspection must not grant rewards");return Inner.SendAsync(r);
   }
  }
  [UnityTest] public IEnumerator ActualHostsHideAdHitTargetsAndKeepBasicAcrossSizes()
  {
   yield return new EnterPlayMode();
   string output=Path.GetFullPath("../QA/AdsOffUnity-20260921/hosts-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(output);Debug.Log("ADS_OFF_HOST_OUTPUT="+output);var cases=new JArray();
   foreach(int width in new[]{360,720,1080})foreach(bool combat in new[]{false,true})foreach(string state in new[]{"off","owned","unknown","loading","pending"})
   {
    int height=width*16/9;string name=(combat?"combat":"supply")+"-"+width+"-"+state;
    var root=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));((RectTransform)root.transform).sizeDelta=new Vector2(width,height);
    var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=new Vector2(0,-height*.04f);
    var events=new GameObject("QaEvents",typeof(EventSystem));var cameraObject=new GameObject("QaCamera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();var target=new RenderTexture(width,height,24);var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);var previous=RenderTexture.active;
    var wire=new Wire(combat,state);var session=AdSkipUseTests.Session();var journal=new AccountActionJournal(new AdSkipUseTests.Storage());string run=(string)wire.Inner.Lifecycle["runId"];
    if(state=="pending")Assert.That(journal.Save(new PendingAccountAction(session.AccountId,combat?"combat-ad-intent":"ad-intent",combat?run:(string)wire.Inner.Pending["intentId"],0,"test_pending",wire.Inner.Pending.ToString())),Is.True);
    using var account=new AccountScreenRuntime(wire,journal,()=>session,()=>1);using var runs=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),new EmptyRuns(),()=>session,()=>1);account.BindRunRecovery(runs);
    try
    {
     camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=(float)width/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
     var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var deck=LobbyCommandDeck.Attach(root,font,"TEST_ONLY",0,Array.Empty<MissionShellMissionRowState>(),null,null);deck.BindAccount(account);
     if(combat)deck.OpenRunLifecycle(run);else root.GetComponentsInChildren<Button>().Single(b=>b.name=="Shortcut3").onClick.Invoke();yield return null;
     Canvas.ForceUpdateCanvases();deck.EvaluateLayout();Canvas.ForceUpdateCanvases();camera.Render();
     var ad=root.GetComponentsInChildren<Button>(true).Single(b=>b.name==(combat?"VictoryAd":"OpenOfflineRoulette"));bool visible=state=="owned"||state=="pending";Assert.That(ad.gameObject.activeSelf,Is.EqualTo(visible),name);
     var ar=(RectTransform)ad.transform;var pointer=new PointerEventData(events.GetComponent<EventSystem>()){position=RectTransformUtility.WorldToScreenPoint(camera,ar.TransformPoint(ar.rect.center)),button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);
     if(!visible){Assert.That(hits.Any(h=>h.gameObject.GetComponentInParent<Button>()==ad),Is.False);ad.onClick.Invoke();Assert.That(root.GetComponentInChildren<SharedRoulettePopup>(),Is.Null);}
     bool basic=combat?account.CanClaimRunReward(run):new OfflineSupplyDisplayState(account).CanClaim;if(state!="pending")Assert.That(basic,Is.True,name);
     if(combat&&state=="pending")Assert.That(root.GetComponentInChildren<RunVictoryView>().GetComponentsInChildren<Button>().Single(b=>b.name=="VictoryClaim").interactable,Is.False,"Original pending result has priority over a new basic claim");
     var scope=combat?(Component)root.GetComponentInChildren<RunVictoryView>():root.GetComponentInChildren<OfflineSupplyView>();int checkedText=0;
     var corners=new Vector3[4];((RectTransform)scope.transform).GetWorldCorners(corners);foreach(var corner in corners){var screen=camera.WorldToScreenPoint(corner);Assert.That(screen.x,Is.InRange(-1f,width+1f),name+" panel x");Assert.That(screen.y,Is.InRange(-1f,height+1f),name+" panel y");}
     foreach(var t in scope.GetComponentsInChildren<TMP_Text>()){t.ForceMeshUpdate();Assert.That(t.isTextOverflowing,Is.False,name+" "+t.name+" "+t.text);checkedText++;}
     RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),pixels.EncodeToPNG());
     Assert.That(wire.Inner.Authorizations,Is.Zero);Assert.That(wire.Inner.Spins,Is.Zero);
     cases.Add(new JObject{["name"]=name,["adEntryVisible"]=visible,["basicAvailable"]=basic,["hiddenHitTargetAbsent"]=!visible,["textChecked"]=checkedText,["writes"]=0});
    }
    finally{wire.Loading.TrySetResult(RunApiHttpResponse.NetworkError("TEST_ONLY closed"));RenderTexture.active=previous;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);}
    yield return null;
   }
   File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="TEST_ONLY real host composition; no network, SDK, grants or operational adapters",["cases"]=cases}.ToString());yield return new ExitPlayMode();
  }
 }
}
