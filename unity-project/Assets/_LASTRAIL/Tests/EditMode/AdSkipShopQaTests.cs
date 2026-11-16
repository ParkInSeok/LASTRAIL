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
    public sealed class AdSkipShopQaTests
    {
        sealed class Wire:IRunApiHttpTransport
        {
            public string State="absent";public int Reads;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                Assert.That(request.Method,Is.EqualTo("GET"),"No store/economy writes in this fixture");
                if(request.EndpointPath!="/v1/ad-skip"||State=="unknown")return Task.FromResult(RunApiHttpResponse.NetworkError("TEST_ONLY missing read"));
                Reads++;var root=JObject.Parse("{\"accepted\":true,\"contractVersion\":\"ad-skip-v1\",\"owned\":false,\"entitlement\":null,\"catalog\":null,\"purchaseAvailable\":false,\"featureAvailability\":{\"OFFLINE\":\"UNAVAILABLE\",\"COMBAT\":\"UNAVAILABLE\",\"REROLL\":\"UNSUPPORTED\"},\"serverTime\":1000}");
                if(State=="owned"){root["owned"]=true;root["entitlement"]=new JObject{["status"]="ACTIVE",["revision"]=1,["permanent"]=true};}
                if(State=="price-missing")root["catalog"]=new JObject{["version"]="TEST_ONLY",["provider"]="GOOGLE_PLAY",["appId"]="fixture.app",["productId"]="fixture_only_not_registered",["price"]=null,["priceSource"]="STORE_REQUIRED"};
                return Task.FromResult(RunApiHttpResponse.Success(200,root.ToString()));
            }
        }
        [UnityTest] public IEnumerator CaptureActualShopWithoutStore()
        {
            yield return new EnterPlayMode();string output=Path.GetFullPath("../QA/AdSkipUnityPhase1-20260917/shop-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(output);Debug.Log("AD_SKIP_SHOP_QA="+output);
            var checks=new JArray();
            foreach(int height in new[]{1280,1600})
            {
                var root=new GameObject("ActualShopQA",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));((RectTransform)root.transform).sizeDelta=new Vector2(720,height);
                var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var safeRect=(RectTransform)safe.transform;safeRect.anchorMin=Vector2.zero;safeRect.anchorMax=Vector2.one;safeRect.sizeDelta=Vector2.zero;
                var events=new GameObject("ShopEvents",typeof(EventSystem));var cameraObject=new GameObject("ShopCamera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();
                var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var previous=RenderTexture.active;
                var wire=new Wire();var session=new GameApiAuthSessionSnapshot("a","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));
                using var account=new AccountScreenRuntime(wire,null,()=>session,()=>1);
                try
                {
                    camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=720f/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;
                    root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;root.GetComponent<Canvas>().worldCamera=camera;
                    var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var deck=LobbyCommandDeck.Attach(root,font,"LOCAL QA",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(account);
                    void Click(string name)
                    {
                        Canvas.ForceUpdateCanvases();deck.EvaluateLayout();Canvas.ForceUpdateCanvases();camera.Render();
                        var button=root.GetComponentsInChildren<Button>().Single(b=>b.name==name);var rect=(RectTransform)button.transform;
                        var pointer=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
                        var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);Assert.That(hits.Count,Is.GreaterThan(0),name);Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.SameAs(button),name);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
                    }
                    Click("Navigation4");yield return null;Click("Category1");yield return null;
                    foreach(string state in new[]{"absent","unknown","owned","price-missing"})
                    {
                        wire.State=state;Click("AdSkipRefresh");yield return null;Canvas.ForceUpdateCanvases();deck.EvaluateLayout();camera.Render();
                        var view=root.GetComponentInChildren<AdSkipShopView>();Assert.That(view,Is.Not.Null);
                        foreach(var text in view.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,text.name+":"+text.text);}
                        Assert.That(view.GetComponentsInChildren<Button>(true).Single(b=>b.name=="AdSkipPurchase").interactable,Is.False);Assert.That(view.GetComponentsInChildren<Button>(true).Single(b=>b.name=="AdSkipRestore").interactable,Is.False);
                        RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();string name=height+"-"+state+".png";File.WriteAllBytes(Path.Combine(output,name),pixels.EncodeToPNG());
                        checks.Add(new JObject{["file"]=name,["purchaseEnabled"]=false,["restoreEnabled"]=false,["overflow"]=false,["state"]=state});
                    }
                }
                finally{RenderTexture.active=previous;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);}
            }
            File.WriteAllText(Path.Combine(output,"verification.json"),checks.ToString());yield return new ExitPlayMode();
        }
    }
}
