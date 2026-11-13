using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;
namespace LastRail.Tests.EditMode
{
    public sealed partial class FormationLocalHttpTests
    {
        [Test,Explicit("Requires pinnedfd06abc local memory SQLite harness; public starts remain BLOCKED.")]
        public async Task ActualBlockedCombatQuoteNoDebitOriginalReadRecoveryAndFailures()
        {
            var(wire,session)=await Setup();var journal=new Journal();
            Func<string,Task<RunApiHttpResponse>> control=path=>wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/__fixture/"+path,null,null),session.AccessToken));
            Func<string,string,Task<RunApiHttpResponse>> start=(id,key)=>wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/v1/runs/start",new JObject{["operationPreviewId"]=id}.ToString(),new[]{new RunApiHttpHeader("Idempotency-Key",key)}),session.AccessToken));
            Assert.That((await control("loadout-enable")).StatusCode,Is.EqualTo(200));Assert.That((await control("departure-enable")).StatusCode,Is.EqualTo(200));
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                await state.RefreshSharedAsync();await state.RefreshTrainLoadoutAsync();var ids=new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"};for(int i=0;i<5;i++)state.Formation.Select(i,ids[i]);await state.Formation.SaveAsync();await state.Formation.ActivateAsync();
                state.SelectTrainCarriage(0,"car_combat");state.SelectTrainCarriage(1,"car_gun");await state.SaveTrainLoadoutAsync();
                await state.PreviewDepartureAsync(DepartureContract.Operations[0],"I");Assert.That(state.Departure,Is.Not.Null,state.ActionNotice);
                var quote=state.Departure;Assert.That(quote.FirstClearEligible,Is.True);Assert.That(quote.Combat.RiskLevel,Is.EqualTo(1));Assert.That(quote.Combat.OperatorSlotCount,Is.EqualTo(5));Assert.That(quote.Combat.BlockingReasons.Length,Is.EqualTo(7));
                state.BattleEntryReady=()=>true;state.ExistingRunRecoveryClear=()=>true;Assert.That(state.CanConfirmDeparture,Is.False,"A client flag cannot override BLOCKED.");await state.ConfirmDepartureAsync();Assert.That(wire.Requests.Count(r=>r.EndpointPath=="/v1/runs/start"),Is.Zero);
                var denied=await start(quote.Id,"new_paid_start");Assert.That(denied.StatusCode,Is.EqualTo(409));Assert.That((string)JObject.Parse(denied.BodyJson)["errorCode"],Is.EqualTo("combat_not_ready"));
                var metrics=JObject.Parse((await control("combat-metrics")).BodyJson);Assert.That((int)metrics["fuel"],Is.EqualTo(30));Assert.That((int)metrics["runs"],Is.Zero);Assert.That((int)metrics["consumed"],Is.Zero);Assert.That((int)metrics["fuelEntries"],Is.Zero);
                await control("combat-bad-source");var bad=await start(quote.Id,"bad_source_start");Assert.That(bad.StatusCode,Is.EqualTo(503));Assert.That((string)JObject.Parse(bad.BodyJson)["errorCode"],Is.EqualTo("combat_requirement_source_invalid"));
                Assert.That((await control("combat-legacy-preview")).StatusCode,Is.EqualTo(200));
                journal.Save(new PendingAccountAction("account_test","departure-preview",DepartureContract.Operations[0],0,"legacy_original_preview",wire.Requests.First(r=>r.EndpointPath=="/v1/operations/preview").BodyJson));
                using(var upgraded=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                    await upgraded.RetryPendingAsync();Assert.That(upgraded.Pending,Is.Null,"Old accepted preview replay must not permanently lock the journal.");Assert.That(upgraded.Departure,Is.Null);Assert.That(upgraded.CanConfirmDeparture,Is.False);
                }
                var originalReply=await control("combat-old-run");Assert.That(originalReply.StatusCode,Is.EqualTo(200));var original=GameApiJson.ReadObject(originalReply.BodyJson);
                state.OriginalRunId=()=>"run_old";await state.RefreshDepartureRecoveryAsync("run_old");Assert.That(state.DepartureRecovery.Available,Is.True,state.DepartureRecoveryNotice);Assert.That(JToken.DeepEquals(state.DepartureRecovery.StartResponse,original),Is.True);Assert.That(state.CanConfirmDeparture,Is.False);
                Assert.That(state.DepartureRecovery.StartResponse["firstClearEligible"],Is.Null);Assert.That(state.DepartureRecovery.StartResponse["combatContract"],Is.Null);
                var replay=await start(quote.Id,"old_original_key");Assert.That(replay.StatusCode,Is.EqualTo(200));Assert.That(JToken.DeepEquals(GameApiJson.ReadObject(replay.BodyJson),original),Is.True);
                var root=new GameObject("ActualRecoveryUi",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var rect=(RectTransform)safe.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
                try{
                    var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset");var deck=LobbyCommandDeck.Attach(root,font,"LOCAL HTTP",1,new MissionShellMissionRowState[0],null,null);deck.BindAccount(state);deck.OpenOperations();
                    var deadline=DateTime.UtcNow.AddSeconds(10);while(state.DepartureRecoveryLoading&&DateTime.UtcNow<deadline)await Task.Delay(10);
                    Assert.That(root.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("engine_output_profile_unapproved")),Is.False);Assert.That(root.GetComponentsInChildren<Button>().Any(b=>b.name=="RefreshDepartureRecovery"),Is.True);
                    typeof(AccountScreenIntegrationTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root,"combat-recovery-pinned-local-http.png"});
                }finally{UnityEngine.Object.DestroyImmediate(root);}
                await control("combat-completed");await state.RefreshDepartureRecoveryAsync("run_old");Assert.That(state.DepartureRecovery.RunStatus,Is.EqualTo("COMPLETED"));Assert.That(JToken.DeepEquals(state.DepartureRecovery.StartResponse,original),Is.True);
                await control("combat-first-clear");await state.RefreshSharedAsync();await state.RefreshTrainLoadoutAsync();await state.PreviewDepartureAsync(DepartureContract.Operations[0],"I");Assert.That(state.Departure.FirstClearEligible,Is.False);
                var firstRequest=wire.Requests.First(r=>r.EndpointPath=="/v1/operations/preview");var oldQuote=await wire.SendAsync(firstRequest);Assert.That((bool)JObject.Parse(oldQuote.BodyJson)["firstClearEligible"],Is.True);
                // Unknown original request is neither removed nor retransmitted by read recovery.
                var pending=new PendingAccountAction("account_test","departure-start",quote.Id,0,"unknown_original_key",new JObject{["operationPreviewId"]=quote.Id}.ToString());journal.Save(pending);
                await control("combat-no-snapshot");await state.RefreshDepartureRecoveryAsync("run_old");Assert.That(state.DepartureRecovery.Available,Is.False);Assert.That(state.DepartureRecovery.StartResponse,Is.Null);
                await control("combat-foreign");await state.RefreshDepartureRecoveryAsync("run_old");Assert.That(state.DepartureRecoveryError,Is.EqualTo("run_not_found"));
                var query=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("GET","/v1/runs/run_old/start-state?accountId=other",null,null),session.AccessToken));Assert.That(query.StatusCode,Is.EqualTo(400));
                metrics=JObject.Parse((await control("combat-metrics")).BodyJson);Assert.That((int)metrics["fuel"],Is.EqualTo(30));Assert.That((int)metrics["runs"],Is.EqualTo(1));Assert.That((int)metrics["fuelEntries"],Is.Zero);Assert.That((int)metrics["rewards"],Is.Zero);
                await control("revoke");await state.RefreshDepartureRecoveryAsync("run_old");Assert.That(state.DepartureRecoveryError,Is.EqualTo("auth_session_invalid"));
                Assert.That(journal.Read("account_test").Key,Is.EqualTo("unknown_original_key"));
                Assert.That(wire.Requests.Where(r=>r.EndpointPath.EndsWith("/start-state")).All(r=>r.Method=="GET"&&r.BodyJson==null),Is.True);
            }
        }
    }
}
