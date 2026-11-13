using System;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class RunReceiptVisibilityTests {
  sealed class Wire:IRunApiHttpTransport {public string Json;public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r)=>Task.FromResult(RunApiHttpResponse.Success(200,Json));}
  [TestCase(false)][TestCase(true)] public async Task OnlyValidatedReceiptIsPublished(bool malformed){
   var data=(JObject)typeof(RunLifecycleTests).GetMethod("Data",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"CLAIMED"});
   data["reward"]["receipt"]["rewardPlan"]["rewardLines"]=new JArray(malformed?new JObject():new JObject{["kind"]="CURRENCY",["id"]="credits",["quantity"]=240,["source"]="RUN"});
   var wire=new Wire{Json=data.ToString()};var session=SupportInquiryTests.Session();using var state=new AccountScreenRuntime(wire,null,()=>session,()=>1);
   var property=typeof(AccountScreenRuntime).GetProperty("VerifiedRunReceipt");Assert.That(property,Is.Not.Null,"UI needs a receipt published only after validation");
   state.Changed+=()=>{if(state.RunResultsLoading)Assert.That(property.GetValue(state),Is.Null,"Do not publish while validation is pending");};
   await state.ReadRunResultsAsync("run_test");var receipt=(JObject)property.GetValue(state);
   if(malformed){Assert.That(receipt,Is.Null);Assert.That(state.RunResultNotice,Does.Contain("확인하지 못했습니다"));}
   else {Assert.That(receipt,Is.Not.Null);receipt["runId"]="tampered";Assert.That((string)((JObject)property.GetValue(state))["runId"],Is.EqualTo("run_test"));}
  }
 }
}
