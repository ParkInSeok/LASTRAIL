using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace LastRail.Tests.EditMode
{
    public sealed partial class FormationLocalHttpTests
    {
        sealed class Journal:IAccountActionJournal,IAccountStartReceiptJournal
        {readonly Dictionary<string,PendingAccountAction> rows=new Dictionary<string,PendingAccountAction>();public PendingAccountAction Read(string id)=>rows.TryGetValue(id,out var a)?a:null;public bool Save(PendingAccountAction a){rows[a.Account]=a;return true;}public bool Remove(PendingAccountAction a)=>rows.Remove(a.Account);public bool ConfirmStart(PendingAccountAction a,string result){var old=Read(a.Account);if(old==null||old.Key!=a.Key||old.Payload!=a.Payload)return false;rows[a.Account]=new PendingAccountAction(a.Account,a.Kind,a.Target,a.Count,a.Key,a.Payload,result);return true;}}
        sealed class Wire:IRunApiHttpTransport
        {
            public IRunApiHttpTransport Inner;public string LoseDeparture;public bool LoseSave,LoseClaim,LoseGrowth,LosePull,LoseTrain;public readonly List<RunApiHttpRequest> Requests=new List<RunApiHttpRequest>();
            public async Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){Requests.Add(r);var result=await Inner.SendAsync(r);if(r.EndpointPath==LoseDeparture){LoseDeparture=null;Assert.That(result.StatusCode,Is.EqualTo(200),result.BodyJson);return RunApiHttpResponse.NetworkError("Lost departure reply after commit.");}if(r.Method=="POST"&&((LoseSave&&r.EndpointPath.EndsWith("/save"))||(LoseClaim&&r.EndpointPath.EndsWith("/claim"))||(LoseGrowth&&r.EndpointPath.StartsWith("/v1/operators/"))||(LosePull&&r.EndpointPath.EndsWith("/pulls"))||(LoseTrain&&r.EndpointPath.StartsWith("/v1/trains/")))){LoseSave=false;LoseClaim=false;LoseGrowth=false;LosePull=false;LoseTrain=false;Assert.That(result.StatusCode,Is.EqualTo(200),result.BodyJson);return RunApiHttpResponse.NetworkError("Lost after server commit.");}return result;}
        }
        static async Task<(Wire,GameApiAuthSessionSnapshot)> Setup()
        {
            var fixture=JObject.Parse(File.ReadAllText(Path.Combine(Application.dataPath,"../QA/ScreenIntegration/local-s03-fixture.json")));var url=(string)fixture["url"];
            Assert.That(new Uri(url).Host,Is.EqualTo("127.0.0.1"));var token=new GameApiAccessToken((string)fixture["token"]);
            var wire=new Wire{Inner=new InstalledAdmissionTransport(new UnityWebRequestRunApiHttpTransport(new UnityWebRequestRunApiHttpAdapter(url,10)),new AppBuildSnapshot("1.0.0",100),"android",null)};
            var reset=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/__fixture/reset",null,null),token));Assert.That(reset.StatusCode,Is.EqualTo(200));
            return (wire,new GameApiAuthSessionSnapshot("account_test","ACTIVE",token,DateTime.UtcNow.AddMinutes(5),new GameApiRefreshToken("local-fixture"),DateTime.UtcNow.AddMinutes(10)));
        }
        static string Key(RunApiHttpRequest r)=>r.Headers.Single(h=>h.Name=="Idempotency-Key").Value;
        [Test,Explicit("Requires pinned6afdc84 with migration0029 local memory SQLite harness.")]
        public async Task ActualTrainLoadoutNativeSaveNoopConflictLostReplyMilestonesAndErrors()
        {
            var (wire,session)=await Setup();var journal=new Journal();Func<string,Task<RunApiHttpResponse>> control=path=>wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/__fixture/"+path,null,null),session.AccessToken));
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                await state.RefreshTrainLoadoutAsync();Assert.That(state.TrainLoadoutReady,Is.True,state.TrainLoadoutNotice);Assert.That(state.TrainLoadout.Train,Is.Null);Assert.That(state.CanSaveTrainLoadout,Is.False);
                Assert.That((await control("loadout-enable")).StatusCode,Is.EqualTo(200));await state.RefreshSharedAsync();await state.RefreshTrainLoadoutAsync();Assert.That(state.TrainLoadout.SlotCount,Is.EqualTo(2));Assert.That(state.TrainLoadout.Loadout,Is.Null);
                Assert.That(state.SelectTrainCarriage(0,"car_medical"),Is.False);Assert.That(state.SelectTrainCarriage(0,"unknown_car"),Is.False);Assert.That(state.SelectTrainCarriage(0,"car_cargo"),Is.False);Assert.That(state.TrainLoadout.Turrets.Count,Is.EqualTo(1));
                var root=new GameObject("ActualTrainLoadoutUi",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var rect=(RectTransform)safe.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
                try{
                    var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset");var deck=LobbyCommandDeck.Attach(root,font,"LOCAL HTTP",1,new MissionShellMissionRowState[0],null,null);deck.BindAccount(state);deck.OpenPage(LobbyPage.Train);
                    var deadline=DateTime.UtcNow.AddSeconds(10);while((state.TrainLoadoutLoading||state.AccountStatus==AccountStateReadStatus.Loading)&&DateTime.UtcNow<deadline)await Task.Delay(10);
                    root.GetComponentsInChildren<Button>().Single(b=>b.name=="Category1").onClick.Invoke();
                    int combat=state.TrainLoadout.Carriages.ToList().FindIndex(c=>(string)c["carriageId"]=="car_combat"),gun=state.TrainLoadout.Carriages.ToList().FindIndex(c=>(string)c["carriageId"]=="car_gun");
                    root.GetComponentsInChildren<Button>().Single(b=>b.name=="TrainLoadoutSlot0").onClick.Invoke();root.GetComponentsInChildren<Button>().Single(b=>b.name=="InstallOwnedCarriage"+combat).onClick.Invoke();
                    root.GetComponentsInChildren<Button>().Single(b=>b.name=="TrainLoadoutSlot1").onClick.Invoke();root.GetComponentsInChildren<Button>().Single(b=>b.name=="InstallOwnedCarriage"+gun).onClick.Invoke();Assert.That(state.CanSaveTrainLoadout,Is.True);
                    typeof(AccountScreenIntegrationTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root,"train-loadout-pinned-local-http.png"});
                    wire.LoseTrain=true;root.GetComponentsInChildren<Button>().Single(b=>b.name=="SaveTrainLoadout").onClick.Invoke();deadline=DateTime.UtcNow.AddSeconds(10);while((state.Busy||state.TrainLoadoutLoading||state.AccountStatus==AccountStateReadStatus.Loading)&&DateTime.UtcNow<deadline)await Task.Delay(10);Assert.That(state.Pending?.Kind,Is.EqualTo("train-loadout"));
                }finally{UnityEngine.Object.DestroyImmediate(root);}
            }
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                await state.RetryPendingAsync();Assert.That(state.Pending,Is.Null,state.ActionNotice);Assert.That(state.TrainDraft.Take(2),Is.EqualTo(new[]{"car_combat","car_gun"}));Assert.That(state.TrainLoadout.Revision,Is.EqualTo(1));
                var requests=wire.Requests.Where(r=>r.Method=="POST"&&r.EndpointPath=="/v1/trains/loadout").ToArray();Assert.That(requests.Length,Is.EqualTo(2));Assert.That(Key(requests[0]),Is.EqualTo(Key(requests[1])));Assert.That(requests[0].BodyJson,Is.EqualTo(requests[1].BodyJson));
                var timestamp=state.TrainLoadout.UpdatedAt;await state.SaveTrainLoadoutAsync();Assert.That(state.LastTrainLoadoutReceipt.Changed,Is.False);Assert.That(state.TrainLoadout.Revision,Is.EqualTo(1));Assert.That(state.TrainLoadout.UpdatedAt,Is.EqualTo(timestamp));
                var draft=state.TrainDraft;Assert.That(state.TrainDraftDirty,Is.False);
                var external=TrainLoadoutWrite.Selection("train_local",new[]{"car_gun","car_combat"},2);external["expectedRevision"]=1;
                var reply=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/v1/trains/loadout",external.ToString(),new[]{new RunApiHttpHeader("Idempotency-Key",Guid.NewGuid().ToString("N"))}),session.AccessToken));Assert.That(reply.StatusCode,Is.EqualTo(200));
                await state.SaveTrainLoadoutAsync();Assert.That(state.ActionErrorCode,Is.EqualTo("train_loadout_revision_conflict"));Assert.That(state.TrainDraft,Is.EqualTo(draft));Assert.That(state.TrainLoadout.Revision,Is.EqualTo(2));await state.SaveTrainLoadoutAsync();Assert.That(state.TrainLoadout.Revision,Is.EqualTo(3));
                Assert.That((await control("loadout-water")).StatusCode,Is.EqualTo(200));await state.RefreshTrainLoadoutAsync();Assert.That(state.TrainLoadout.SlotCount,Is.EqualTo(3));Assert.That(state.TrainLoadout.LoadoutError,Is.EqualTo("train_slot_count_invalid"));Assert.That(state.TrainLoadout.Loadout,Is.Not.Null);Assert.That(state.CanSaveTrainLoadout,Is.False);Assert.That(state.SelectTrainCarriage(2,"car_cargo"),Is.True);await state.SaveTrainLoadoutAsync();Assert.That(state.TrainLoadout.Revision,Is.EqualTo(4));
                Assert.That((await control("loadout-night")).StatusCode,Is.EqualTo(200));await state.RefreshTrainLoadoutAsync();Assert.That(state.TrainLoadout.SlotCount,Is.EqualTo(4));Assert.That(state.SelectTrainCarriage(3,"car_defense"),Is.True);await state.SaveTrainLoadoutAsync();Assert.That(state.TrainLoadout.Revision,Is.EqualTo(5));Assert.That(state.TrainLoadout.LoadoutError,Is.Null);
                Assert.That((await control("loadout-open")).StatusCode,Is.EqualTo(200));await state.RefreshTrainLoadoutAsync();Assert.That(state.TrainLoadout.OpenRun,Is.True);Assert.That(state.CanSaveTrainLoadout,Is.False);Assert.That(state.TrainLoadout.Carriages.OfType<JObject>().Single(c=>(string)c["carriageId"]=="car_combat")["installable"].Value<bool>(),Is.True);
                Assert.That((await control("loadout-inconsistent")).StatusCode,Is.EqualTo(200));await state.RefreshTrainLoadoutAsync();Assert.That(state.TrainLoadout.ProgressError,Is.EqualTo("story_progress_inconsistent"));Assert.That(state.TrainLoadout.SlotCount,Is.Null);Assert.That(state.TrainLoadout.Loadout,Is.Not.Null);Assert.That(state.CanEditTrain,Is.False);Assert.That(state.Account.Currencies["CREDITS"],Is.EqualTo(5000));
            }
        }
        [Test,Explicit("Requires pinned567191d local memory SQLite harness in s05 mode.")]
        public async Task ActualTrainGrowthNativeButtonLostReplyHoldingsAndCap()
        {
            var (wire,session)=await Setup();var journal=new Journal();
            Func<string,Task<RunApiHttpResponse>> control=path=>wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/__fixture/"+path,null,null),session.AccessToken));
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                await state.RefreshSharedAsync();Assert.That(state.Account.Train,Is.Null);Assert.That(state.CanGrow("growth-train","train_local"),Is.False);
                Assert.That((await control("s04-enable")).StatusCode,Is.EqualTo(200));await state.RefreshSharedAsync();Assert.That(state.CanGrow("growth-train","not_owned"),Is.False);
                var cost=state.Growth.Cost("growth-train","train_local",state.Account);Assert.That(cost["CREDITS"],Is.EqualTo(200));Assert.That(cost["MACHINE_PARTS"],Is.EqualTo(100));
                var root=new GameObject("ActualTrainUi",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var rect=(RectTransform)safe.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
                try{
                    var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset");var deck=LobbyCommandDeck.Attach(root,font,"LOCAL HTTP",1,new MissionShellMissionRowState[0],null,null);deck.BindAccount(state);deck.OpenPage(LobbyPage.Train);
                    var deadline=DateTime.UtcNow.AddSeconds(10);while((state.AccountStatus==AccountStateReadStatus.Loading||!state.GrowthReady)&&DateTime.UtcNow<deadline)await Task.Delay(10);
                    Assert.That(root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="TrainGrowthCost").text,Does.Contain("기계 부품 100"));
                    root.GetComponentsInChildren<Button>().Single(b=>b.name=="Category2").onClick.Invoke();Assert.That(root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="TrainHolding0").text,Is.EqualTo("car_local · Lv.3"));
                    root.GetComponentsInChildren<Button>().Single(b=>b.name=="Category3").onClick.Invoke();Assert.That(root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="TrainHolding0").text,Is.EqualTo("turret_local · Lv.2"));
                    root.GetComponentsInChildren<Button>().Single(b=>b.name=="Category0").onClick.Invoke();
                    typeof(AccountScreenIntegrationTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root,"train-growth-pinned-local-http.png"});
                    wire.LoseTrain=true;root.GetComponentsInChildren<Button>().Single(b=>b.name=="UpgradeTrain").onClick.Invoke();root.GetComponentsInChildren<Button>().Single(b=>b.name=="ConfirmCoreAction").onClick.Invoke();
                    deadline=DateTime.UtcNow.AddSeconds(10);while((state.Busy||state.AccountStatus==AccountStateReadStatus.Loading)&&DateTime.UtcNow<deadline)await Task.Delay(10);
                    Assert.That(state.Pending?.Kind,Is.EqualTo("growth-train"));
                }finally{UnityEngine.Object.DestroyImmediate(root);}
            }
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                await state.RefreshSharedAsync();await state.RetryPendingAsync();Assert.That(state.Pending,Is.Null,state.ActionNotice);Assert.That(state.Account.Train.Number("level"),Is.EqualTo(2));Assert.That(state.Account.Currencies["CREDITS"],Is.EqualTo(4800));Assert.That(state.Account.Currencies["MACHINE_PARTS"],Is.EqualTo(900));
                var requests=wire.Requests.Where(r=>r.Method=="POST"&&r.EndpointPath.StartsWith("/v1/trains/")&&r.EndpointPath.EndsWith("/level-up")).ToArray();Assert.That(requests.Length,Is.EqualTo(2));Assert.That(Key(requests[0]),Is.EqualTo(Key(requests[1])));Assert.That(requests[0].BodyJson,Is.EqualTo(requests[1].BodyJson));
                await state.GrowAsync("growth-train","train_local");Assert.That(state.Account.Train.Number("level"),Is.EqualTo(3));Assert.That(state.Account.Currencies["CREDITS"],Is.EqualTo(4500));Assert.That(state.Account.Currencies["MACHINE_PARTS"],Is.EqualTo(750));
                Assert.That((await control("s04-cap")).StatusCode,Is.EqualTo(200));await state.RefreshSharedAsync();Assert.That(state.CanGrow("growth-train","train_local"),Is.False);
            }
        }
        [Test,Explicit("Requires pinned567191d S05 local memory SQLite harness.")]
        public async Task ActualRecruitmentEmptyNativePullLostReplySparseRecoveryAndEndedHistory()
        {
            var (wire,session)=await Setup();var journal=new Journal();
            Func<string,Task<RunApiHttpResponse>> control=path=>wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/__fixture/"+path,null,null),session.AccessToken));
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                await state.RefreshSharedAsync();await state.RefreshBannersAsync();Assert.That(state.BannersReady,Is.True,state.RecruitmentNotice);Assert.That(state.Banners,Is.Empty);Assert.That(state.CanRecruit(1),Is.False);
                Assert.That((await control("s05-enable")).StatusCode,Is.EqualTo(200));await state.RefreshSharedAsync();await state.RefreshBannersAsync();Assert.That(state.Banners.Count,Is.EqualTo(2));await state.SelectBannerAsync("banner_local_a");Assert.That(state.SelectedBanner.Price(10),Is.EqualTo(10));Assert.That((double)state.SelectedBanner.Probabilities[0]["baseProbability"],Is.EqualTo(1));
                wire.LosePull=true;await state.RecruitAsync(1);Assert.That(state.Pending?.Kind,Is.EqualTo("gacha"));
            }
            Assert.That((await control("s05-sparse")).StatusCode,Is.EqualTo(200));
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                await state.RefreshSharedAsync();await state.RetryPendingAsync();Assert.That(state.Pending,Is.Null,state.ActionNotice+state.RecruitmentNotice);Assert.That(state.SelectedBatch.Complete,Is.True);Assert.That(state.SelectedBatch.Version,Is.Null);Assert.That(state.SelectedBatch.ResultLine(1),Does.Contain("× 7"));Assert.That(state.RecruitmentTickets,Is.EqualTo(39));
                var requests=wire.Requests.Where(r=>r.EndpointPath.EndsWith("/pulls")).ToArray();Assert.That(requests.Length,Is.EqualTo(2));Assert.That(Key(requests[0]),Is.EqualTo(Key(requests[1])));Assert.That(requests[0].BodyJson,Is.EqualTo(requests[1].BodyJson));
                var root=new GameObject("ActualRecruitmentUi",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var rect=(RectTransform)safe.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
                try{
                    var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset");var deck=LobbyCommandDeck.Attach(root,font,"LOCAL HTTP",1,new MissionShellMissionRowState[0],null,null);deck.BindAccount(state);deck.OpenPage(LobbyPage.Recruit);
                    var deadline=DateTime.UtcNow.AddSeconds(10);while((state.BannersLoading||state.AccountStatus==AccountStateReadStatus.Loading||state.DetailLoading)&&DateTime.UtcNow<deadline)await Task.Delay(10);
                    Assert.That(root.GetComponentsInChildren<Button>().Single(b=>b.name=="RecruitTen").interactable,Is.True,state.RecruitmentNotice);
                    typeof(AccountScreenIntegrationTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root,"recruitment-pinned-local-http.png"});
                    root.GetComponentsInChildren<Button>().Single(b=>b.name=="RecruitTen").onClick.Invoke();root.GetComponentsInChildren<Button>().Single(b=>b.name=="ConfirmCoreAction").onClick.Invoke();
                    deadline=DateTime.UtcNow.AddSeconds(10);while((state.Busy||state.AccountStatus==AccountStateReadStatus.Loading||state.BannersLoading||state.DetailLoading)&&DateTime.UtcNow<deadline)await Task.Delay(10);
                    Assert.That(state.SelectedBatch?.Count,Is.EqualTo(10),state.ActionNotice+state.RecruitmentNotice);Assert.That(state.RecruitmentTickets,Is.EqualTo(29));Assert.That(state.Account.Items.Single(r=>r.Text("itemId")=="mat_scrap").Number("quantity"),Is.EqualTo(77));
                    string batch=state.SelectedBatch.Id;Assert.That((await control("s05-damage")).StatusCode,Is.EqualTo(200));await state.RefreshBatchAsync(batch);Assert.That(state.SelectedBatch.Complete,Is.False);Assert.That(state.SelectedBatch.Version,Is.Null);Assert.That(state.SelectedBatch.Missing.Select(t=>(int)t),Is.EqualTo(new[]{2}));Assert.That(state.SelectedBatch.Unknown.Select(t=>(int)t),Is.EqualTo(new[]{1}));Assert.That(state.SelectedBatch.ResultLine(1),Does.Contain("수량 확인 필요"));
                    typeof(AccountScreenIntegrationTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root,"recruitment-incomplete-local-http.png"});
                    await state.RefreshHistoryAsync();Assert.That(state.History.Count,Is.EqualTo(2));await state.RefreshBannersAsync();Assert.That(state.Banners,Is.Empty);await state.SelectBannerAsync("banner_local_a");Assert.That(state.SelectedBanner.Availability,Is.EqualTo("ENDED"));Assert.That(state.CanRecruit(1),Is.False);await state.RefreshBatchAsync(batch);Assert.That(state.SelectedBatch.Id,Is.EqualTo(batch));await state.RefreshSharedAsync();Assert.That(state.RecruitmentTickets,Is.EqualTo(29));
                    Assert.That(wire.Requests.Count(r=>r.EndpointPath.EndsWith("/pulls")),Is.EqualTo(3));
                }finally{UnityEngine.Object.DestroyImmediate(root);}
            }
        }
        [Test,Explicit("Requires pinned9c68601 local memory SQLite harness.")]
        public async Task ActualCoreReplacementRefundAndSyncRecoverWithoutDuplicateMutation()
        {
            var (wire,session)=await Setup();var journal=new Journal();
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                await state.RefreshSharedAsync();Assert.That(state.CanGrow("growth-sync","all"),Is.False);
                Assert.That(state.CanReplaceCore(2,"op_kyle"),Is.False);Assert.That(state.CanReplaceCore(2,"not_owned"),Is.False);
                var root=new GameObject("ActualCoreUi",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var rect=(RectTransform)safe.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
                try{
                    var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset");
                    var deck=LobbyCommandDeck.Attach(root,font,"LOCAL HTTP",1,new MissionShellMissionRowState[0],null,null);deck.BindAccount(state);deck.OpenPage(LobbyPage.Operators);
                    int index=state.Account.Operators.ToList().FindIndex(o=>o.Text("operatorId")=="op_reina");root.GetComponentsInChildren<Button>().Single(b=>b.name=="SelectOwnedOperator"+index).onClick.Invoke();
                    root.GetComponentsInChildren<Button>().Single(b=>b.name=="ManageTrainingCore").onClick.Invoke();
                    Assert.That(root.GetComponentsInChildren<Button>().Single(b=>b.name=="SyncTrainingCore").interactable,Is.False);
                    typeof(AccountScreenIntegrationTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root,"core-management-pinned-local-http.png"});
                    root.GetComponentsInChildren<Button>().Single(b=>b.name=="ReplaceCore2").onClick.Invoke();
                    Assert.That(root.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("120 전액 환급")),Is.True);
                    wire.LoseGrowth=true;root.GetComponentsInChildren<Button>().Single(b=>b.name=="ConfirmCoreAction").onClick.Invoke();
                    var deadline=DateTime.UtcNow.AddSeconds(10);while(state.Busy&&DateTime.UtcNow<deadline)await Task.Delay(10);
                    Assert.That(state.Pending?.Kind,Is.EqualTo("growth-replace"));Assert.That(state.Account.Currencies["OPERATOR_EXP"],Is.EqualTo(1120));
                }finally{UnityEngine.Object.DestroyImmediate(root);}
            }
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                await state.RefreshSharedAsync();await state.RetryPendingAsync();Assert.That(state.Pending,Is.Null,state.ActionNotice);
                Assert.That(state.Account.Currencies["OPERATOR_EXP"],Is.EqualTo(1120));var replaced=state.Account.TrainingCoreSlots.Single();Assert.That(replaced.Text("operatorId"),Is.EqualTo("op_reina"));Assert.That(replaced.Number("trainedLevel"),Is.EqualTo(1));Assert.That(replaced.Number("investedOperatorExp"),Is.Zero);
                var requests=wire.Requests.Where(r=>r.EndpointPath.EndsWith("/replace")).ToArray();Assert.That(Key(requests[0]),Is.EqualTo(Key(requests[1])));Assert.That(requests[0].BodyJson,Is.EqualTo(requests[1].BodyJson));
                foreach(var pair in new[]{(0,"op_kyle"),(1,"op_eve"),(3,"op_sera"),(4,"op_mira")})await state.ReplaceCoreAsync(pair.Item1,pair.Item2);
                Assert.That(state.Account.TrainingCoreSlots.Count,Is.EqualTo(5));Assert.That(state.Account.Currencies["OPERATOR_EXP"],Is.EqualTo(1120));Assert.That(state.CanGrow("growth-sync","all"),Is.True);
                wire.LoseGrowth=true;await state.GrowAsync("growth-sync","all");Assert.That(state.Pending?.Kind,Is.EqualTo("growth-sync"));
            }
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                await state.RefreshSharedAsync();await state.RetryPendingAsync();Assert.That(state.Pending,Is.Null,state.ActionNotice);
                Assert.That(state.Account.Currencies["OPERATOR_EXP"],Is.EqualTo(50));Assert.That(state.Account.TrainingCoreSlots.OrderBy(s=>s.Number("slot")).Select(s=>s.Number("trainedLevel")),Is.EqualTo(new long[]{5,5,5,5,4}));
                Assert.That(state.Account.TrainingCoreSlots.Sum(s=>s.Number("investedOperatorExp")),Is.EqualTo(1070));Assert.That(state.CanGrow("growth-sync","all"),Is.False);
                var requests=wire.Requests.Where(r=>r.EndpointPath.EndsWith("/sync-up")).ToArray();Assert.That(requests.Length,Is.EqualTo(2));Assert.That(Key(requests[0]),Is.EqualTo(Key(requests[1])));Assert.That(requests[0].BodyJson,Is.EqualTo(requests[1].BodyJson));
            }
        }
        [Test,Explicit("Requires pinned9c68601 local memory SQLite harness.")]
        public async Task ActualFormationSaveActivateConflictLostReplyAndNativeUi()
        {
            var (wire,session)=await Setup();var journal=new Journal();string preset;
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                await state.RefreshSharedAsync();Assert.That(state.AccountStatus,Is.EqualTo(AccountStateReadStatus.Ready),state.AccountErrorCode);Assert.That(state.Account.ActivationSupported,Is.True);
                var edit=state.Formation;var owned=new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"};for(int i=0;i<5;i++)Assert.That(edit.Select(i,owned[i]),Is.True);
                edit.Rename("Alpha");Assert.That(edit.CanActivate,Is.False);await edit.SaveAsync();Assert.That(state.LastFormationReceipt?.Kind,Is.EqualTo("SAVE"),state.ActionNotice);preset=edit.PresetId;
                Assert.That(state.Account.ActivePresetId,Is.Null);Assert.That(edit.CanActivate,Is.True);await edit.ActivateAsync();Assert.That(state.Account.ActivePresetId,Is.EqualTo(preset));Assert.That(state.Account.ActivePresetRevision,Is.EqualTo(1));
                edit.Select(0,"op_reina");var draft=edit.Selection;
                var external=new RunApiHttpRequest("POST","/v1/formation/presets/"+preset+"/save",new JObject{["expectedRevision"]=1,["name"]="Other editor",["operators"]=FormationWrite.Operators(owned)}.ToString(),new[]{new RunApiHttpHeader("Idempotency-Key",Guid.NewGuid().ToString("N"))});
                var conflictWriter=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(external,session.AccessToken));Assert.That(conflictWriter.StatusCode,Is.EqualTo(200),conflictWriter.BodyJson);
                await edit.SaveAsync();Assert.That(state.ActionErrorCode,Is.EqualTo("preset_revision_conflict"));Assert.That(edit.Selection,Is.EqualTo(draft));Assert.That(edit.ExpectedRevision,Is.EqualTo(2));Assert.That(edit.Dirty,Is.True);
                await edit.SaveAsync();Assert.That(edit.ExpectedRevision,Is.EqualTo(3));Assert.That(state.Account.ActivePresetRevision,Is.EqualTo(1));Assert.That(state.Account.ActiveOperators.First(o=>o.Number("slot")==0).Text("operatorId"),Is.EqualTo("op_kyle"));
                await edit.ActivateAsync();Assert.That(state.Account.ActivePresetRevision,Is.EqualTo(3));Assert.That(state.Account.ActiveOperators.First(o=>o.Number("slot")==0).Text("operatorId"),Is.EqualTo("op_reina"));
                edit.Rename("Recovered squad");wire.LoseSave=true;await edit.SaveAsync();Assert.That(state.Pending?.Kind,Is.EqualTo("formation-save"));
            }
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                await state.RefreshSharedAsync();await state.Formation.RetryAsync();Assert.That(state.Pending,Is.Null);Assert.That(state.Formation.ExpectedRevision,Is.EqualTo(4));Assert.That(state.Account.ActivePresetRevision,Is.EqualTo(3));
                var saves=wire.Requests.Where(r=>r.Method=="POST"&&r.EndpointPath.EndsWith("/save")).ToArray();Assert.That(Key(saves[saves.Length-1]),Is.EqualTo(Key(saves[saves.Length-2])));Assert.That(saves[saves.Length-1].BodyJson,Is.EqualTo(saves[saves.Length-2].BodyJson));
                var root=new GameObject("ActualFormationUi",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var rect=(RectTransform)safe.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
                try{
                    var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset");
                    var deck=LobbyCommandDeck.Attach(root,font,"LOCAL HTTP",1,new MissionShellMissionRowState[0],null,null);deck.BindAccount(state);deck.OpenPage(LobbyPage.Formation);
                    var readDeadline=DateTime.UtcNow.AddSeconds(10);while((state.UltimateLoading||state.AccountStatus==AccountStateReadStatus.Loading)&&DateTime.UtcNow<readDeadline)await Task.Delay(10);
                    var activate=root.GetComponentsInChildren<Button>().Single(b=>b.name=="ActivateFormation");Assert.That(activate.interactable,Is.True);activate.onClick.Invoke();
                    var deadline=DateTime.UtcNow.AddSeconds(10);while((state.Busy||state.AccountStatus==AccountStateReadStatus.Loading)&&DateTime.UtcNow<deadline)await Task.Delay(10);
                    Assert.That(state.Account.ActivePresetRevision,Is.EqualTo(4),state.ActionNotice);Assert.That(root.GetComponentsInChildren<TMP_Text>().Any(t=>t.name=="OwnedName"&&t.text=="Kyle"),Is.True);
                    typeof(AccountScreenIntegrationTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root,"formation-pinned-local-http.png"});
                    deck.Pages.Close();Assert.That(root.GetComponentsInChildren<TMP_Text>().Any(t=>t.name=="EmptySlot"&&t.text=="Reina"),Is.True);
                }finally{UnityEngine.Object.DestroyImmediate(root);}
            }
        }
        [Test,Explicit("Requires pinned9c68601 local memory SQLite harness.")]
        public async Task ActualOfflineReplayPreservesOriginalEconomyAfterUnbinding()
        {
            var (wire,session)=await Setup();var journal=new Journal();
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                await state.RefreshOfflineAsync();Assert.That(state.Offline,Is.Not.Null,state.OfflineNotice);var policy=state.Offline.EconomyMetadata;
                Assert.That(policy.EconomyVersion,Is.EqualTo("act1.economy.2026-09-09.001"));Assert.That(policy.EconomyHash.Length,Is.EqualTo(64));
                wire.LoseClaim=true;await state.ClaimOfflineAsync();Assert.That(state.Pending?.Kind,Is.EqualTo("offline"));
                var unbind=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/__fixture/unbind",null,null),session.AccessToken));Assert.That(unbind.StatusCode,Is.EqualTo(200));
                await state.RetryPendingAsync();Assert.That(state.Pending,Is.Null,state.ActionNotice);Assert.That(state.LastOfflineReceipt.Confirmed,Is.True);
                Assert.That(state.LastOfflineReceipt.EconomyMetadata.EconomyVersion,Is.EqualTo(policy.EconomyVersion));Assert.That(state.LastOfflineReceipt.EconomyMetadata.EconomyHash,Is.EqualTo(policy.EconomyHash));Assert.That(state.Offline.EconomyMetadata.EconomyVersion,Is.Null);
                var claims=wire.Requests.Where(r=>r.Method=="POST"&&r.EndpointPath.EndsWith("/claim")).ToArray();Assert.That(Key(claims[0]),Is.EqualTo(Key(claims[1])));
            }
        }
        [Test,Explicit("Requires pinned9c68601 local memory SQLite harness.")]
        public async Task ActualGrowthCostsLostReplySkillPromotionAndNativeButton()
        {
            var (wire,session)=await Setup();var journal=new Journal();
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                await state.RefreshSharedAsync();Assert.That(state.GrowthReady,Is.True,state.GrowthNotice);
                Assert.That(state.Growth.Cost("growth-core","2",state.Account)["OPERATOR_EXP"],Is.EqualTo(60));
                wire.LoseGrowth=true;await state.GrowAsync("growth-core","2");Assert.That(state.Pending?.Kind,Is.EqualTo("growth-core"));Assert.That(state.CanGrow("growth-core","2"),Is.False);
            }
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                await state.RefreshSharedAsync();await state.RetryPendingAsync();Assert.That(state.Pending,Is.Null,state.ActionNotice);Assert.That(state.LastGrowthReceipt,Is.Not.Null);
                Assert.That(state.Account.Currencies["OPERATOR_EXP"],Is.EqualTo(940));Assert.That(state.Account.TrainingCoreSlots.Single().Number("trainedLevel"),Is.EqualTo(4));
                var posts=wire.Requests.Where(r=>r.Method=="POST"&&r.EndpointPath.StartsWith("/v1/operators/")).ToArray();Assert.That(Key(posts[0]),Is.EqualTo(Key(posts[1])));Assert.That(posts[0].BodyJson,Is.EqualTo(posts[1].BodyJson));
                await state.GrowAsync("growth-skill","op_kyle");Assert.That(state.LastGrowthReceipt,Is.Not.Null,state.ActionNotice);Assert.That(state.Account.Operators.Single(o=>o.Text("operatorId")=="op_kyle").Number("skillTraining"),Is.EqualTo(2));
                await state.GrowAsync("growth-promotion","op_kyle");Assert.That(state.LastGrowthReceipt,Is.Not.Null,state.ActionNotice);Assert.That(state.Account.Operators.Single(o=>o.Text("operatorId")=="op_kyle").Number("promotion"),Is.EqualTo(1));
                Assert.That(state.Account.Currencies["CREDITS"],Is.EqualTo(4200));Assert.That(state.Account.Currencies["TACTICAL_DATA"],Is.EqualTo(4600));Assert.That(state.Account.Currencies["SPECIAL_CORE"],Is.EqualTo(9));
                var root=new GameObject("ActualGrowthUi",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var rect=(RectTransform)safe.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
                try{
                    var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset");var deck=LobbyCommandDeck.Attach(root,font,"LOCAL HTTP",1,new MissionShellMissionRowState[0],null,null);deck.BindAccount(state);deck.OpenPage(LobbyPage.Operators);
                    root.GetComponentsInChildren<Button>().Single(b=>b.name.StartsWith("SelectOwnedOperator")&&b.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="Kyle")).onClick.Invoke();
                    var grow=root.GetComponentsInChildren<Button>().Single(b=>b.name=="GrowOperator0");Assert.That(grow.interactable,Is.True);grow.onClick.Invoke();
                    var deadline=DateTime.UtcNow.AddSeconds(10);while((state.Busy||state.AccountStatus==AccountStateReadStatus.Loading||!state.GrowthReady)&&DateTime.UtcNow<deadline)await Task.Delay(10);
                    Assert.That(state.Account.Currencies["OPERATOR_EXP"],Is.EqualTo(860));Assert.That(root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="OwnedGrowthState").text,Does.Contain("훈련 Lv.5"));
                    typeof(AccountScreenIntegrationTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root,"operator-growth-pinned-local-http.png"});
                }finally{UnityEngine.Object.DestroyImmediate(root);}
            }
        }
    }
}
