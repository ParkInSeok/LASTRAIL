using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.OfflineRewards;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LastRail.Tests.EditMode
{
    public sealed class AccountScreenIntegrationTests
    {
        [Test] public async Task AccountStateUsesAuthenticatedReadWithoutQueryOrBody()
        {
            var wire=new Wire{Reply=r=>Task.FromResult(RunApiHttpResponse.Success(404,"{}"))};var session=SupportInquiryTests.Session();
            using(var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1)){
                var refresh=typeof(AccountScreenRuntime).GetMethod("RefreshAccountAsync");
                Assert.That(refresh,Is.Not.Null,"S01 shared account snapshot must have its own read path.");
                await (Task)refresh.Invoke(state,null);
                var req=wire.Requests.Single();Assert.That(req.Method,Is.EqualTo("GET"));Assert.That(req.EndpointPath,Is.EqualTo("/v1/account/state"));Assert.That(req.BodyJson,Is.Null);
                Assert.That(req.Headers.Any(h=>h.Name=="Authorization"&&h.Value=="Bearer lst1.fixture"),Is.True);
                Assert.That(req.Headers.Any(h=>h.Name=="Idempotency-Key"),Is.False);
            }
        }
        sealed class SecurePort : IAndroidKeystoreNativeStoragePort
        {
            public bool IsAndroidRuntime=>true;public bool IsKeystoreAvailable=>true;public string ProviderName=>"test-only";public bool UsedPlayerPrefsFallback=>false;
            AndroidKeystoreEncryptedTokenPayload saved;
            public AndroidKeystoreEncryptedTokenPayload Encrypt(string s)=>new AndroidKeystoreEncryptedTokenPayload(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(s)),"fixture");
            public string Decrypt(AndroidKeystoreEncryptedTokenPayload p)=>System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(p.CipherTextBase64));
            public bool Save(AndroidKeystoreEncryptedTokenPayload p){saved=p;return true;}public AndroidKeystoreEncryptedTokenPayload Load()=>saved;public bool Clear(){saved=null;return true;}
        }
        [Test] public void JournalReloadPreservesAccountPartitionAndRejectsReplacement()
        {
            var port=new SecurePort();var journal=new AccountActionJournal(port);
            var a=new PendingAccountAction("first","offline","",0,"key_one");var b=new PendingAccountAction("second","gacha","banner",10,"key_two");
            Assert.That(journal.Save(a),Is.True);Assert.That(journal.Save(b),Is.True);
            var reloaded=new AccountActionJournal(port);Assert.That(reloaded.Read("first").Key,Is.EqualTo("key_one"));Assert.That(reloaded.Read("second").Count,Is.EqualTo(10));
            Assert.That(reloaded.Save(new PendingAccountAction("first","offline","",0,"replacement")),Is.False);
            Assert.That(reloaded.Remove(a),Is.True);Assert.That(reloaded.Read("first"),Is.Null);Assert.That(reloaded.Read("second"),Is.Not.Null);
            var body=new JObject{["expectedRevision"]=0,["name"]="Saved draft",["operators"]=FormationWrite.Operators(new[]{"a","b","c","d","e"})}.ToString();
            Assert.That(reloaded.Save(new PendingAccountAction("third","formation-save","preset_a",0,"formation_key",body)),Is.True);
            Assert.That(new AccountActionJournal(port).Read("third").Payload,Is.EqualTo(body));
        }
        sealed class Wire : IRunApiHttpTransport
        {
            public readonly List<RunApiHttpRequest> Requests=new List<RunApiHttpRequest>();
            public Func<RunApiHttpRequest,Task<RunApiHttpResponse>> Reply;
            public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){Requests.Add(r);return Reply(r);}
        }
        sealed class Journal : IAccountActionJournal
        {
            public readonly Dictionary<string,PendingAccountAction> Rows=new Dictionary<string,PendingAccountAction>();
            public bool SaveFails;
            public PendingAccountAction Read(string id)=>Rows.TryGetValue(id,out var a)?a:null;
            public bool Save(PendingAccountAction a){if(SaveFails)return false;Rows[a.Account]=a;return true;}
            public bool Remove(PendingAccountAction a)=>Rows.Remove(a.Account);
        }
        static string Fixture(Type type,string method)=>(string)type.GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        static string Preview=>Fixture(typeof(OfflineRewardPreviewFlowTests),"AvailableJson");
        static Task<RunApiHttpResponse> Ok(string body)=>Task.FromResult(RunApiHttpResponse.Success(200,body));
        const string Wallet="{\"currencyId\":\"cur_crystal\",\"policyVersion\":\"fixture\",\"freeBalance\":12,\"paidBalance\":3,\"totalBalance\":15,\"spendOrder\":[\"FREE\",\"PAID\"],\"ticketExchange\":null,\"purchaseConfigured\":false}";
        static string DisplayGrowth=>new JObject{["balanceVersion"]=1,["economyVersion"]="TEST_ONLY",["economyHash"]=new string('a',64),["growth"]=new JObject{["operatorMaxLevel"]=50,["operatorExpSteps1to10"]=new JArray(),["skillStepsCreditsData"]=new JArray(),["promotionStepsCreditsDataCores"]=new JArray(),["trainCoreStepsCreditsPartsCores"]=new JArray()}}.ToString();
        [Test] public async Task DisplayReentryReusesReadySharedOwnerButExplicitRefreshStillReads()
        {
            var session=SupportInquiryTests.Session();var wire=new Wire{Reply=r=>Ok(r.EndpointPath.Contains("account/state")?AccountStateReadTests.Json():r.EndpointPath.Contains("wallet")?Wallet:DisplayGrowth)};
            using var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);
            await state.EnsureSharedAsync();Assert.That(wire.Requests.Count,Is.EqualTo(3));Assert.That(state.GrowthReady,Is.True);
            await state.EnsureSharedAsync();Assert.That(wire.Requests.Count,Is.EqualTo(3));Assert.That(state.BalanceText(1),Is.EqualTo("123"));
            await state.RefreshSharedAsync();Assert.That(wire.Requests.Count,Is.EqualTo(6));Assert.That(wire.Requests.All(r=>r.Method=="GET"),Is.True);
        }
        [Test] public async Task DisplayReadSharesInFlightAndSessionChangeRejectsOldResult()
        {
            var old=new TaskCompletionSource<RunApiHttpResponse>();var session=SupportInquiryTests.Session();long generation=1;int calls=0;
            var wire=new Wire{Reply=r=>++calls==1?old.Task:Ok(AccountStateReadTests.Json(456))};
            using var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>generation);
            var first=state.EnsureAccountAsync();var same=state.EnsureAccountAsync();Assert.That(calls,Is.EqualTo(1));
            session=SupportInquiryTests.Session();generation++;await state.EnsureAccountAsync();old.SetResult(RunApiHttpResponse.Success(200,AccountStateReadTests.Json(123)));await Task.WhenAll(first,same);
            Assert.That(calls,Is.EqualTo(2));Assert.That(state.Account.Currencies["CREDITS"],Is.EqualTo(456));
        }
        [Test] public async Task FailedDisplayReadIsRetriedInsteadOfReusingStaleAccount()
        {
            bool fail=false;var session=SupportInquiryTests.Session();var wire=new Wire{Reply=r=>fail?Task.FromResult(RunApiHttpResponse.NetworkError("TEST_ONLY")):Ok(AccountStateReadTests.Json())};
            using var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);
            await state.EnsureAccountAsync();fail=true;await state.RefreshAccountAsync();Assert.That(state.AccountIsStale,Is.True);fail=false;
            await state.EnsureAccountAsync();Assert.That(wire.Requests.Count,Is.EqualTo(3));Assert.That(state.AccountStatus,Is.EqualTo(AccountStateReadStatus.Ready));
        }
        static string Key(RunApiHttpRequest r)=>r.Headers.Single(h=>h.Name=="Idempotency-Key").Value;
        [Test] public async Task LobbyFormationShowsPlusOnlyForUnassignedSlotsAndClearsOnUnbind()
        {
            var payload=JObject.Parse(FormationContractTests.S03Json());
            var wire=new Wire{Reply=r=>Ok(payload.ToString())};var session=SupportInquiryTests.Session();
            using var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);
            var root=new GameObject("FormationDisplayTest",typeof(RectTransform));
            ((RectTransform)root.transform).sizeDelta=new Vector2(1600,900);
            var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);
            var safeRect=(RectTransform)safe.transform;safeRect.anchorMin=Vector2.zero;safeRect.anchorMax=Vector2.one;safeRect.sizeDelta=Vector2.zero;
            try
            {
                var deck=LobbyCommandDeck.Attach(root,TMP_Settings.defaultFontAsset,"QA",1,new MissionShellMissionRowState[0],null,null);
                deck.BindAccount(state);await state.RefreshAccountAsync();
                var slots=deck.GetComponentsInChildren<TMP_Text>(true).Where(x=>x.name=="EmptySlot").ToArray();
                Assert.That(slots.Length,Is.EqualTo(5));
                Assert.That(slots.All(x=>x.gameObject.activeSelf&&x.text=="+"),Is.True);
                payload["formation"]["activePresetId"]="preset-a";
                payload["formation"]["activePresetRevision"]=1;
                payload["formation"]["updatedAt"]="2026-11-11T00:00:00Z";
                payload["formation"]["operators"]=new JArray(Enumerable.Range(0,5).Select(i=>new JObject{["slot"]=i,["operatorId"]="missing-portrait-test-"+i}));
                await state.RefreshAccountAsync();
                Assert.That(state.Account.ActiveOperators.Count,Is.EqualTo(5));
                Assert.That(slots.All(x=>!x.gameObject.activeSelf),Is.True,"A missing portrait is not an empty formation slot.");
                deck.gameObject.SetActive(false);deck.gameObject.SetActive(true);deck.BindAccount(state);
                Assert.That(slots.All(x=>!x.gameObject.activeSelf),Is.True);
                deck.BindAccount(null);
                Assert.That(slots.All(x=>x.gameObject.activeSelf&&x.text=="+"),Is.True);
                Assert.That(deck.GetComponentsInChildren<RawImage>().Where(x=>x.name.StartsWith("SquadPortrait")).All(x=>x.texture==null),Is.True);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        // Isolated loopback response fixture. Exercises the actual Unity HTTP adapter,
        // not server authorization/business logic, and never sends real START/claim.
        sealed class DisplayHttpFixture:IDisposable
        {
            readonly System.Net.Sockets.TcpListener listener;
            readonly Task worker;volatile bool stopping;
            public string Url {get;}
            public DisplayHttpFixture()
            {
                listener=new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback,0);listener.Start();
                Url="http://127.0.0.1:"+((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
                worker=Task.Run(async()=>{
                    while(!stopping){
                        System.Net.Sockets.TcpClient client;
                        try{client=await listener.AcceptTcpClientAsync();}catch(System.Net.Sockets.SocketException)when(stopping){break;}catch(ObjectDisposedException)when(stopping){break;}
                        using(client){using var stream=client.GetStream();using var reader=new StreamReader(stream,System.Text.Encoding.ASCII,false,1024,true);
                            string first=await reader.ReadLineAsync();string header;do{header=await reader.ReadLineAsync();}while(!string.IsNullOrEmpty(header));
                            string path=first.Split(' ')[1];string body=Body(path);byte[] bytes=System.Text.Encoding.UTF8.GetBytes(body);
                            byte[] prefix=System.Text.Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: "+bytes.Length+"\r\nConnection: close\r\n\r\n");
                            await stream.WriteAsync(prefix,0,prefix.Length);await stream.WriteAsync(bytes,0,bytes.Length);await stream.FlushAsync();
                        }
                    }
                });
            }
            static string Body(string path)
            {
                if(path=="/v1/account/state")return AccountStateReadTests.Json();
                if(path=="/v1/wallet/crystals")return Wallet;if(path=="/v1/growth/config")return DisplayGrowth;
                if(path=="/v1/trains/loadout")return typeof(TrainLoadoutTests).GetMethod("Snapshot",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{1L}).ToString();
                if(path=="/v1/formation/ultimate")return typeof(UltimateSelectionTests).GetMethod("Read",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{null,0L}).ToString();
                throw new InvalidOperationException("Unexpected fixture route: "+path);
            }
            public void Dispose(){stopping=true;listener.Stop();worker.GetAwaiter().GetResult();}
        }
        [Test] public async Task ActualLoopbackHttpCountsForCurrencyTrainFormationReadCycle()
        {
            using var fixture=new DisplayHttpFixture();var session=SupportInquiryTests.Session();
            var adapter=new UnityWebRequestRunApiHttpTransport(new UnityWebRequestRunApiHttpAdapter(fixture.Url,10));
            var wire=new Wire{Reply=adapter.SendAsync};var rows=new JArray();
            async Task Cycle(AccountScreenRuntime state,bool reuse,string phase){int start=wire.Requests.Count;
                if(reuse)await state.EnsureSharedAsync();else await state.RefreshSharedAsync();
                await Task.WhenAll(reuse?state.EnsureSharedAsync():state.RefreshSharedAsync(),state.RefreshTrainLoadoutAsync());
                await Task.WhenAll(reuse?state.EnsureAccountAsync():state.RefreshAccountAsync(),state.RefreshUltimateAsync());
                Assert.That(state.AccountStatus,Is.EqualTo(AccountStateReadStatus.Ready));Assert.That(state.Wallet.Status,Is.EqualTo(CrystalWalletReadStatus.Ready));Assert.That(state.GrowthReady&&state.TrainLoadoutReady&&state.UltimateReady,Is.True);
                rows.Add(new JObject{["phase"]=phase,["calls"]=new JArray(wire.Requests.Skip(start).Select(r=>new JObject{["method"]=r.Method,["path"]=r.EndpointPath})),["count"]=wire.Requests.Count-start});
            }
            using(var baseline=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1)){await Cycle(baseline,false,"previous_forced_reads");Assert.That((int)rows[0]["count"],Is.EqualTo(9));}
            using(var current=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1)){await Cycle(current,true,"current_first_read");Assert.That((int)rows[1]["count"],Is.EqualTo(5));await Cycle(current,true,"current_valid_reentry");Assert.That((int)rows[2]["count"],Is.EqualTo(2));}
            string dir="D:/Project/LASTRAIL/QA/OperatorInformation-20261019/SceneHttp14";Directory.CreateDirectory(dir);
            File.WriteAllText(dir+"/http-counts.json",new JObject{["scope"]="ACTUAL_UNITY_HTTP_TO_ISOLATED_RESPONSE_FIXTURE_NOT_GAME_SERVER",["externalServerCalls"]=0,["economicCommands"]=0,["fullRunCycleMeasured"]=false,["rows"]=rows}.ToString());
        }

        [Test] public async Task PreviewUsesBearerAndParsesReorderedFields()
        {
            var root=JObject.Parse(Preview);var reversed=new JObject(root.Properties().Reverse().Select(p=>new JProperty(p.Name,p.Value)));
            var wire=new Wire{Reply=r=>Ok(reversed.ToString())};
            var result=await new OfflineRewardHttpClient(wire,new GameApiAccessToken("lst1.fixture")).ReadAsync(OfflineRewardPreviewRequest.Create());
            Assert.That(result.Outcome,Is.EqualTo(OfflineRewardPreviewClientOutcome.Available));
            Assert.That(result.Response.RewardLines.Count,Is.EqualTo(2));
            var req=wire.Requests.Single();Assert.That(req.Method,Is.EqualTo("GET"));Assert.That(req.EndpointPath,Is.EqualTo("/v1/offline-rewards/preview"));Assert.That(req.BodyJson,Is.Null);
            Assert.That(req.Headers.Any(h=>h.Name=="Authorization"&&h.Value=="Bearer lst1.fixture"),Is.True);
        }
        [Test] public async Task NestedAcceptedCannotReplaceMissingRootField()
        {
            var root=JObject.Parse(Preview);root.Remove("accepted");root["extra"]=new JObject{["accepted"]=true};
            var wire=new Wire{Reply=r=>Ok(root.ToString())};
            var result=await new OfflineRewardHttpClient(wire,new GameApiAccessToken("lst1.fixture")).ReadAsync(OfflineRewardPreviewRequest.Create());
            Assert.That(result.Response,Is.Null);
        }
        [Test] public async Task OldSessionWalletIsDiscarded()
        {
            var completion=new TaskCompletionSource<RunApiHttpResponse>();var session=SupportInquiryTests.Session();long generation=1;
            var wire=new Wire{Reply=r=>completion.Task};using(var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>generation)){
                var pending=state.RefreshWalletAsync();session=SupportInquiryTests.Session("other");generation++;state.SynchronizeSession();
                completion.SetResult(RunApiHttpResponse.Success(200,Wallet));await pending;Assert.That(state.Wallet,Is.Null);
            }
        }
        [Test] public async Task PersistenceFailurePreventsMutation()
        {
            var wire=new Wire{Reply=r=>Ok(Preview)};var session=SupportInquiryTests.Session();
            using(var state=new AccountScreenRuntime(wire,new Journal{SaveFails=true},()=>session,()=>1)){
                await state.RefreshOfflineAsync();await state.ClaimOfflineAsync();Assert.That(wire.Requests.Any(r=>r.Method=="POST"),Is.False);
            }
        }
        [Test] public async Task LostReplyThenRestartReusesKeyAndAcceptsSparseServerReceipt()
        {
            var journal=new Journal();var session=SupportInquiryTests.Session();string originalKey=null;
            var wire=new Wire{Reply=r=>{
                if(r.Method=="GET")return Ok(Preview);
                Assert.That(journal.Read(session.AccountId),Is.Not.Null);originalKey=Key(r);
                return Task.FromResult(RunApiHttpResponse.NetworkError("lost"));}};
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){await state.RefreshOfflineAsync();await state.ClaimOfflineAsync();Assert.That(state.Pending,Is.Not.Null);}
            int invalidations=0;
            wire.Reply=r=>{
                if(r.Method=="GET")return Ok(r.EndpointPath=="/v1/account/state"?AccountStateReadTests.Json():r.EndpointPath.Contains("wallet")?Wallet:Preview);
                Assert.That(Key(r),Is.EqualTo(originalKey));Assert.That(r.BodyJson,Is.Null);
                return Ok("{\"accepted\":false,\"claimStatus\":\"DUPLICATE_REPLAY\",\"offlineRewardClaimId\":\"receipt_fixture\",\"rewardTransactionId\":\"transaction_fixture\",\"claimableSeconds\":null,\"calculation\":null,\"serverClaimedAt\":\"2026-09-10T00:00:00.123Z\",\"errorCode\":\"duplicate_replay\"}");};
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                state.AccountDataInvalidated+=()=>invalidations++;await state.ClaimOfflineAsync();
                Assert.That(state.Pending,Is.Null);Assert.That(journal.Read(session.AccountId),Is.Null);Assert.That(invalidations,Is.EqualTo(1));Assert.That(state.Wallet.TotalBalance,Is.EqualTo(15));Assert.That(state.Account.Currencies["CREDITS"],Is.EqualTo(123));
            }
        }
        [Test] public async Task DoubleClickDoesNotSendSecondMutation()
        {
            var reply=new TaskCompletionSource<RunApiHttpResponse>();var session=SupportInquiryTests.Session();
            var wire=new Wire{Reply=r=>r.Method=="GET"?Ok(Preview):reply.Task};
            using(var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1)){
                await state.RefreshOfflineAsync();var first=state.ClaimOfflineAsync();await state.ClaimOfflineAsync();
                Assert.That(wire.Requests.Count(r=>r.Method=="POST"),Is.EqualTo(1));
                reply.SetResult(RunApiHttpResponse.NetworkError("lost"));await first;Assert.That(state.Pending,Is.Not.Null);
            }
        }
        [Test] public async Task OtherAccountCannotReplayPendingAction()
        {
            var journal=new Journal();journal.Save(new PendingAccountAction("account_test","offline","",0,"old_key"));var session=SupportInquiryTests.Session("other");
            var wire=new Wire{Reply=r=>Ok(Preview)};
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){await state.ClaimOfflineAsync();Assert.That(wire.Requests,Is.Empty);Assert.That(journal.Read("account_test"),Is.Not.Null);}
        }
        [Test] public async Task NonSuccessGachaResponseRetainsPendingAction()
        {
            var journal=new Journal();var session=SupportInquiryTests.Session();
            var wire=new Wire{Reply=r=>Task.FromResult(RunApiHttpResponse.Success(503,"{\"accepted\":true,\"resultStatus\":\"ACCEPTED\"}"))};
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){await state.PullAsync("banner_fixture",1);Assert.That(state.LastPull,Is.Null);Assert.That(state.Pending,Is.Not.Null);}
        }
        [Test] public async Task NativePopupAndPageHeaderUseSharedAccountState()
        {
            var session=SupportInquiryTests.Session();var wire=new Wire{Reply=r=>Ok(r.EndpointPath=="/v1/account/state"?AccountStateReadTests.Json():r.EndpointPath.Contains("wallet")?Wallet:Preview)};
            var root=new GameObject("AccountUiTest",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);
            var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var rect=(RectTransform)safe.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
            using(var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1))try{
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset");
                Assert.That(font,Is.Not.Null);
                var deck=LobbyCommandDeck.Attach(root,font,"QA",1,new MissionShellMissionRowState[0],null,null);deck.BindAccount(state);
                await state.RefreshSharedAsync();Assert.That(deck.GetComponentsInChildren<TMP_Text>().Where(t=>t.name=="Balance").Last().text,Is.EqualTo("15"));
                Assert.That(deck.GetComponentsInChildren<TMP_Text>().Where(t=>t.name=="Balance").First().text,Is.EqualTo("7 / 10"));
                deck.GetComponentsInChildren<Button>().Single(b=>b.name=="Shortcut3").onClick.Invoke();
                Assert.That(deck.IsDialogOpen,Is.True);
                var supply=root.GetComponentInChildren<OfflineSupplyView>();Assert.That(supply,Is.Not.Null);
                Assert.That(supply.transform.Find("SupplyRewardViewport/SupplyRewardContent/SupplyReward0/Name").GetComponent<TMP_Text>().text,Is.EqualTo("크레딧"));
                Assert.That(supply.transform.Find("SupplyRewardViewport/SupplyRewardContent/SupplyReward0/Value").GetComponent<TMP_Text>().text,Is.EqualTo("120"));
                Assert.That(root.GetComponentsInChildren<Button>().Single(b=>b.name=="ClaimOffline").interactable,Is.True);
                deck.CloseDialog();deck.OpenPage(LobbyPage.Operators);
                Assert.That(root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="CurrencyValue2").text,Is.EqualTo("15"));
                Assert.That(root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="CurrencyValue1").text,Is.EqualTo("123"));
                wire.Reply=r=>Task.FromResult(RunApiHttpResponse.Success(503,"{\"accepted\":false,\"errorCode\":\"account_state_unavailable\"}"));await state.RefreshAccountAsync();
                Assert.That(root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="CurrencyValue1").text,Is.EqualTo("123 *"));
                Assert.That(root.GetComponentsInChildren<TMP_Text>().Where(t=>t.name=="AccountStateNotice").All(t=>t.text.Contains("이전 정보")),Is.True);
                Capture(root,"account-state-stale-local-fixture.png");
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        static readonly string captureRunName="regression-"+System.Guid.NewGuid().ToString("N");
        static void Capture(GameObject root,string file)
        {
            var cameraObject=new GameObject("AccountCaptureCamera",typeof(Camera));var target=new RenderTexture(720,1280,24);var pixels=new Texture2D(720,1280,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
            try{
                foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=640;camera.aspect=720f/1280;camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=1<<31;
                var canvas=root.GetComponent<Canvas>();if(canvas==null)canvas=root.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);
                Canvas.ForceUpdateCanvases();camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,720,1280),0,0);pixels.Apply();
                // Each test domain writes a fresh run; approved historical captures remain immutable.
                var folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../QA/ScreenIntegration/",captureRunName));System.IO.Directory.CreateDirectory(folder);
                var output=System.IO.Path.Combine(folder,System.IO.Path.GetFileName(file));if(System.IO.File.Exists(output))output=System.IO.Path.Combine(folder,System.Guid.NewGuid().ToString("N")+"-"+System.IO.Path.GetFileName(file));
                System.IO.File.WriteAllBytes(output,pixels.EncodeToPNG());Debug.Log("Regression capture: "+output);
            }finally{RenderTexture.active=previous;cameraObject.GetComponent<Camera>().targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(cameraObject);}
        }
    }
}
