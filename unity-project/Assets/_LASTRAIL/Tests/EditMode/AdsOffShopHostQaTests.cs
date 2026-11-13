using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
 public sealed class AdsOffShopHostQaTests
 {
  sealed class Wire:IRunApiHttpTransport
  {
   internal string Mode="owned";internal int Reads,Writes;internal TaskCompletionSource<RunApiHttpResponse> Gate;
   public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r)
   {
    if(r.Method!="GET"){Writes++;throw new InvalidOperationException("No purchase or reward in shop QA");}
    if(r.EndpointPath=="/v1/runtime-policy")return Task.FromResult(RunApiHttpResponse.Success(200,AdRuntimePolicyTests.Policy(false)));
    if(r.EndpointPath=="/v1/ad-skip")
    {
     Reads++;if(Mode=="pending")return Gate.Task;
     return Task.FromResult(Mode=="error"?RunApiHttpResponse.NetworkError("TEST_ONLY entitlement unavailable"):RunApiHttpResponse.Success(200,AdSkipUseTests.Sample(Mode=="owned"?"07-owned-available":"03-configured-unowned")));
    }
    return Task.FromResult(RunApiHttpResponse.NetworkError("TEST_ONLY unrelated account service unavailable"));
   }
  }
  [UnityTest] public IEnumerator FullShopPreservesManagementAndQueryLifecycleWithoutFakeStoreRestore()
  {
   yield return new EnterPlayMode();string output=Path.GetFullPath("../QA/AdsOffUnity-20260921/Followup/shop-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(output);Debug.Log("ADS_OFF_SHOP_OUTPUT="+output);var records=new JArray();
   foreach(int width in new[]{360,720,1080})
   {
    int height=width*16/9;var root=new GameObject("ShopHost",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));((RectTransform)root.transform).sizeDelta=new Vector2(width,height);
    var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=new Vector2(0,-height*.04f);
    var events=new GameObject("QaEvents",typeof(EventSystem));var co=new GameObject("QaCamera",typeof(Camera));var camera=co.GetComponent<Camera>();var target=new RenderTexture(width,height,24);var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);var previous=RenderTexture.active;
    var wire=new Wire();var session=AdSkipUseTests.Session();using var account=new AccountScreenRuntime(wire,new AccountActionJournal(new AdSkipUseTests.Storage()),()=>session,()=>1);
    try
    {
     camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=(float)width/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
     var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var deck=LobbyCommandDeck.Attach(root,font,"TEST_ONLY",0,Array.Empty<MissionShellMissionRowState>(),null,null);deck.BindAccount(account);
     Button Find(string name)=>root.GetComponentsInChildren<Button>().Single(b=>b.name==name);
     void Draw(){Canvas.ForceUpdateCanvases();deck.EvaluateLayout();Canvas.ForceUpdateCanvases();camera.Render();}
     void Click(string name,bool enabled=true)
     {
      Draw();var b=Find(name);Assert.That(b.interactable,Is.EqualTo(enabled),name);var rect=(RectTransform)b.transform;var pointer=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
      var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);Assert.That(hits.Count,Is.GreaterThan(0));Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.SameAs(b),name);ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerClickHandler);
     }
     void Capture(string state)
     {
      Draw();var view=root.GetComponentInChildren<AdSkipShopView>();Assert.That(view,Is.Not.Null);var purchase=view.GetComponentsInChildren<Button>(true).Single(b=>b.name=="AdSkipPurchase");var restore=Find("AdSkipRestore");var refresh=Find("AdSkipRefresh");
      Assert.That(purchase.gameObject.activeSelf,Is.False);Assert.That(restore.interactable,Is.False,"Store restore adapter is absent, not a simulated pending purchase");int before=wire.Reads;purchase.onClick.Invoke();restore.onClick.Invoke();Assert.That(wire.Reads,Is.EqualTo(before));Assert.That(wire.Writes,Is.Zero);
      var corners=new Vector3[4];((RectTransform)view.transform).GetWorldCorners(corners);foreach(var p in corners){var screen=camera.WorldToScreenPoint(p);Assert.That(screen.x,Is.InRange(-1f,width+1f));Assert.That(screen.y,Is.InRange(-1f,height+1f));}
      foreach(var t in view.GetComponentsInChildren<TMP_Text>()){t.ForceMeshUpdate();Assert.That(t.isTextOverflowing,Is.False,t.name+": "+t.text);}
      RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,width+"-"+state+".png"),pixels.EncodeToPNG());
      records.Add(new JObject{["width"]=width,["state"]=state,["sourceEligibility"]=new JObject{["entitlementOwned"]=account.AdSkip.Snapshot?.Owned,["adsEnabled"]=false,["storeRestoreAvailable"]=false},["viewInteractable"]=new JObject{["purchaseActive"]=purchase.gameObject.activeSelf,["restore"]=restore.interactable,["entitlementRefresh"]=refresh.interactable},["writes"]=wire.Writes});
     }
     Click("Navigation4");yield return null;Click("Category1");yield return null;Capture("owned");Click("AdSkipRestore",false);
     wire.Mode="absent";int beforeRead=wire.Reads;Click("AdSkipRefresh");yield return null;Assert.That(wire.Reads-beforeRead,Is.EqualTo(1));Capture("absent");
     wire.Mode="pending";wire.Gate=new TaskCompletionSource<RunApiHttpResponse>();Click("AdSkipRefresh");yield return null;Assert.That(account.AdSkip.Loading,Is.True);Capture("query-pending");Click("AdSkipRefresh",false);
     var oldRefresh=Find("AdSkipRefresh");Click("Back");Assert.That(deck.Pages.IsOpen,Is.False);int closedReads=wire.Reads;oldRefresh.onClick.Invoke();Assert.That(wire.Reads,Is.EqualTo(closedReads));
     wire.Mode="error";wire.Gate.SetResult(RunApiHttpResponse.NetworkError("TEST_ONLY response lost"));yield return null;Assert.That(deck.Pages.IsOpen,Is.False);Click("Navigation4");yield return null;Click("Category1");yield return null;Capture("reentry-error");
     wire.Mode="owned";beforeRead=wire.Reads;Click("AdSkipRefresh");yield return null;Assert.That(wire.Reads-beforeRead,Is.EqualTo(1));Capture("recovered");
     Click("Category2");yield return null;Assert.That(deck.Pages.Category,Is.EqualTo(2),"Non-ad currency category remains reachable");Click("Category1");yield return null;Capture("category-return");Assert.That(root.GetComponentsInChildren<AdSkipShopView>().Length,Is.EqualTo(1));
    }
    finally{wire.Gate?.TrySetResult(RunApiHttpResponse.NetworkError("TEST_ONLY shutdown"));RenderTexture.active=previous;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(co);UnityEngine.Object.DestroyImmediate(events);}
    yield return null;
   }
   File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="TEST_ONLY full production shop host; entitlement query pending/error/reentry verified. Store restore pending/error is unavailable, no adapter/receipt fabricated.",["cases"]=records}.ToString());yield return new ExitPlayMode();
  }
 }
}
