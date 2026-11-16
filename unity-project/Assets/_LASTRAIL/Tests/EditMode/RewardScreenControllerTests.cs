using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode
{
 public sealed class RewardScreenControllerTests
 {
  [Test] public void CombatDisplayProjectionKeepsRecoveryAndProviderAvailabilitySeparate()
  {
   var unavailable=new CombatRouletteDisplayState(null,"",false,false,false,true);
   Assert.IsFalse(unavailable.Enabled);Assert.IsTrue(unavailable.CanBasic);Assert.That(unavailable.Notice,Does.Contain("현재 광고를 이용할 수 없습니다"));
   var recovery=new CombatRouletteDisplayState(null,"pending",false,false,true,false,false,"사용 결과 재확인",true);
   Assert.IsTrue(recovery.Enabled);Assert.IsTrue(recovery.SeparateRefresh);Assert.AreEqual("사용 결과 재확인",recovery.Label);Assert.AreEqual("pending",recovery.Notice);Assert.IsFalse(recovery.Paid);
  }
  sealed class Wire:IRunApiHttpTransport
  {
   public readonly List<RunApiHttpRequest> Requests=new List<RunApiHttpRequest>();
   public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){Requests.Add(request);return Task.FromResult(RunApiHttpResponse.NetworkError("offline fixture"));}
  }
  [Test] public async Task BulkSelectionOwnsPagingAndCapsSelectionAndRejectsUnlistedIds()
  {
   var wire=new Wire();var session=SupportInquiryTests.Session();using var account=new AccountScreenRuntime(wire,null,()=>session,()=>1);account.SynchronizeSession();
   var rows=new JArray();for(int i=0;i<55;i++)rows.Add(new JObject{["id"]="item_"+i,["label"]="보상 "+i});
   var selection=(BulkClaimSelection)Activator.CreateInstance(typeof(BulkClaimSelection),BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{session,1L,"MAIL",null,null,rows},null);
   BulkClaimScreenState display=null;using var controller=new BulkClaimScreenController(account,selection,false,false,s=>display=s,()=>{},()=>{},()=>true);
   controller.Render();Assert.AreEqual(11,display.Pages);Assert.AreEqual(5,display.Rows.Count);
   controller.MovePage(-1);Assert.AreEqual(10,display.Page);Assert.AreEqual("item_50",display.Rows[0].Id);
   controller.ToggleAll();Assert.AreEqual("50개 수령",display.SubmitText);
   controller.Toggle("item_54");Assert.AreEqual("50개 수령",display.SubmitText);
   controller.Toggle("item_0");controller.Toggle("not_in_list");Assert.AreEqual("49개 수령",display.SubmitText);
   controller.Dispose();var prior=display;controller.Toggle("item_54");controller.MovePage(1);controller.Render();await controller.SubmitAsync();
   Assert.AreSame(prior,display);Assert.IsEmpty(wire.Requests);
  }
  [TestCase(false)][TestCase(true)] public async Task RouletteCallbacksCannotRunAfterDisposalOrAccountSwitch(bool changeAccount)
  {
   var wire=new Wire();var session=SupportInquiryTests.Session();long generation=1;using var account=new AccountScreenRuntime(wire,null,()=>session,()=>generation);
   int renders=0,closed=0;using var offline=new OfflineRouletteScreenController(account,s=>renders++,()=>closed++,()=>true);
   using var combat=new CombatRouletteScreenController(account,"run_test",s=>renders++,()=>closed++,()=>true);
   if(changeAccount){session=SupportInquiryTests.Session("other");generation++;}else{offline.Dispose();combat.Dispose();}
   offline.Render();combat.Render();offline.Back();combat.Close();await offline.ActAsync();await offline.RecheckAsync();await offline.EnterAsync(true);await combat.ActAsync();await combat.RefreshAsync();await combat.ClaimBasicAsync();await combat.EnterAsync(true,"preview_test");
   Assert.AreEqual(0,renders);Assert.AreEqual(0,closed);Assert.IsEmpty(wire.Requests);
  }
  [Test] public async Task OfflineUnknownEntitlementRoutesOneIntentToReadAndDisplayDoesNotQuery()
  {
   var wire=new Wire();var session=SupportInquiryTests.Session();using var account=new AccountScreenRuntime(wire,null,()=>session,()=>1);
   OfflineRouletteScreenState display=null;using var controller=new OfflineRouletteScreenController(account,s=>display=s,()=>{},()=>true);
   controller.Render();Assert.AreEqual("이용권 다시 확인",display.Label);Assert.IsEmpty(wire.Requests);
   await controller.ActAsync();
   Assert.That(wire.Requests.Select(r=>r.EndpointPath),Is.EquivalentTo(new[]{"/v1/ad-skip","/v1/runtime-policy"}),"The existing account refresh reads entitlement and advertising policy once each.");
   Assert.That(wire.Requests.All(r=>r.Method=="GET"),Is.True);
   int reads=wire.Requests.Count;controller.Render();controller.Render();Assert.AreEqual(reads,wire.Requests.Count);
  }
 }
}
