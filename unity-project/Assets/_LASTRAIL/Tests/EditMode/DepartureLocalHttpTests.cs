using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;
namespace LastRail.Tests.EditMode
{
    public sealed partial class FormationLocalHttpTests
    {
        [Test,Explicit("Requires pinned4ced349/migration0030 local memory SQLite harness.")]
        public async Task ActualDepartureQuoteRecoveryExpiryChangedSelectionStartReplayAndBattleGate()
        {
            var(wire,session)=await Setup();var journal=new Journal();
            Func<string,Task<RunApiHttpResponse>> control=path=>wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/__fixture/"+path,null,null),session.AccessToken));
            Assert.That((await control("loadout-enable")).StatusCode,Is.EqualTo(200));Assert.That((await control("departure-enable")).StatusCode,Is.EqualTo(200));
            string previewKey;
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                await state.RefreshSharedAsync();await state.RefreshTrainLoadoutAsync();
                var ids=new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"};for(int i=0;i<5;i++)Assert.That(state.Formation.Select(i,ids[i]),Is.True);
                await state.Formation.SaveAsync();await state.Formation.ActivateAsync();
                Assert.That(state.SelectTrainCarriage(0,"car_combat"),Is.True);Assert.That(state.SelectTrainCarriage(1,"car_gun"),Is.True);await state.SaveTrainLoadoutAsync();Assert.That(state.CanPreviewDeparture,Is.True,state.ActionNotice);
                wire.LoseDeparture="/v1/operations/preview";await state.PreviewDepartureAsync(DepartureContract.Operations[0],"I");Assert.That(state.Departure,Is.Null);Assert.That(state.Pending.Kind,Is.EqualTo("departure-preview"));previewKey=state.Pending.Key;
                await state.PreviewDepartureAsync(DepartureContract.Operations[1],"II");Assert.That(state.Pending.Key,Is.EqualTo(previewKey));
            }
            string startKey,previewId;
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                await state.RetryPendingAsync();Assert.That(state.Pending,Is.Null,state.ActionNotice);Assert.That(state.Departure,Is.Not.Null,state.ActionNotice);Assert.That(state.Departure.FuelCost,Is.EqualTo(10));Assert.That(state.Departure.AvailableFuel,Is.EqualTo(30));
                var requests=wire.Requests.Where(r=>r.EndpointPath=="/v1/operations/preview").ToArray();Assert.That(requests.Length,Is.EqualTo(2));Assert.That(Key(requests[1]),Is.EqualTo(previewKey));Assert.That(requests[1].BodyJson,Is.EqualTo(requests[0].BodyJson));
                await state.RefreshSharedAsync();await state.RefreshTrainLoadoutAsync();Assert.That(state.Account.Fuel.Number("current"),Is.EqualTo(30));Assert.That(state.CanConfirmDeparture,Is.False);
                await state.ConfirmDepartureAsync();Assert.That(wire.Requests.Count(r=>r.EndpointPath=="/v1/runs/start"),Is.Zero);
                var root=new GameObject("ActualDepartureUi",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var rect=(RectTransform)safe.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
                try{
                    var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/Fonts/Pretendard/TMP/Pretendard-Regular SDF.asset");
                    var rows=new[]{new MissionShellMissionRowState(DepartureContract.Operations[0],"마지막 출발",RunEncounterType.Ordinary15Wave,true,"start","loading",false)};
                    var deck=LobbyCommandDeck.Attach(root,font,"LOCAL HTTP",1,rows,null,null);deck.BindAccount(state);deck.OpenOperations();root.GetComponentsInChildren<Button>().Single(b=>b.name=="SelectDeparture0").onClick.Invoke();
                    var deadline=DateTime.UtcNow.AddSeconds(10);while((state.AccountStatus==AccountStateReadStatus.Loading||state.TrainLoadoutLoading)&&DateTime.UtcNow<deadline)await Task.Delay(10);
                    Assert.That(root.GetComponentsInChildren<Button>().Single(b=>b.name=="ConfirmDeparture").interactable,Is.False);
                    typeof(AccountScreenIntegrationTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root,"departure-preview-pinned-local-http.png"});
                }finally{UnityEngine.Object.DestroyImmediate(root);}
                // Test-only combat capability: production has no binding, therefore cannot debit fuel.
                state.BattleEntryReady=()=>true;state.ExistingRunRecoveryClear=()=>true;
                await control("departure-expire");await state.ConfirmDepartureAsync();Assert.That(state.Pending,Is.Null);Assert.That(state.Departure,Is.Null);Assert.That(state.Account.Fuel.Number("current"),Is.EqualTo(30));
                await state.PreviewDepartureAsync(DepartureContract.Operations[0],"I");await control("departure-change");await state.ConfirmDepartureAsync();Assert.That(state.ActionErrorCode,Is.EqualTo("departure_quote_changed"));Assert.That(state.Pending,Is.Null);Assert.That(state.Departure,Is.Null);
                await state.PreviewDepartureAsync(DepartureContract.Operations[0],"I");Assert.That(state.Departure,Is.Not.Null,state.ActionNotice);previewId=state.Departure.Id;
                wire.LoseDeparture="/v1/runs/start";await state.ConfirmDepartureAsync();Assert.That(state.Pending.Kind,Is.EqualTo("departure-start"));startKey=state.Pending.Key;Assert.That(state.DepartureStarted,Is.Null);
            }
            using(var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1)){
                var priorPosts=wire.Requests.Count(r=>r.EndpointPath=="/v1/runs/start");await state.RetryPendingAsync();Assert.That(wire.Requests.Count(r=>r.EndpointPath=="/v1/runs/start"),Is.EqualTo(priorPosts),"An unresolved start must not execute when combat is unavailable.");state.BattleEntryReady=()=>true;state.ExistingRunRecoveryClear=()=>true;
                await state.RetryPendingAsync();Assert.That(state.DepartureStarted,Is.Not.Null,state.ActionNotice);var runId=(string)state.DepartureStarted["runId"];
                Assert.That(state.Pending.Key,Is.EqualTo(startKey));Assert.That(state.Account.Fuel.Number("current"),Is.EqualTo(20));Assert.That(state.TrainLoadout.OpenRun,Is.True);
                Assert.That((string)state.DepartureStarted["operationPreviewId"],Is.EqualTo(previewId));Assert.That(state.CanPreviewDeparture,Is.False);
                await state.RetryPendingAsync();Assert.That((string)state.DepartureStarted["runId"],Is.EqualTo(runId));Assert.That(state.Account.Fuel.Number("current"),Is.EqualTo(20));
                var requests=wire.Requests.Where(r=>r.EndpointPath=="/v1/runs/start"&&Key(r)==startKey).ToArray();Assert.That(requests.Length,Is.EqualTo(3));Assert.That(requests.Select(r=>r.BodyJson).Distinct().Count(),Is.EqualTo(1));
                var copy=state.DepartureStarted;copy["runId"]="changed";Assert.That((string)state.DepartureStarted["runId"],Is.EqualTo(runId));
            }
        }
    }
}
