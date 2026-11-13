using System;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {public sealed partial class FormationLocalHttpTests {
 [Test,Explicit("Frozen7ad482c actual readonly lifecycle; historical local memory run.")]
 public async Task ActualLifecycleReadPreservesOriginalJournalAndDatabase(){var(wire,session)=await Setup();Func<string,Task<RunApiHttpResponse>> control=path=>wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/__fixture/"+path,null,null),session.AccessToken));using var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);await control("loadout-enable");await control("departure-enable");await state.RefreshSharedAsync();await state.RefreshTrainLoadoutAsync();var ids=new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"};for(int i=0;i<5;i++)state.Formation.Select(i,ids[i]);await state.Formation.SaveAsync();await state.Formation.ActivateAsync();state.SelectTrainCarriage(0,"car_combat");state.SelectTrainCarriage(1,"car_gun");await state.SaveTrainLoadoutAsync();await state.PreviewDepartureAsync(DepartureContract.Operations[0],"I");Assert.That(state.Departure?.RewardReference?.Verification,Is.EqualTo("OPAQUE_SERVER_PIN"));Assert.That((await control("combat-old-run")).StatusCode,Is.EqualTo(200));var hash=(await control("lookup-digest")).BodyJson;await state.RefreshRunLifecycleAsync("run_old");Assert.That(state.Lifecycle,Is.Not.Null,state.LifecycleNotice);Assert.That(state.Lifecycle.NextAction,Is.EqualTo("RECOVER_COMBAT_CONSUMER"));Assert.That((await control("lookup-digest")).BodyJson,Is.EqualTo(hash));Assert.That(state.CanConfirmDeparture,Is.False);await control("combat-completed");await state.RefreshRunLifecycleAsync("run_old");Assert.That(state.Lifecycle.NextAction,Is.EqualTo("REVIEW_REQUIRED"));Assert.That(state.Lifecycle.UiReturnAllowed,Is.True);await state.RefreshRunLifecycleAsync("not_owned");Assert.That(state.Lifecycle,Is.Null);Assert.That(wire.Requests.Where(r=>r.EndpointPath.Contains("/lifecycle")).All(r=>r.Method=="GET"&&r.BodyJson==null),Is.True);}
} }

