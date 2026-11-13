using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
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
        [Test,Explicit("Requires frozen b63d5f4 API with migration0031; local memory fixture only.")]
        public async Task ActualUltimateSelectionUiRecoveryConflictFormationAndPreviewPins()
        {
            var(wire,session)=await Setup();var journal=new Journal();
            Func<string,Task<RunApiHttpResponse>> control=path=>wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/__fixture/"+path,null,null),session.AccessToken));
            await control("loadout-enable");await control("departure-enable");
            using var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1);
            await state.RefreshSharedAsync();await state.RefreshTrainLoadoutAsync();await state.RefreshUltimateAsync();Assert.That(state.Ultimate.Status,Is.EqualTo("FORMATION_REQUIRED"));Assert.That(state.Ultimate.OperatorId,Is.Null);
            var ids=new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"};for(int i=0;i<5;i++)state.Formation.Select(i,ids[i]);await state.Formation.SaveAsync();await state.Formation.ActivateAsync();
            Assert.That(state.Ultimate.Status,Is.EqualTo("UNSELECTED"));Assert.That(state.Ultimate.Revision,Is.Zero);Assert.That(state.UltimateDraft,Is.Null);
            state.SelectTrainCarriage(0,"car_combat");state.SelectTrainCarriage(1,"car_gun");await state.SaveTrainLoadoutAsync();await state.PreviewDepartureAsync(DepartureContract.Operations[0],"I");
            Assert.That(state.Departure.Combat.BlockingReasons,Does.Contain("ultimate_selection_required"));Assert.That(state.Departure.Combat.CanStart,Is.False);
            var root=new GameObject("UltimateUi",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);
            try {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var pages=LobbyDestinationPages.Create(root.transform,font,()=>{});pages.BindAccount(state);
                Action rebuild=()=>{if(pages!=null)pages.RefreshAccountView(state);};state.Changed+=rebuild;
                try {
                    pages.Open(LobbyPage.Formation);while(state.UltimateLoading||state.AccountStatus==AccountStateReadStatus.Loading)await Task.Yield();
                    pages.GetComponentsInChildren<Button>().Single(b=>b.name=="OpenUltimateSelection").onClick.Invoke();while(state.UltimateLoading||state.AccountStatus==AccountStateReadStatus.Loading)await Task.Yield();
                    pages.GetComponentsInChildren<Button>().Single(b=>b.name=="UltimateMember2").onClick.Invoke();Assert.That(state.UltimateDraft,Is.EqualTo("op_eve"));
                    wire.LoseDeparture="/v1/formation/ultimate";pages.GetComponentsInChildren<Button>().Single(b=>b.name=="SaveUltimate").onClick.Invoke();while(state.Busy)await Task.Yield();
                    var original=journal.Read("account_test");Assert.That(original,Is.Not.Null);Assert.That(state.UltimateReady,Is.False);
                    await state.RetryPendingAsync();Assert.That(state.Pending,Is.Null,state.ActionNotice);Assert.That(state.Ultimate.OperatorId,Is.EqualTo("op_eve"));Assert.That(state.Ultimate.Revision,Is.EqualTo(1));
                    var writes=wire.Requests.Where(r=>r.Method=="POST"&&r.EndpointPath=="/v1/formation/ultimate").ToArray();Assert.That(writes.Length,Is.EqualTo(2));Assert.That(Key(writes[0]),Is.EqualTo(Key(writes[1])));Assert.That(writes[0].BodyJson,Is.EqualTo(writes[1].BodyJson));
                    Assert.That(state.Departure,Is.Null);await state.PreviewDepartureAsync(DepartureContract.Operations[0],"I");
                    var quote=state.Departure;Assert.That(quote.CurrentVersion,Is.True);Assert.That(quote.Combat.Version,Is.EqualTo("run-combat-requirements-v2"));Assert.That(quote.Combat.UltimateOperatorId,Is.EqualTo("op_eve"));Assert.That(quote.Combat.BlockingReasons.Length,Is.EqualTo(6));Assert.That(state.CanConfirmDeparture,Is.False);
                    var previewBody=JObject.Parse(wire.Requests.Last(r=>r.EndpointPath=="/v1/operations/preview").BodyJson);Assert.That((long)previewBody["expectedUltimateRevision"],Is.EqualTo(1));
                    state.SelectUltimate("op_reina");
                    var external=new JObject{["rulesVersion"]="ultimate-selection-v1",["operatorId"]="op_mira",["expectedRevision"]=1,["expectedFormationRevision"]=state.Account.FormationRevision};
                    var response=await wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/v1/formation/ultimate",external.ToString(),new[]{new RunApiHttpHeader("Idempotency-Key","external_choice")}),session.AccessToken));Assert.That(response.StatusCode,Is.EqualTo(200));
                    await state.SaveUltimateAsync();Assert.That(state.ActionErrorCode,Is.EqualTo("ultimate_revision_conflict"));Assert.That(state.UltimateDraft,Is.EqualTo("op_reina"));Assert.That(state.Ultimate.OperatorId,Is.EqualTo("op_mira"));Assert.That(state.Pending,Is.Null);
                    await state.SaveUltimateAsync();Assert.That(state.Ultimate.OperatorId,Is.EqualTo("op_reina"));Assert.That(state.Ultimate.Revision,Is.EqualTo(3));
                    Assert.That((string)quote.Admission["ultimateSelection"]["operatorId"],Is.EqualTo("op_eve"),"Frozen quote is not rewritten by current choice.");
                    typeof(AccountScreenIntegrationTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root,"ultimate-selection-pinned-local-http.png"});
                    state.SelectUltimate(null);await state.SaveUltimateAsync();Assert.That(state.Ultimate.Status,Is.EqualTo("UNSELECTED"));Assert.That(state.Ultimate.Revision,Is.EqualTo(4));
                    state.SelectUltimate("op_reina");await state.SaveUltimateAsync();var revision=state.Ultimate.Revision;
                    state.Formation.Select(0,"op_reina");await state.Formation.SaveAsync();Assert.That(state.Ultimate.OperatorId,Is.EqualTo("op_reina"));await state.Formation.ActivateAsync();Assert.That(state.Ultimate.OperatorId,Is.EqualTo("op_reina"));Assert.That(state.Ultimate.Revision,Is.EqualTo(revision));
                    await control("ultimate-extra-owner");await state.RefreshSharedAsync();state.Formation.Select(0,"op_extra_fixture");await state.Formation.SaveAsync();Assert.That(state.Ultimate.OperatorId,Is.EqualTo("op_reina"));await state.Formation.ActivateAsync();Assert.That(state.Ultimate.OperatorId,Is.Null);Assert.That(state.Ultimate.Revision,Is.EqualTo(revision+1));
                    await control("loadout-open");await state.RefreshUltimateAsync();Assert.That(state.Ultimate.ChangeLocked,Is.True);Assert.That(state.SelectUltimate("op_kyle"),Is.False);
                    var metrics=JObject.Parse((await control("combat-metrics")).BodyJson);Assert.That((int)metrics["fuel"],Is.EqualTo(30));Assert.That((int)metrics["fuelEntries"],Is.Zero);Assert.That(wire.Requests.Any(r=>r.EndpointPath=="/v1/runs/start"),Is.False);
                }finally{state.Changed-=rebuild;}
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
