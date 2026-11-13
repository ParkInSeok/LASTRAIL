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
namespace LastRail.Tests.EditMode {public sealed partial class FormationLocalHttpTests {
 sealed class MonthlyFixtureRegistry:IMonthlyAttendanceRegistry {public string CurrentActivityId {get;set;}="fixture-monthly";}
 [Test,Explicit("Frozen 59e9ad6 API with memory-only activity/elite binding; not release policy.")]
 public async Task ActualMonthlyAttendanceReadOnlyMissingBindingLostAutoAndPaidRecovery(){
  var(wire,session)=await Setup();var journal=new Journal();var registry=new MonthlyFixtureRegistry();
  Func<string,Task<RunApiHttpResponse>> control=path=>wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/__fixture/"+path,null,null),session.AccessToken));
  await control("shop-enable");await control("monthly-missing");registry.CurrentActivityId="fixture-monthly-missing";
  using var state=new AccountScreenRuntime(wire,journal,()=>session,()=>1,registry);
  await state.EnterMonthlyAttendanceAsync();Assert.That(state.MonthlyReady,Is.True,state.MonthlyError);Assert.That(state.MonthlyAttendance.Cells.Count,Is.EqualTo(28));Assert.That(state.MonthlyAttendance.UnavailableReason,Is.EqualTo("elite_ticket_binding_missing"));Assert.That(wire.Requests.Any(r=>r.EndpointPath.EndsWith("/auto-attend")),Is.False);
  await control("reset");await control("shop-enable");await control("monthly-enable");registry.CurrentActivityId="fixture-monthly";await state.RefreshMonthlyAttendanceAsync();Assert.That(state.MonthlyReady,Is.True,state.MonthlyError);Assert.That(state.MonthlyAttendance.Completed,Is.Zero);Assert.That(state.PrepareMonthlyRecovery(),Is.Null);
  wire.LoseDeparture="/v1/attendance/fixture-monthly/auto-attend";await state.EnterMonthlyAttendanceAsync();Assert.That(state.Pending?.Kind,Is.EqualTo("monthly-auto"));Assert.That(state.LastMonthlyReceipt,Is.Null);var original=state.Pending;
  await state.RetryPendingAsync();Assert.That(state.Pending,Is.Null,state.ActionNotice);Assert.That(state.LastMonthlyReceipt.Cell,Is.EqualTo(1));Assert.That(state.MonthlyReady,Is.True,state.MonthlyError);Assert.That(state.MonthlyAttendance.TodayAttended,Is.True);Assert.That(state.Wallet.FreeBalance,Is.EqualTo(65));
  var writes=wire.Requests.Where(r=>r.EndpointPath.EndsWith("/auto-attend")).ToArray();Assert.That(writes.Length,Is.EqualTo(2));Assert.That(Key(writes[0]),Is.EqualTo(original.Key));Assert.That(Key(writes[1]),Is.EqualTo(original.Key));Assert.That(writes[1].BodyJson,Is.EqualTo(original.Payload));
  await state.EnterMonthlyAttendanceAsync();Assert.That(wire.Requests.Count(r=>r.EndpointPath.EndsWith("/auto-attend")),Is.EqualTo(2));
  var quote=state.PrepareMonthlyRecovery();Assert.That(quote,Is.Not.Null);Assert.That(quote.Paid,Is.EqualTo(35));await state.RecoverMonthlyAttendanceAsync(quote,false);Assert.That(wire.Requests.Any(r=>r.EndpointPath.EndsWith("/recover-next")),Is.False);
  // A competing wallet spend changes the split after the user saw the quote.
  await state.ExchangeTicketsAsync(1);await state.RecoverMonthlyAttendanceAsync(quote,true);Assert.That(state.Pending,Is.Null);Assert.That(state.ActionErrorCode,Is.EqualTo("attendance_paid_confirmation_changed"));Assert.That(state.MonthlyAttendance.Completed,Is.EqualTo(1));
  quote=state.PrepareMonthlyRecovery();Assert.That(quote.Paid,Is.EqualTo(46));
  var root=new GameObject("MonthlyHttpUi",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var rect=(RectTransform)safe.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
  try{
   var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var deck=LobbyCommandDeck.Attach(root,font,"LOCAL HTTP",1,new MissionShellMissionRowState[0],null,null);deck.BindAccount(state);deck.OpenMonthlyAttendance();while(state.MonthlyLoading)await Task.Yield();
   root.GetComponentsInChildren<Button>().Single(b=>b.name=="RecoverMonthly").onClick.Invoke();Assert.That(root.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("유료 크리스탈 46")),Is.True);
   wire.LoseDeparture="/v1/attendance/fixture-monthly/recover-next";root.GetComponentsInChildren<Button>().Single(b=>b.name=="ConfirmPaidAttendance").onClick.Invoke();while(state.Busy)await Task.Yield();Assert.That(state.Pending?.Kind,Is.EqualTo("monthly-recovery"));var recovery=state.Pending;
   await state.RetryPendingAsync();Assert.That(state.Pending,Is.Null,state.ActionNotice);Assert.That(state.LastMonthlyReceipt.Kind,Is.EqualTo("RECOVERY"));Assert.That(state.MonthlyAttendance.Completed,Is.EqualTo(2));Assert.That(state.Wallet.PaidBalance,Is.EqualTo(54));Assert.That(state.Wallet.FreeBalance,Is.Zero);
   writes=wire.Requests.Where(r=>r.EndpointPath.EndsWith("/recover-next")).ToArray();Assert.That(writes.Length,Is.EqualTo(3));Assert.That(Key(writes[1]),Is.EqualTo(recovery.Key));Assert.That(Key(writes[2]),Is.EqualTo(recovery.Key));Assert.That(writes[2].BodyJson,Is.EqualTo(recovery.Payload));
   var metrics=JObject.Parse((await control("monthly-metrics")).BodyJson);Assert.That((int)metrics["visits"],Is.EqualTo(1));Assert.That((int)metrics["recoveries"],Is.EqualTo(1));Assert.That((int)metrics["cells"],Is.EqualTo(2));Assert.That((int)metrics["credits"],Is.EqualTo(6000));
   typeof(AccountScreenIntegrationTests).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{root,"monthly-attendance-pinned-local-http.png"});
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 [Test,Explicit("Frozen e330309 discovery with memory-only policies. No release binding.")]
 public async Task ActualMonthlyDiscoveryEmptyBlockedAvailableAndAmbiguous(){
  var(wire,session)=await Setup();using var state=new AccountScreenRuntime(wire,new Journal(),()=>session,()=>1);
  Func<string,Task<RunApiHttpResponse>> control=path=>wire.SendAsync(GameApiBearerAuthRequestFactory.WithBearerToken(new RunApiHttpRequest("POST","/__fixture/"+path,null,null),session.AccessToken));
  await state.EnterMonthlyAttendanceAsync();Assert.That(state.MonthlyAttendance,Is.Null);Assert.That(state.MonthlyError,Is.EqualTo("NOT_CONFIGURED"));
  await control("monthly-missing");await state.EnterMonthlyAttendanceAsync();Assert.That(state.MonthlyReady,Is.True,state.MonthlyError);Assert.That(state.MonthlyAttendance.UnavailableReason,Is.EqualTo("elite_ticket_binding_missing"));Assert.That(wire.Requests.Any(r=>r.EndpointPath.EndsWith("/auto-attend")),Is.False);
  await control("reset");await control("shop-enable");await control("monthly-enable");await state.EnterMonthlyAttendanceAsync();Assert.That(state.MonthlyReady,Is.True,state.MonthlyError);Assert.That(state.MonthlyAttendance.Completed,Is.EqualTo(1));Assert.That(state.LastMonthlyReceipt.Cell,Is.EqualTo(1));
  await control("monthly-missing");var writes=wire.Requests.Count(r=>r.EndpointPath.EndsWith("/auto-attend"));await state.EnterMonthlyAttendanceAsync();Assert.That(state.MonthlyAttendance,Is.Null);Assert.That(state.MonthlyReady,Is.False);Assert.That(state.MonthlyError,Is.EqualTo("attendance_definition_ambiguous"));Assert.That(wire.Requests.Count(r=>r.EndpointPath.EndsWith("/auto-attend")),Is.EqualTo(writes));
 }
} }
