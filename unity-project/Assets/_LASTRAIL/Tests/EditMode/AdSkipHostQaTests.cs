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
    // Full production host/controller composition, server fixture shapes with explicit TEST_ONLY long-quantity variations and memory-only secure-storage stand-in.
    public sealed class AdSkipHostQaTests
    {
        sealed class EmptyRuns:IRunOperationJournal {
            public Task<IReadOnlyList<PendingRunOperation>> ReadAsync(string account)=>Task.FromResult<IReadOnlyList<PendingRunOperation>>(Array.Empty<PendingRunOperation>());
            public Task<bool> PutAsync(PendingRunOperation op)=>throw new InvalidOperationException("No basic writes in purchased QA");
            public Task<bool> ConfirmAsync(PendingRunOperation op,string json)=>throw new InvalidOperationException("No basic writes in purchased QA");
            public Task<bool> RemoveConfirmedAsync(string a,PendingRunKind k,string key)=>Task.FromResult(false);
        }
        const string Supply = "D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-AdSamples-20260914/";
        const string Runs = "D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-NoncombatCases-20260914/";
        internal sealed class Wire : IRunApiHttpTransport
        {
            public readonly bool Combat;
            public readonly JObject Pending, Result, Lifecycle, Preview;
            public string Status = "AD_VERIFIED";
            public int Authorizations;
            public string Entitlement=AdSkipUseTests.Sample("07-owned-available");
            public int Reads, Spins;
            public TaskCompletionSource<RunApiHttpResponse> SpinReply;
            public Wire(bool combat, bool longRows)
            {
                Combat = combat;
                Lifecycle = JObject.Parse(File.ReadAllText(Runs + "result-lost-finish-restored.json"));
                Lifecycle["runStatus"] = "COMPLETED"; Lifecycle["finish"]["outcome"] = "VICTORY";
                Pending = JObject.Parse(AdSkipUseTests.Sample(combat?"12-combat-approved":"08-offline-approved"));
                Result = JObject.Parse(AdSkipUseTests.Sample(combat?"14-combat-spin":"10-offline-spin"));
                if (combat)
                {
                    foreach (var o in new[] { Pending, Result }) { o["runId"] = Lifecycle["runId"].DeepClone(); o["rewardPreviewId"] = Lifecycle["reward"]["rewardPreviewId"].DeepClone(); }
                }
                else
                {
                    Pending["displayMultipliers"] = new JArray(2,3,4);
                    Pending["probabilities"] = CombatAdControllerTests.Sample()["probabilities"].DeepClone();
                }
                Result["multiplier"] = 4;
                var rows = new JArray();
                string[] ids = { "cur_credits", "cur_machine_parts", "cur_operator_exp", "cur_tactical_data" };
                long[] amounts = { longRows ? 9007199254740991L : 3120, 20, 640, 9007199254740991L };
                for (int i=0;i<(longRows?4:3);i++) rows.Add(new JObject { ["kind"]="CURRENCY", ["id"]=ids[i], ["source"]=combat?"RUN_CLEAR":"OFFLINE_REWARD", ["quantity"]=amounts[i], ["balanceAfter"]=amounts[i] });
                Result["rewardLines"] = rows;
                Preview=JObject.Parse(File.ReadAllText(Supply+"BasicPreview01/preview.json"));
                var previewRows=(JArray)rows.DeepClone();
                long[] baseAmounts={longRows?int.MaxValue:780,5,160,int.MaxValue};
                for(int i=0;i<previewRows.Count;i++){previewRows[i]["quantity"]=baseAmounts[i];((JObject)previewRows[i]).Remove("balanceAfter");}
                if(combat)Pending["eligibleBaseRewardLines"]=previewRows.DeepClone();else Pending["baseRewardLines"]=previewRows.DeepClone();
                foreach(var row in previewRows)row["source"]="OFFLINE_REWARD";
                Preview["rewardLines"]=previewRows;
                // The visible basic preview may be older than the newly pinned purchased opportunity.
                foreach(var row in previewRows){long value=(long)row["quantity"];row["quantity"]=value>37?value-37:value+1;}
            }
            public JObject Intent()
            {
                var o=(JObject)Pending.DeepClone();o["status"]=Status;o["canSpin"]=Status=="AD_VERIFIED";
                if(Combat)o["canClaimBasic"]=Status!="CLAIMED";
                o["result"]=Status=="CLAIMED"?Result.DeepClone():null;return o;
            }
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                if(request.EndpointPath=="/v1/ad-skip")return Task.FromResult(Entitlement==null?RunApiHttpResponse.NetworkError("TEST_ONLY query failure"):RunApiHttpResponse.Success(200,Entitlement));
                if(request.EndpointPath.EndsWith("/uses/authorize")){Authorizations++;Assert.That(request.Method,Is.EqualTo("POST"));Assert.That(Authorizations,Is.EqualTo(1));return Task.FromResult(RunApiHttpResponse.Success(200,Intent().ToString()));}
                if(request.Method=="POST" && request.EndpointPath.EndsWith("/spin"))
                {
                    Assert.That(request.BodyJson,Is.Null);Spins++;Assert.That(Spins,Is.EqualTo(1),"Repeated input duplicated spin request");
                    SpinReply=new TaskCompletionSource<RunApiHttpResponse>();return SpinReply.Task;
                }
                Assert.That(request.Method,Is.EqualTo("GET"),"No other writes allowed");Reads++;
                string body=request.EndpointPath.Contains("ad-intents")?Intent().ToString():
                    request.EndpointPath.EndsWith("/lifecycle")?Lifecycle.ToString():
                    request.EndpointPath.Contains("/reward-preview/")?File.ReadAllText(Runs+"result-preview.json"):
                    request.EndpointPath=="/v1/offline-rewards/preview"?Preview.ToString():null;
                return Task.FromResult(RunApiHttpResponse.Success(body==null?404:200,body??"{}"));
            }
            public void Complete() { Status="CLAIMED";SpinReply.SetResult(RunApiHttpResponse.Success(200,Result.ToString())); }
        }

        [UnityTest] public IEnumerator PurchasedHostsUseOneEntryAndPreservePreviewMotionAndReturn()
        {
            yield return new EnterPlayMode();
            string output=Path.GetFullPath("../QA/AdSkipUnityPhase1-20260917/hosts-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(output);
            Debug.Log("AD_SKIP_HOST_OUTPUT="+output);
            var cases=new JArray();
            foreach(bool combat in new[]{false,true}) foreach(int height in new[]{1280,1600}) foreach(bool longRows in new[]{false,true})
                yield return Case(output,combat,height,longRows,cases);
            File.WriteAllText(Path.Combine(output,"verification.json"),new JObject { ["scope"]="Production LobbyCommandDeck and AccountScreenRuntime, memory-only transport/journal. No network/ad SDK/real grants.", ["cases"]=cases }.ToString());
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator UnownedRevokedAndUnknownKeepBasicRewardAndNeverAutomaticallyAdvertise(){
            yield return new EnterPlayMode();string output=Path.GetFullPath("../QA/AdsOffUnity-20260921/Followup/legacy-states-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(output);Debug.Log("AD_SKIP_STATES_OUTPUT="+output);var cases=new JArray();
            foreach(bool combat in new[]{false,true})foreach(int height in new[]{1280,1600})foreach(string state in new[]{"unknown","absent","revoked"}){
                var root=new GameObject("StateHost",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));((RectTransform)root.transform).sizeDelta=new Vector2(720,height);var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
                var events=new GameObject("QaEvents",typeof(EventSystem));var co=new GameObject("QaCamera",typeof(Camera));var camera=co.GetComponent<Camera>();var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var previous=RenderTexture.active;
                var wire=new Wire(combat,false){Entitlement=state=="unknown"?null:AdSkipUseTests.Sample(state=="revoked"?"17-revoked":"03-configured-unowned")};var session=AdSkipUseTests.Session();using var account=new AccountScreenRuntime(wire,new AccountActionJournal(new AdSkipUseTests.Storage()),()=>session,()=>1);using var runs=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),new EmptyRuns(),()=>session,()=>1);account.BindRunRecovery(runs);
                try{
                    camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=720f/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
                    var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var deck=LobbyCommandDeck.Attach(root,font,"TEST_ONLY",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(account);string name=(combat?"combat":"supply")+"-"+height+"-"+state;
                    void Draw(){Canvas.ForceUpdateCanvases();deck.EvaluateLayout();Canvas.ForceUpdateCanvases();camera.Render();}
                    Button Button(string id)=>root.GetComponentsInChildren<Button>().Single(b=>b.name==id);
                    void Click(string id){Draw();var button=Button(id);Assert.That(button.interactable,Is.True,id);var rect=(RectTransform)button.transform;var pointer=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.SameAs(button));ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);}
                    void Capture(string suffix){Draw();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(output,name+suffix+".png"),pixels.EncodeToPNG());}
                    string run=(string)wire.Lifecycle["runId"];if(combat)deck.OpenRunLifecycle(run);else Click("Shortcut3");yield return null;
                    bool eligibility=combat?account.CanClaimRunReward(run):new OfflineSupplyDisplayState(account).CanClaim;Assert.That(eligibility,Is.True,"Read failure cannot lock basic reward");
                    var ad=root.GetComponentsInChildren<Button>(true).Single(b=>b.name==(combat?"VictoryAd":"OpenOfflineRoulette"));Assert.That(ad.gameObject.activeSelf,Is.False,"Unconfigured policy/provider cannot expose new advertising");ad.onClick.Invoke();Assert.That(root.GetComponentInChildren<SharedRoulettePopup>(),Is.Null);
                    var claim=Button(combat?"VictoryClaim":"ClaimOffline");Assert.That(claim.interactable,Is.True);Capture("-host");
                    Assert.That(wire.Authorizations,Is.Zero);Assert.That(wire.Spins,Is.Zero);Assert.That(account.Pending,Is.Null);Assert.That(combat?account.CanClaimRunReward(run):new OfflineSupplyDisplayState(account).CanClaim,Is.True);
                    var displayed=combat?(Component)root.GetComponentInChildren<RunVictoryView>():root.GetComponentInChildren<OfflineSupplyView>();foreach(var text in displayed.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,text.name+": "+text.text);}
                    cases.Add(new JObject{["name"]=name,["sourceEligibility"]=eligibility,["sourceEligibilityBasis"]=combat?"account.CanClaimRunReward":"OfflineSupplyDisplayState.CanClaim",["viewInteractable"]=claim.interactable,["adEntryVisible"]=ad.gameObject.activeSelf,["authorizePosts"]=0,["spinPosts"]=0,["snapshot"]=account.AdSkip.Snapshot?.Status??(account.AdSkip.Snapshot==null?"UNKNOWN":"ABSENT")});
                }finally{RenderTexture.active=previous;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(co);UnityEngine.Object.DestroyImmediate(events);}
                yield return null;
            }
            File.WriteAllText(Path.Combine(output,"verification.json"),new JObject{["cases"]=cases,["scope"]="TEST_ONLY production hosts; no provider or real network"}.ToString());yield return new ExitPlayMode();
        }
        static IEnumerator Case(string output,bool combat,int height,bool longRows,JArray cases)
        {
            string name=(combat?"combat":"supply")+"-"+height+(longRows?"-long":"-normal");
            string folder=Path.Combine(output,name);Directory.CreateDirectory(folder);
            var root=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
            ((RectTransform)root.transform).sizeDelta=new Vector2(720,height);
            var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);
            var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
            var events=new GameObject("QaEvents",typeof(EventSystem));
            var cameraObject=new GameObject("QaCamera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();
            var target=new RenderTexture(720,height,24);var pixels=new Texture2D(720,height,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            var wire=new Wire(combat,longRows);var journal=new AccountActionJournal(new AdSkipUseTests.Storage());
            string run=(string)wire.Lifecycle["runId"];
            var session=new GameApiAuthSessionSnapshot("a","ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddDays(1));
            using var account=new AccountScreenRuntime(wire,journal,()=>session,()=>1);
            using var runs=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),new EmptyRuns(),()=>session,()=>1);account.BindRunRecovery(runs);
            try
            {
                camera.orthographic=true;camera.orthographicSize=height/2f;camera.aspect=720f/height;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
                var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var deck=LobbyCommandDeck.Attach(root,font,"LOCAL QA",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(account);
                string main=combat?"CombatAdMain":"AdMainAction", close=combat?"CombatAdClose":"AdClose", refresh=combat?"CombatAdRefresh":"AdRefresh";
                var clicks=new JArray();var frames=new JArray();var geometry=new JObject();
                Button Button(string id)=>root.GetComponentsInChildren<Button>().Single(b=>b.name==id);
                void Draw(){Canvas.ForceUpdateCanvases();deck.EvaluateLayout();Canvas.ForceUpdateCanvases();camera.Render();}
                void Capture(string id)
                {
                    Draw();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,id+".png"),pixels.EncodeToPNG());
                    if(!id.StartsWith("motion-"))
                    {
                        var nodes=new JArray();
                        foreach(var r in root.GetComponentsInChildren<RectTransform>().Where(t=>t.name=="RoulettePanel"||t.name==main||t.name.StartsWith("RewardCard")||t.name=="RewardQuantity"||t.name=="MoreRewardsHint"||t.name=="RouletteNotice"))
                        {
                            var corners=new Vector3[4];r.GetWorldCorners(corners);var a=RectTransformUtility.WorldToScreenPoint(camera,corners[1]);var b=RectTransformUtility.WorldToScreenPoint(camera,corners[3]);
                            nodes.Add(new JObject{["name"]=r.name,["x"]=a.x,["y"]=height-a.y,["width"]=b.x-a.x,["height"]=a.y-b.y,["text"]=r.GetComponent<TMP_Text>()?.text});
                        }
                        geometry[id]=nodes;
                    }
                }
                void Click(string id,bool enabled=true)
                {
                    Draw();var b=Button(id);if(enabled)Assert.That(b.interactable,Is.True,id);
                    var r=(RectTransform)b.transform;var p=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(r.rect.center))};
                    var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(p,hits);
                    Assert.That(hits.Count,Is.GreaterThan(0),id);Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.SameAs(b),"First hit "+id);
                    clicks.Add(new JObject{["button"]=id,["interactable"]=b.interactable});ExecuteEvents.Execute(b.gameObject,p,ExecuteEvents.pointerClickHandler);
                }
                void Open(){if(combat)Click("VictoryAd");else Click("OpenOfflineRoulette");}
                if(combat)deck.OpenRunLifecycle(run);else Click("Shortcut3");
                yield return null;Capture("00-host");
                Texture[] supplyTextures=null;Rect[] supplyUvs=null;
                if(!combat)
                {
                    var supply=root.GetComponentInChildren<OfflineSupplyView>();
                    var icons=supply.GetComponentsInChildren<RawImage>().Where(x=>x.name=="RewardIcon").ToArray();
                    Assert.That(icons.Length,Is.EqualTo(longRows?4:3));supplyTextures=icons.Select(x=>x.texture).ToArray();supplyUvs=icons.Select(x=>x.uvRect).ToArray();
                    foreach(var value in supply.GetComponentsInChildren<TMP_Text>().Where(t=>t.name=="Value"||t.name=="Name")){value.ForceMeshUpdate();Assert.That(value.isTextOverflowing,Is.False,value.text);}
                    if(longRows)
                    {
                        var scroll=supply.GetComponentInChildren<ScrollRect>();Assert.That(scroll.horizontal,Is.True);
                        var p=new PointerEventData(events.GetComponent<EventSystem>()){scrollDelta=new Vector2(-100,0)};ExecuteEvents.Execute(scroll.gameObject,p,ExecuteEvents.scrollHandler);yield return null;
                        Assert.That(scroll.horizontalNormalizedPosition,Is.GreaterThan(.99f));Capture("00-host-last-card");scroll.horizontalNormalizedPosition=0;
                    }
                }
                Open();yield return null;
                if(!combat)
                {
                    var icons=root.GetComponentInChildren<SharedRoulettePopup>().GetComponentsInChildren<RawImage>().Where(x=>x.name=="RewardIcon").ToArray();
                    for(int i=0;i<icons.Length;i++){Assert.That(icons[i].texture,Is.SameAs(supplyTextures[i]));Assert.That(icons[i].uvRect,Is.EqualTo(supplyUvs[i]));}
                }
                Assert.That(wire.Authorizations,Is.EqualTo(1),"Entry click must authorize without a second popup click");
                for(int i=0;i<5;i++)Click(main,false);
                Assert.That(wire.Spins,Is.EqualTo(1));Capture("04-request-inflight");
                var baseStrings=((JArray)wire.Pending[combat?"eligibleBaseRewardLines":"baseRewardLines"]).Select(r=>r["quantity"].Value<long>().ToString("N0",System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                float began=Time.unscaledTime;wire.Complete();float reveal=-1,wheelReveal=-1;int frame=0,countFrames=0;
                var visual=root.GetComponentInChildren<SharedRouletteVisual>();Assert.That(visual.IsAnimating,Is.True);
                var orbitLabels=visual.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.name.StartsWith("Multiplier")).ToArray();
                var wheel=orbitLabels[0].transform.parent.parent;
                var animatedQuantities=visual.GetComponentsInChildren<TMP_Text>().Where(t=>t.name=="RewardQuantity").ToArray();
                var fixedWidths=animatedQuantities.Select(t=>((RectTransform)t.transform.parent).rect.width).ToArray();
                int checkedFrames=0;float lastCapture=-1;
                while(Time.unscaledTime-began<7.8f)
                {
                    float elapsed=Time.unscaledTime-began;
                    checkedFrames++;
                    for(int i=0;i<animatedQuantities.Length;i++)
                    {
                        var q=animatedQuantities[i];q.ForceMeshUpdate();
                        Assert.That(q.textInfo.lineCount,Is.EqualTo(1),q.text);Assert.That(q.isTextOverflowing,Is.False,q.text);
                        Assert.That(q.GetPreferredValues(q.text,Mathf.Infinity,Mathf.Infinity).x,Is.LessThanOrEqualTo(q.rectTransform.rect.width),q.text);
                        Assert.That(((RectTransform)q.transform.parent).rect.width,Is.EqualTo(fixedWidths[i]));
                    }
                    float labelAngle=orbitLabels.Max(t=>Quaternion.Angle(t.transform.rotation,wheel.rotation));
                    Assert.That(labelAngle,Is.LessThan(.05f),"Orbiting labels must stay readable");
                    var shown=visual.GetComponentsInChildren<TMP_Text>().Where(t=>t.name=="RewardQuantity").Select(t=>t.text).ToArray();
                    bool winner=visual.GetComponentsInChildren<TMP_Text>().Any(t=>t.name=="CommittedMultiplier"&&t.text=="×4");
                    if(elapsed<6f){Assert.That(visual.IsAnimating,Is.True);Assert.That(Button(main).interactable,Is.False);Assert.That(winner,Is.False);Assert.That(shown,Is.EqualTo(baseStrings));Assert.That(visual.GetComponentsInChildren<RouletteButtonGlowGraphic>().Count(g=>g.name=="RewardSpinGlow"),Is.EqualTo(baseStrings.Length));}
                    if(winner&&wheelReveal<0)wheelReveal=elapsed;
                    if(winner&&visual.IsAnimating){countFrames++;Assert.That(Button(main).interactable,Is.False);}
                    if(!visual.IsAnimating&&reveal<0)reveal=elapsed;
                    if(elapsed-lastCapture>=1f/15)
                    {
                    lastCapture=elapsed;if(frame%10==0&&visual.IsAnimating)Click(main,false);
                    // Avoid clicking after reveal: normal confirmation is exercised explicitly below.
                    string file=$"motion-{frame++:D4}";Capture(file);frames.Add(new JObject{["file"]=file+".png",["time"]=elapsed,["maximumLabelAngle"]=labelAngle,["winnerVisible"]=winner,["animating"]=visual.IsAnimating,["confirmEnabled"]=Button(main).interactable,["quantities"]=new JArray(shown)});
                    }
                    yield return null;
                }
                Assert.That(countFrames,Is.GreaterThan(0));Assert.That(wheelReveal,Is.GreaterThanOrEqualTo(6.5f));Assert.That(reveal-wheelReveal,Is.GreaterThanOrEqualTo(.5f));
                Assert.That(visual.IsAnimating,Is.False);Assert.That(Button(main).interactable,Is.True);Assert.That(wire.Spins,Is.EqualTo(1));Capture("05-result");
                var popup=root.GetComponentInChildren<SharedRoulettePopup>();
                var quantities=popup.GetComponentsInChildren<TMP_Text>().Where(t=>t.name=="RewardQuantity").ToArray();
                Assert.That(quantities.Length,Is.EqualTo(longRows?4:3));foreach(var t in quantities){t.ForceMeshUpdate();Assert.That(t.isTextOverflowing,Is.False,t.text);Assert.That(t.fontSize,Is.EqualTo(38));}
                Assert.That(quantities.Select(t=>t.text),Is.EqualTo(((JArray)wire.Result["rewardLines"]).Select(r=>r["quantity"].Value<long>().ToString("N0",System.Globalization.CultureInfo.InvariantCulture))));
                Assert.That(popup.GetComponentsInChildren<Button>().Length,Is.EqualTo(1));
                // Popup overlay must be the first hit even outside the inset frame.
                var outside=new PointerEventData(events.GetComponent<EventSystem>()){position=new Vector2(360,12)};var outsideHits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(outside,outsideHits);
                var outsideTarget=outsideHits[0].gameObject;
                Assert.That(outsideTarget.transform.IsChildOf(popup.transform)||outsideTarget.name=="LobbyDialogShield",Is.True,"Host inputs must stay blocked: "+outsideTarget.name);
                Assert.That(outsideTarget.GetComponentInParent<Button>(),Is.Null);
                if(!longRows)
                {
                    foreach(string element in new[]{"RouletteRoom","RewardEquipmentDeck","CommittedMultiplier","HubTextBloom","RewardIcon"})
                    {
                        var graphics=popup.GetComponentsInChildren<Graphic>().Where(g=>g.name==element).ToArray();
                        if(element=="RewardEquipmentDeck")
                        {
                            var deckNode=popup.GetComponentsInChildren<RectTransform>().Single(t=>t.name==element);
                            graphics=deckNode.GetComponentsInChildren<Graphic>();
                        }
                        Assert.That(graphics.Length,Is.GreaterThan(0),element);
                        foreach(var g in graphics)g.enabled=false;Capture("hide-"+element);
                        foreach(var g in graphics)g.enabled=true;Capture("restore-"+element);
                    }
                }
                if(longRows)
                {
                    var scroll=popup.GetComponentInChildren<ScrollRect>();Assert.That(scroll.horizontal,Is.True);
                    var p=new PointerEventData(events.GetComponent<EventSystem>()){scrollDelta=new Vector2(-100,0)};
                    ExecuteEvents.Execute(scroll.gameObject,p,ExecuteEvents.scrollHandler);yield return null;
                    Assert.That(scroll.horizontalNormalizedPosition,Is.GreaterThan(.99f));Capture("06-last-card");
                }
                var quantityStrings=quantities.Select(t=>t.text).ToArray();
                // Host closure preserves the unacknowledged receipt, then production entry rebuilds the view.
                deck.CloseDialog();yield return null;
                if(combat)deck.OpenRunLifecycle(run);else Click("Shortcut3");
                yield return null;Open();yield return null;Capture("08-reopened-result");
                Assert.That(root.GetComponentInChildren<SharedRouletteVisual>().IsAnimating,Is.False,"Restored receipt must not replay");
                deck.BindAccount(account);deck.BindAccount(account);
                var oldClick=Button(main).onClick;Click(main);oldClick.Invoke();yield return null;Capture("09-final-return");
                Assert.That(journal.Read("a"),Is.Null);Assert.That(wire.Spins,Is.EqualTo(1));
                File.WriteAllText(Path.Combine(folder,"frames.json"),frames.ToString());
                File.WriteAllText(Path.Combine(folder,"geometry.json"),geometry.ToString());
                File.WriteAllText(Path.Combine(folder,"receipt.json"),wire.Result.ToString());
                File.WriteAllText(Path.Combine(folder,"supply-preview.json"),wire.Preview.ToString());
                cases.Add(new JObject{["name"]=name,["nativeClicks"]=clicks,["mockReads"]=wire.Reads,["mockSpinPosts"]=wire.Spins,["wheelRevealSeconds"]=wheelReveal,["confirmationEnabledSeconds"]=reveal,["countFrames"]=countFrames,["checkedEveryAnimationFrame"]=checkedFrames,["maximumLines"]=1,["overflowFrames"]=0,["fixedCardWidths"]=new JArray(fixedWidths),["baseQuantities"]=new JArray(baseStrings),["journalCleared"]=journal.Read("a")==null,["authorizationPosts"]=wire.Authorizations,["quantities"]=new JArray(quantityStrings),["frames"]=frames.Count});
            }
            finally
            {
                RenderTexture.active=previous;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(events);
            }
            yield return null;
        }
    }
}
