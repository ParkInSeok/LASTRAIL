using System;
using System.Collections;
using System.Linq;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using LastRail.Auth;
using LastRail.Battle;

namespace LastRail.Tests.EditMode
{
    public sealed class DepartureBlockTests
    {
        static object Read(int flags)
        {
            var type=typeof(DepartureQuote).Assembly.GetType("LastRail.Presentation.MissionShell.DepartureBlockState");
            Assert.IsNotNull(type,"Missing shared multi-block display projection.");
            return type.GetMethod("FromFlags").Invoke(null,new object[]{flags});
        }
        static string[] Codes(object state)=>((IEnumerable)state.GetType().GetProperty("Reasons").GetValue(state)).Cast<object>().Select(x=>(string)x.GetType().GetProperty("Code").GetValue(x)).ToArray();
        [Test] public void AllReasonsRemainOrderedUntilEachSourceClears()
        {
            // Bit contract: outcome, authentication, storage, unsupported, formation, progress, assets.
            var all=Read(127);Assert.That(Codes(all),Is.EqualTo(new[]{"outcome","authentication","storage","unsupported","formation","progress","assets"}));
            var partial=Read(127&~1&~2&~4);Assert.That(Codes(partial),Is.EqualTo(new[]{"unsupported","formation","progress","assets"}));
            Assert.IsTrue((bool)partial.GetType().GetProperty("HasBlocks").GetValue(partial));
            Assert.That(Codes(all).Length,Is.EqualTo(7),"New observations must not mutate an existing display snapshot.");
            Assert.IsFalse((bool)Read(0).GetType().GetProperty("HasBlocks").GetValue(Read(0)));
        }
        [Test] public void ViewRebindUpdatesReasonsWithoutInvokingCommandsOrKeepingOldCallbacks()
        {
            var root=new GameObject("BlockView",typeof(RectTransform));int oldCalls=0,newCalls=0;
            try{
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var view=DeparturePreparationView.Create(root.transform,font,null,null,null,null,null,null,null,null,null,null,null);
                var bind=typeof(DeparturePreparationView).GetMethod("BindBlockActions");Assert.IsNotNull(bind,"Block action callback binding missing.");
                var render=typeof(DeparturePreparationView).GetMethod("RenderBlocks");Assert.IsNotNull(render);
                bind.Invoke(view,new object[]{new Action<string>(_=>oldCalls++)});
                render.Invoke(view,new[]{Read(127)});
                bind.Invoke(view,new object[]{new Action<string>(_=>newCalls++)});
                render.Invoke(view,new[]{Read(127&~1&~2&~4)});
                Assert.AreEqual(0,oldCalls+newCalls,"Rendering does not execute commands.");
                var buttons=root.GetComponentsInChildren<Button>(true);
                buttons.Single(b=>b.name=="DepartureBlockDetails").onClick.Invoke();
                Assert.That(root.GetComponentsInChildren<TMP_Text>().Any(t=>t.text?.Contains("현재 버전에서는")==true),Is.True);
                buttons.Single(b=>b.name=="ResolveDepartureBlock_formation").onClick.Invoke();
                Assert.AreEqual(0,oldCalls);Assert.AreEqual(1,newCalls);
                view.gameObject.SetActive(false);buttons.Single(b=>b.name=="ResolveDepartureBlock_formation").onClick.Invoke();Assert.AreEqual(1,newCalls);view.gameObject.SetActive(true);
                bind.Invoke(view,new object[]{null});buttons.Single(b=>b.name=="ResolveDepartureBlock_formation").onClick.Invoke();
                Assert.AreEqual(1,newCalls,"Unbound view cannot call previous controller.");
                render.Invoke(view,new[]{Read(127)});view.Render("시험 작전","I","진행 중인 요청이나 작전 기록을 먼저 확인해 주세요.",null,"비용 확인 필요",false,false,false,false,false,"출격");Capture(root);
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        static void Capture(GameObject root)
        {
            var cameraObject=new GameObject("MultiBlockCamera",typeof(Camera));var target=new RenderTexture(941,1672,24);var pixels=new Texture2D(941,1672,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
            try{
                foreach(var child in root.GetComponentsInChildren<Transform>(true))child.gameObject.layer=31;
                var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=836;camera.aspect=941f/1672;camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=1<<31;
                var canvas=root.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;((RectTransform)root.transform).sizeDelta=new Vector2(941,1672);
                Canvas.ForceUpdateCanvases();camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,941,1672),0,0);pixels.Apply();
                var path="D:/Project/LASTRAIL/QA/OperatorInformation-20261019/MultiBlock560/blocks-"+Guid.NewGuid().ToString("N")+".png";
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));System.IO.File.WriteAllBytes(path,pixels.EncodeToPNG());Debug.Log("MultiBlock capture: "+path);
            }finally{RenderTexture.active=previous;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(cameraObject);}
        }
        sealed class LifecycleWire:IRunApiHttpTransport
        {
            public int Calls;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request)
            {
                Calls++;var data=JObject.Parse(System.IO.File.ReadAllText("D:/Project/LASTRAIL/unity-project/QA/Unity1Artifacts/Reports/Server2-NoncombatCases-20260914/result-lost-finish-restored.json"));
                data["runStatus"]="COMPLETED";data["finish"]["outcome"]="VICTORY";
                return Task.FromResult(RunApiHttpResponse.Success(200,data.ToString()));
            }
        }
        sealed class PendingJournal:IAccountActionJournal
        {
            readonly PendingAccountAction action=new PendingAccountAction("account_test","departure-start","preview_test",0,"original_start_key","{\"operationPreviewId\":\"preview_test\"}");
            public PendingAccountAction Read(string account)=>action;public bool Save(PendingAccountAction a)=>throw new InvalidOperationException();public bool Remove(PendingAccountAction a)=>throw new InvalidOperationException();
        }
        sealed class LookupWire:IRunApiHttpTransport
        {
            public int Lookups;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){Assert.AreEqual("/v1/runs/start-result",request.EndpointPath);Lookups++;return Task.FromResult(RunApiHttpResponse.Success(200,"{\"accepted\":true,\"lookupStatus\":\"NOT_FOUND\",\"runId\":null,\"runStatus\":null,\"startResponse\":null,\"errorCode\":null}"));}
        }
        [Test] public void IdleLocalFlowDoesNotSwallowAccountOriginalRequestResolution()
        {
            var wire=new LookupWire();var session=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,new PendingJournal(),()=>session,()=>1);
            var client=new Ce03LocalClient(new Uri("http://127.0.0.1:12345/"),"account_test",wire,new GameApiAccessToken("lst1.test"),raw=>{});
            using var flow=new Ce03LocalDeparture(client,p=>false,(s,p)=>{},o=>Assert.Fail("No entry"));
            var root=new GameObject("AccountResolution",typeof(RectTransform));
            try{
                var deck=root.AddComponent<LobbyCommandDeck>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                typeof(LobbyCommandDeck).GetField("accountState",flags).SetValue(deck,state);typeof(LobbyCommandDeck).GetField("departureDialog",flags).SetValue(deck,true);deck.BindLocalDeparture(flow);
                typeof(LobbyCommandDeck).GetMethod("ResolveDepartureBlock",flags).Invoke(deck,new object[]{"outcome"});
                Assert.AreEqual(1,wire.Lookups);Assert.IsNotNull(state.Pending);Assert.IsFalse(flow.CanStart);
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public async Task TerminalHistoryIsNotAnUncertainDepartureAndProjectionDoesNotFetch()
        {
            var wire=new LifecycleWire();var session=SupportInquiryTests.Session();
            using var state=new AccountScreenRuntime(wire,null,()=>session,()=>1);
            await state.RefreshRunLifecycleAsync("run-smoke-1");Assert.AreEqual(LobbyRunSelectionKind.Terminal,state.RunSelection.Kind);
            var root=new GameObject("BlockOwner",typeof(RectTransform));
            try{
                var deck=root.AddComponent<LobbyCommandDeck>();
                typeof(LobbyCommandDeck).GetField("accountState",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(deck,state);
                var read=typeof(LobbyCommandDeck).GetMethod("CurrentDepartureBlocks",BindingFlags.Instance|BindingFlags.NonPublic);
                int calls=wire.Calls;var display=(DepartureBlockState)read.Invoke(deck,null);
                Assert.That(display.Reasons.Select(x=>x.Code),Does.Not.Contain("outcome"),"A terminal history is not a pending new departure.");
                Assert.AreSame(display,read.Invoke(deck,null));Assert.AreEqual(calls,wire.Calls);
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
