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
    public sealed class RunVictoryRewardTests
    {
        const string Source="D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-NoncombatCases-20260914/";
        static JObject Line(string kind,string id,long amount)=>new JObject{["kind"]=kind,["id"]=id,["quantity"]=amount,["source"]="RUN_CLEAR"};
        sealed class Wire:IRunApiHttpTransport
        {
            public JArray Lines=new JArray(Line("CURRENCY","cur_credits",186),Line("CURRENCY","cur_operator_exp",425));
            public bool Receipt,Missing; public int Writes,Reads;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                if(request.Method!="GET"){Writes++;throw new InvalidOperationException("Read-only victory fixture");}
                Reads++;
                if(request.EndpointPath.EndsWith("/lifecycle"))
                {
                    var data=JObject.Parse(File.ReadAllText(Source+"result-lost-finish-restored.json"));
                    data["runStatus"]="COMPLETED";data["finish"]["outcome"]="VICTORY";
                    if(Receipt)
                    {
                        data["reward"]["status"]="CLAIMED";data["reward"]["claimId"]="claim_test";
                        data["reward"]["receipt"]=new JObject{["rewardTransactionId"]="claim_test",["runId"]="run-smoke-1",["serverClaimedAt"]=1789377113408L,["rewardPlan"]=new JObject{["rewardLines"]=Lines.DeepClone()}};
                        data["nextAction"]="RETURN_TO_LOBBY";data["disposition"]="REWARDED";
                    }
                    return Task.FromResult(RunApiHttpResponse.Success(200,data.ToString()));
                }
                if(request.EndpointPath.Contains("/reward-preview/"))
                {
                    if(Missing)return Task.FromResult(RunApiHttpResponse.NetworkError("TEST_ONLY missing preview"));
                    var data=JObject.Parse(File.ReadAllText(Source+"result-preview.json"));data["rewardLines"]=Lines.DeepClone();
                    return Task.FromResult(RunApiHttpResponse.Success(200,data.ToString()));
                }
                return Task.FromResult(RunApiHttpResponse.NetworkError("TEST_ONLY unrelated service unavailable"));
            }
        }
        [TestCase("CURRENCY","cur_credits","크레딧","cur_credits")]
        [TestCase("CURRENCY","cur_machine_parts","기계 부품","cur_machine_parts")]
        [TestCase("CURRENCY","cur_operator_exp","대원 경험치","cur_operator_exp")]
        [TestCase("CURRENCY","cur_tactical_data","전술 자료","cur_tactical_data")]
        [TestCase("FUTURE","cur_credits","미등록 보상",null)]
        [TestCase("CURRENCY","future_coin","미등록 보상",null)]
        [TestCase("ITEM","cur_credits","아이템",null)]
        [TestCase("CRYSTAL","premium","크리스탈",null)]
        public void ProjectionPreservesIdentityWithoutGuessingArt(string kind,string id,string name,string icon)
        {
            var json=Line(kind,id,long.MaxValue);var item=new RunVictoryRewardPresentation(json);json["quantity"]=0;
            Assert.That(item.Kind,Is.EqualTo(kind));Assert.That(item.Id,Is.EqualTo(id));Assert.That(item.Quantity,Is.EqualTo(long.MaxValue));
            Assert.That(item.Name,Is.EqualTo(name));Assert.That(item.IconId,Is.EqualTo(icon));Assert.That(item.QuantityText,Is.EqualTo("9,223,372,036,854,775,807"));
        }
        [Test] public void EqualFallbackLabelsStillInvalidateChangedIdentity()
        {
            var first=new RunVictoryRewardPresentation(Line("FUTURE","future_a",1));
            var second=new RunVictoryRewardPresentation(Line("FUTURE","future_b",1));
            Assert.That(first.Label,Is.EqualTo(second.Label));Assert.That(first.RenderKey,Is.Not.EqualTo(second.RenderKey));
        }
        [Test] public async Task ConfirmedAdReceiptWinsWithoutChangingTheBasicReceipt()
        {
            var wire=new Wire{Receipt=true};var session=SupportInquiryTests.Session();using var owner=new AccountScreenRuntime(wire,null,()=>session,()=>1);
            await owner.ReadRunResultsAsync("run-smoke-1");Assert.That(owner.VerifiedRunReceipt,Is.Not.Null);
            var sample=CombatAdControllerTests.Sample("CLAIMED");sample["runId"]="run-smoke-1";sample["rewardPreviewId"]=owner.Lifecycle.PreviewId;
            sample["result"]["runId"]="run-smoke-1";sample["result"]["rewardPreviewId"]=owner.Lifecycle.PreviewId;
            var receipt=CombatAdIntent.Parse(sample.ToString(),"run-smoke-1",owner.Lifecycle.PreviewId);
            typeof(CombatAdController).GetMethod("AdoptApproved",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(owner.CombatAds,new object[]{receipt});
            var display=new RunVictoryDisplayState(owner,"run-smoke-1",Array.Empty<MissionShellMissionRowState>());
            Assert.That(display.RewardItems.Single().Quantity,Is.EqualTo(480));Assert.That(display.RewardStatus,Is.EqualTo("지급 완료"));
            Assert.That((long)owner.VerifiedRunReceipt["rewardPlan"]["rewardLines"][0]["quantity"],Is.EqualTo(186));
            Assert.That(new RunVictoryDisplayState(owner,"another_run",Array.Empty<MissionShellMissionRowState>()).RewardItems,Is.Empty);
            Assert.That(wire.Writes,Is.Zero);
        }
        [TestCase(80)][TestCase(128)][TestCase(154)][TestCase(180)] public void ExperienceCaptionFitsSmallAndExistingIconSizes(int height)
        {
            var root=new GameObject("IconSize",typeof(RectTransform),typeof(Canvas));
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                RewardIconPresentation.Create(root.transform,font,"cur_operator_exp",new Rect(0,0,height*1.2f,height));
                Canvas.ForceUpdateCanvases();var caption=root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="ExperienceChipLabel");caption.ForceMeshUpdate();Assert.That(caption.isTextOverflowing,Is.False);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [UnityTest] public IEnumerator NativeHostShowsRewardsAndRefreshesWithoutGrantingOrRetainingOldCards()
        {
            yield return new EnterPlayMode();
            var output=Path.GetFullPath("../QA/Result22Rewards-20260921/host-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(output);Debug.Log("RESULT22_OUTPUT="+output);var records=new JArray();
            foreach(int width in new[]{360,720,1080})
            {
                int height=width*16/9;var wire=new Wire();var session=SupportInquiryTests.Session();using var owner=new AccountScreenRuntime(wire,null,()=>session,()=>1);
                var root=new GameObject("VictoryHost",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));((RectTransform)root.transform).sizeDelta=new Vector2(width,height);
                var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
                var co=new GameObject("VictoryCamera",typeof(Camera));var camera=co.GetComponent<Camera>();var target=new RenderTexture(width,height,24);var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);var previous=RenderTexture.active;
                var events=new GameObject("VictoryEvents",typeof(EventSystem));
                try
                {
                    camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=(float)width/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;
                    var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
                    var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                    var deck=LobbyCommandDeck.Attach(root,font,"TEST_ONLY",0,Array.Empty<MissionShellMissionRowState>(),null,null);deck.BindAccount(owner);deck.OpenRunLifecycle("run-smoke-1");yield return null;
                    RunVictoryView View()=>root.GetComponentInChildren<RunVictoryView>();
                    Button Button(string name)=>View().GetComponentsInChildren<Button>(true).Single(b=>b.name==name);
                    void Draw(){Canvas.ForceUpdateCanvases();deck.EvaluateLayout();Canvas.ForceUpdateCanvases();camera.Render();}
                    void Click(string name)
                    {
                        Draw();var b=Button(name);var r=(RectTransform)b.transform;var pointer=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(r.rect.center))};
                        var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);Assert.That(hits.Count,Is.GreaterThan(0));Assert.That(hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>(),Is.SameAs(b));ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerClickHandler);
                    }
                    void Capture(string state)
                    {
                        Draw();var view=View();Assert.That(view,Is.Not.Null);Assert.That(wire.Writes,Is.Zero);
                        foreach(var t in view.GetComponentsInChildren<TMP_Text>()){t.ForceMeshUpdate();Assert.That(t.isTextOverflowing,Is.False,t.name+": "+t.text);}
                        foreach(var icon in view.GetComponentsInChildren<RawImage>().Where(i=>i.name=="RewardIcon")){Assert.That(icon.texture,Is.Not.Null);Assert.That(icon.raycastTarget,Is.False);}
                        var corners=new Vector3[4];((RectTransform)view.transform).GetWorldCorners(corners);foreach(var c in corners){var point=camera.WorldToScreenPoint(c);Assert.That(point.x,Is.InRange(-1f,width+1f));Assert.That(point.y,Is.InRange(-1f,height+1f));}
                        RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,width+"-"+state+".png"),pixels.EncodeToPNG());
                        records.Add(new JObject{["width"]=width,["state"]=state,["count"]=view.CurrentState.RewardItems.Count,["writes"]=wire.Writes});
                    }
                    Assert.That(View().GetComponentsInChildren<RawImage>().Count(i=>i.name=="RewardIcon"),Is.EqualTo(2));Capture("preview");
                    var separateIcon=View().GetComponentsInChildren<RawImage>().First(i=>i.name=="RewardIcon");separateIcon.gameObject.SetActive(false);
                    Assert.That(View().GetComponentsInChildren<TMP_Text>().Count(t=>t.name=="RewardValue"),Is.EqualTo(2));separateIcon.gameObject.SetActive(true);
                    var frozen=View().CurrentState;wire.Lines=new JArray(Line("CURRENCY","cur_credits",9007199254740991L),Line("CURRENCY","cur_machine_parts",1234567890),Line("CURRENCY","cur_operator_exp",425),Line("CURRENCY","cur_tactical_data",900),Line("FUTURE","cur_credits",12),Line("ITEM","item_test",2),Line("CRYSTAL","premium",300),Line("CURRENCY","future_coin",77));
                    Click("VictoryDetails");int before=wire.Reads;Click("VictoryRefresh");yield return null;Assert.That(wire.Reads-before,Is.EqualTo(2));Click("VictoryDetails");
                    Assert.That(frozen.RewardItems[0].Quantity,Is.EqualTo(186));Assert.That(View().CurrentState.RewardItems.Count,Is.EqualTo(8));Capture("multi-top");
                    var scroll=View().GetComponentInChildren<ScrollRect>();Assert.That(scroll.content.rect.height,Is.GreaterThan(scroll.viewport.rect.height));                    scroll.verticalNormalizedPosition=1;float beforeScroll=scroll.content.anchoredPosition.y;
                    ExecuteEvents.Execute(scroll.gameObject,new PointerEventData(events.GetComponent<EventSystem>()){scrollDelta=new Vector2(0,-12)},ExecuteEvents.scrollHandler);yield return null;
                    Assert.That(scroll.content.anchoredPosition.y,Is.GreaterThan(beforeScroll),"Native scroll input moves the reward list.");
                    scroll.verticalNormalizedPosition=0;yield return null;Capture("multi-bottom");
                    Assert.That(View().GetComponentsInChildren<TMP_Text>().Count(t=>t.name=="MissingRewardArt"),Is.EqualTo(4));
                    // Empty/missing previews precede payment: a verified CLAIMED run
                    // must never be rewound to CLAIMABLE just to exercise another UI case.
                    wire.Receipt=false;wire.Lines=new JArray();yield return owner.ReadRunResultsAsync("run-smoke-1");yield return null;
                    Assert.That(View().GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="VictoryRewardsUnavailable").text,Is.EqualTo("보상 항목이 없습니다."));
                    wire.Missing=true;yield return owner.ReadRunResultsAsync("run-smoke-1");yield return null;
                    Assert.That(View().GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="VictoryRewardsUnavailable").text,Is.EqualTo("보상 내역 확인 필요"));Capture("missing");
                    wire.Missing=false;wire.Receipt=true;wire.Lines=new JArray(Line("CURRENCY","cur_credits",558));yield return owner.ReadRunResultsAsync("run-smoke-1");yield return null;
                    Assert.That(owner.VerifiedRunReceipt,Is.Not.Null);Assert.That(View().CurrentState.RewardItems.Single().Quantity,Is.EqualTo(558));Assert.That(View().CurrentState.RewardStatus,Is.EqualTo("지급 완료"));Assert.That(Button("VictoryClaim").interactable,Is.False);Capture("receipt");
                    var oldRefresh=Button("VictoryRefresh");Click("VictoryReturn");int closedReads=wire.Reads;oldRefresh.onClick.Invoke();Assert.That(wire.Reads,Is.EqualTo(closedReads));Assert.That(wire.Writes,Is.Zero);
                }
                finally{RenderTexture.active=previous;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(co);UnityEngine.Object.DestroyImmediate(events);}
                yield return null;
            }
            File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["scope"]="TEST_ONLY native victory host, no grants/SDK/Store/operational HTTP",["cases"]=records}.ToString());yield return new ExitPlayMode();
        }
    }
}
