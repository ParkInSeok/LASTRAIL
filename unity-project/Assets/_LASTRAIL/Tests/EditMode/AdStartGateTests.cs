using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
 public sealed class AdStartGateTests
 {
  sealed class Ad:IRewardAdPresentation {public bool Available=>true;public int Shows;public Task ShowAsync(string a,string b,string c){Shows++;return Task.CompletedTask;}}
  sealed class Wire:IRunApiHttpTransport
  {
   public List<RunApiHttpRequest> Calls=new();public string Mode="approved";public TaskCompletionSource<RunApiHttpResponse> Deferred;public JObject Intent,Start;
   public Wire(bool combat=false)
   {
    var fixtures=JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/AdsOffUnity-20260921/ServerFixtures/http-fixtures.json"));
    Intent=combat?CombatAdControllerTests.Sample():(JObject)fixtures["samples"]["created"]["body"].DeepClone();
    Start=(JObject)fixtures["samples"]["started"]["body"].DeepClone();
    // Time-shifted TEST_ONLY execution fixture; immutable HTTP source remains unchanged.
    long now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();Intent["createdAt"]=now-1000;Intent["expiresAt"]=now+60000;if(combat)Intent["callbackDeadline"]=now+90000;
    Start["consumer"]=combat?"COMBAT":"OFFLINE";Start["intentId"]=Intent["intentId"];Start["approvedAt"]=now;Start["expiresAt"]=Intent["expiresAt"];
   }
   public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r)
   {
    Calls.Add(r);string json;
    if(r.EndpointPath=="/v1/runtime-policy")json=AdRuntimePolicyTests.Policy(true);
    else if(r.EndpointPath=="/v1/ad-skip")json=AdSkipUseTests.Sample("03-configured-unowned");
    else if(r.EndpointPath.EndsWith("/start"))
    {
     if(Deferred!=null)return Deferred.Task;
     if(Mode=="lost")return Task.FromResult(RunApiHttpResponse.NetworkError("TEST_ONLY lost"));
     if(Mode=="replay"){Start["startStatus"]="ALREADY_APPROVED";Start["mayShow"]=false;}
     if(Mode=="wrong")Start["intentId"]="other_intent";
     if(Mode=="expired")Start["expiresAt"]=1;
     json=Start.ToString();
    }
    else if(r.Method=="POST"&&r.EndpointPath.EndsWith("ad-intents")&&Mode.StartsWith("ad"))return Task.FromResult(RunApiHttpResponse.Success(409,new JObject{["accepted"]=false,["errorCode"]=Mode}.ToString()));
    else if(r.EndpointPath.Contains("ad-intents"))json=Intent.ToString();
    else return Task.FromResult(RunApiHttpResponse.NetworkError("TEST_ONLY outside fixture"));
    return Task.FromResult(RunApiHttpResponse.Success(200,json));
   }
  }
  [TestCase(false,"approved",1)][TestCase(true,"approved",1)]
  [TestCase(false,"replay",0)][TestCase(true,"replay",0)]
  [TestCase(false,"lost",0)][TestCase(true,"lost",0)]
  [TestCase(false,"wrong",0)][TestCase(true,"wrong",0)]
  [TestCase(false,"expired",0)][TestCase(true,"expired",0)]
  public async Task OnlyMatchingFreshFirstApprovalCanPresent(bool combat,string mode,int shows)
  {
   var wire=new Wire(combat){Mode=mode};var ad=new Ad();var session=SupportInquiryTests.Session();var journal=new AccountActionJournal(new AdSkipUseTests.Storage());
   using var owner=new AccountScreenRuntime(wire,journal,()=>session,()=>1);owner.ConfigureRewardAds(ad);
   using var controller=new CombatAdController(wire,journal,()=>session,()=>1,()=>ad,()=>Task.FromResult(true));
   if(combat)await controller.BeginAsync("run_test","preview_test");else await owner.BeginAdAsync();
   Assert.That(wire.Calls.Count(r=>r.EndpointPath.EndsWith("/start")),Is.EqualTo(1));Assert.That(ad.Shows,Is.EqualTo(shows));
   var create=wire.Calls.Single(r=>r.Method=="POST"&&r.EndpointPath.EndsWith("ad-intents"));
   Assert.That(create.Headers.Single(h=>h.Name=="X-Ad-Contract").Value,Is.EqualTo("ad-start-v1"));
   string original=journal.Read(session.AccountId).Key;
   Assert.That(wire.Calls.Single(r=>r.EndpointPath.EndsWith("/start")).Headers.Single(h=>h.Name=="Idempotency-Key").Value,Is.EqualTo(original));
   if(combat){await controller.RefreshAsync("run_test","preview_test");await controller.BeginAsync("run_test","preview_test");}
   else{await owner.RecheckAdAsync();await owner.PresentAdAsync();}
   Assert.That(ad.Shows,Is.EqualTo(shows));Assert.That(journal.Read(session.AccountId).Key,Is.EqualTo(original));
   Assert.That(wire.Calls.Count(r=>r.EndpointPath.EndsWith("/start")),Is.EqualTo(1),"Uncertain/replayed start must not create another presentation attempt");
  }
  [TestCase(false)][TestCase(true)] public async Task OwnerChangeWhileStartPendingCannotPresent(bool combat)
  {
   var wire=new Wire(combat){Deferred=new TaskCompletionSource<RunApiHttpResponse>()};var ad=new Ad();var session=SupportInquiryTests.Session();var journal=new AccountActionJournal(new AdSkipUseTests.Storage());
   using var owner=new AccountScreenRuntime(wire,journal,()=>session,()=>1);owner.ConfigureRewardAds(ad);
   using var controller=new CombatAdController(wire,journal,()=>session,()=>1,()=>ad,()=>Task.FromResult(true));
   var operation=combat?controller.BeginAsync("run_test","preview_test"):owner.BeginAdAsync();
   Assert.That(wire.Calls.Any(r=>r.EndpointPath.EndsWith("/start")),Is.True);session=SupportInquiryTests.Session();wire.Deferred.SetResult(RunApiHttpResponse.Success(200,wire.Start.ToString()));await operation;Assert.That(ad.Shows,Is.Zero);
  }
  [TestCase(false,"ads_disabled_by_policy")][TestCase(true,"ads_disabled_by_policy")]
  [TestCase(false,"ad_runtime_policy_changed")][TestCase(true,"ad_runtime_policy_changed")]
  public async Task DefinitiveCreateRejectionDoesNotLeaveBasicBlocked(bool combat,string code)
  {
   var wire=new Wire(combat){Mode=code};var ad=new Ad();var session=SupportInquiryTests.Session();var journal=new AccountActionJournal(new AdSkipUseTests.Storage());using var owner=new AccountScreenRuntime(wire,journal,()=>session,()=>1);owner.ConfigureRewardAds(ad);
   using var controller=new CombatAdController(wire,journal,()=>session,()=>1,()=>ad,()=>Task.FromResult(true));if(combat)await controller.BeginAsync("run_test","preview_test");else await owner.BeginAdAsync();
   Assert.That(journal.Read(session.AccountId),Is.Null);Assert.That(ad.Shows,Is.Zero);Assert.That(wire.Calls.Any(r=>r.EndpointPath.EndsWith("/start")),Is.False);
  }
  [TestCase(false)][TestCase(true)] public async Task LocalPolicyInvalidationDuringStartCannotPresent(bool combat)
  {
   var wire=new Wire(combat){Deferred=new TaskCompletionSource<RunApiHttpResponse>()};var ad=new Ad();var session=SupportInquiryTests.Session();var journal=new AccountActionJournal(new AdSkipUseTests.Storage());using var owner=new AccountScreenRuntime(wire,journal,()=>session,()=>1);owner.ConfigureRewardAds(ad);bool enabled=true;
   using var controller=new CombatAdController(wire,journal,()=>session,()=>1,()=>ad,()=>Task.FromResult(true),()=>enabled);
   var task=combat?controller.BeginAsync("run_test","preview_test"):owner.BeginAdAsync();Assert.That(wire.Calls.Any(r=>r.EndpointPath.EndsWith("/start")),Is.True);enabled=false;owner.RuntimeAds.Invalidate();wire.Deferred.SetResult(RunApiHttpResponse.Success(200,wire.Start.ToString()));await task;Assert.That(ad.Shows,Is.Zero);
  }
 }
}
