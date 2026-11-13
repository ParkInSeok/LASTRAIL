using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class AdSkipUseTests {
  internal static string Sample(string name)=>File.ReadAllText("D:/Project/LASTRAIL/QA/AdSkipUnityPhase1-20260917/ServerFixtures/"+name+".json");
  internal sealed class Storage:IAndroidKeystoreNativeStoragePort {
   AndroidKeystoreEncryptedTokenPayload saved;public bool IsAndroidRuntime=>true;public bool IsKeystoreAvailable=>true;public string ProviderName=>"TEST_ONLY";public bool UsedPlayerPrefsFallback=>false;
   public AndroidKeystoreEncryptedTokenPayload Encrypt(string s)=>new(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(s)),"TEST_IV");public string Decrypt(AndroidKeystoreEncryptedTokenPayload p)=>System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(p.CipherTextBase64));public bool Save(AndroidKeystoreEncryptedTokenPayload p){saved=p;return true;}public AndroidKeystoreEncryptedTokenPayload Load()=>saved;public bool Clear(){saved=null;return true;}
  }
  internal sealed class Wire:IRunApiHttpTransport {
   public readonly List<RunApiHttpRequest> Calls=new();public Func<RunApiHttpRequest,Task<RunApiHttpResponse>> Override;
   public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest q){Calls.Add(q);return Override?.Invoke(q)??Reply(q);}
   public static Task<RunApiHttpResponse> Reply(RunApiHttpRequest q){
    bool combat=q.EndpointPath.Contains("/runs/")||q.BodyJson?.Contains("COMBAT")==true;
    string name=q.EndpointPath=="/v1/ad-skip"?"07-owned-available":q.EndpointPath.EndsWith("/uses/authorize")?combat?"12-combat-approved":"08-offline-approved":q.EndpointPath.EndsWith("/spin")?combat?"14-combat-spin":"10-offline-spin":q.EndpointPath.Contains("ad-intents/")?combat?"13-combat-read":"09-offline-read":null;
    if(q.Method!="GET"&&!q.EndpointPath.EndsWith("/uses/authorize")&&!q.EndpointPath.EndsWith("/spin"))Assert.Fail("Unexpected mutation: "+q.EndpointPath);
    return Task.FromResult(name==null?RunApiHttpResponse.NetworkError("TEST_ONLY absent fixture"):RunApiHttpResponse.Success(200,Sample(name)));
   }
  }
  sealed class Ad:IRewardAdPresentation {public int Calls;public bool Available=>true;public Task ShowAsync(string a,string b,string c){Calls++;return Task.CompletedTask;}}
  internal static GameApiAuthSessionSnapshot Session(string id="a")=>new(id,"ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddMinutes(20),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddHours(1));
  internal static Task Use(AccountScreenRuntime owner,bool combat=false){var method=typeof(AccountScreenRuntime).GetMethod(combat?"UseAdSkipCombatAsync":"UseAdSkipOfflineAsync");Assert.That(method,Is.Not.Null,"Single-input purchase approval composition required");return (Task)method.Invoke(owner,combat?new object[]{"test-run","test-preview"}:Array.Empty<object>());}
  static UnityEngine.GameObject Host(AccountScreenRuntime owner,out LobbyCommandDeck deck){
   var root=new UnityEngine.GameObject("AdSkipLifecycleHost",typeof(UnityEngine.RectTransform),typeof(UnityEngine.Canvas));((UnityEngine.RectTransform)root.transform).sizeDelta=new UnityEngine.Vector2(720,1280);var safe=new UnityEngine.GameObject("SafeArea",typeof(UnityEngine.RectTransform));safe.transform.SetParent(root.transform,false);var rect=(UnityEngine.RectTransform)safe.transform;rect.anchorMin=UnityEngine.Vector2.zero;rect.anchorMax=UnityEngine.Vector2.one;rect.sizeDelta=UnityEngine.Vector2.zero;var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");deck=LobbyCommandDeck.Attach(root,font,"TEST_ONLY",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(owner);return root;
  }
  [Test] public void ExistingPendingReadSurvivesUnavailableEntitlement(){
   string response=OfflineAdContractTests.Sample("pending");var s=Session();var j=new AccountActionJournal(new Storage());j.Save(new PendingAccountAction("a","ad-presented",(string)JObject.Parse(response)["intentId"],0,"old_ad",response));var w=new Wire{Override=q=>q.EndpointPath.Contains("ad-intents/")?Task.FromResult(RunApiHttpResponse.Success(200,response)):Task.FromResult(RunApiHttpResponse.NetworkError("unavailable"))};using var a=new AccountScreenRuntime(w,j,()=>s,()=>1);var root=Host(a,out var deck);
   try{root.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="Shortcut3").onClick.Invoke();root.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="OpenOfflineRoulette").onClick.Invoke();var main=root.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="AdMainAction");Assert.That(main.GetComponentInChildren<TMPro.TMP_Text>().text,Is.EqualTo("다시 확인"));response=OfflineAdContractTests.Sample("verified");main.onClick.Invoke();Assert.That(a.CanSpinAd,Is.True);Assert.That(w.Calls.All(q=>q.Method=="GET"),Is.True);}
   finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  [Test] public async Task ClosingHostDuringApprovalDoesNotRecreateViewOrLoseReceipt(){
   var s=Session();var gate=new TaskCompletionSource<RunApiHttpResponse>();var w=new Wire{Override=q=>q.EndpointPath.EndsWith("/uses/authorize")?gate.Task:Wire.Reply(q)};using var a=new AccountScreenRuntime(w,new AccountActionJournal(new Storage()),()=>s,()=>1);var root=Host(a,out var deck);
   try{root.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="Shortcut3").onClick.Invoke();root.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="OpenOfflineRoulette").onClick.Invoke();Assert.That(a.Busy,Is.True);deck.CloseDialog();gate.SetResult(RunApiHttpResponse.Success(200,Sample("08-offline-approved")));await Task.Yield();Assert.That(a.AdIntent?.Status,Is.EqualTo("CLAIMED"));Assert.That(root.GetComponentInChildren<SharedRoulettePopup>(),Is.Null);Assert.That(a.Pending.Kind,Is.EqualTo("ad-spin"));}
   finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  [TestCase(false)][TestCase(true)] public async Task ProviderlessOneInputAuthorizesThenUsesExistingSpin(bool combat){
   var w=new Wire();var s=Session();var j=new AccountActionJournal(new Storage());using var a=new AccountScreenRuntime(w,j,()=>s,()=>1);bool sawBase=false;
   a.Changed+=()=>{if(combat?a.CombatAds.Intent?.CanSpin==true:a.AdIntent?.CanSpin==true)sawBase=true;};await Use(a,combat);
   Assert.That(combat?a.CombatAds.Intent?.Status:a.AdIntent?.Status,Is.EqualTo("CLAIMED"));Assert.That(sawBase,Is.True,"Publish pinned waiting preview before receipt");
   Assert.That(w.Calls.Count(q=>q.EndpointPath.EndsWith("/uses/authorize")),Is.EqualTo(1));Assert.That(w.Calls.Count(q=>q.EndpointPath.EndsWith("/spin")),Is.EqualTo(1));
   Assert.That(w.Calls.Where(q=>q.EndpointPath.Contains("ad-intents")||q.EndpointPath.EndsWith("/uses/authorize")).All(q=>q.Headers.Any(h=>h.Name=="X-Reward-Contract"&&h.Value=="reward-opportunity-v2")),Is.True);
  }
  [Test] public async Task UnknownEntitlementCannotFallBackToAdvertising(){var w=new Wire{Override=q=>Task.FromResult(RunApiHttpResponse.NetworkError("offline"))};var s=Session();var ad=new Ad();using var a=new AccountScreenRuntime(w,new AccountActionJournal(new Storage()),()=>s,()=>1);a.ConfigureRewardAds(ad);await Use(a);Assert.That(w.Calls.All(q=>q.Method=="GET"),Is.True);Assert.That(ad.Calls,Is.Zero);Assert.That(a.Pending,Is.Null);}
  [Test] public async Task LostAuthorizationRetriesSameKeyAndBodyAfterOwnerRestart(){var port=new Storage();var s=Session();var w=new Wire{Override=q=>q.EndpointPath.EndsWith("/uses/authorize")?Task.FromResult(RunApiHttpResponse.NetworkError("lost")):Wire.Reply(q)};using(var a=new AccountScreenRuntime(w,new AccountActionJournal(port),()=>s,()=>1)){await Use(a);Assert.That(a.Pending.Kind,Is.EqualTo("reward-approval"));}w.Override=null;using(var a=new AccountScreenRuntime(w,new AccountActionJournal(port),()=>s,()=>1)){await a.RetryPendingAsync();Assert.That(a.AdIntent?.Status,Is.EqualTo("CLAIMED"));}var calls=w.Calls.Where(q=>q.EndpointPath.EndsWith("/uses/authorize")).ToArray();Assert.That(calls.Length,Is.EqualTo(2));Assert.That(calls.Select(q=>q.BodyJson).Distinct().Count(),Is.EqualTo(1));Assert.That(calls.Select(q=>q.Headers.Single(h=>h.Name=="Idempotency-Key").Value).Distinct().Count(),Is.EqualTo(1));}
  [Test] public async Task AccountSwitchDropsLateApprovalAndDoesNotSpin(){var s=Session();long g=1;var gate=new TaskCompletionSource<RunApiHttpResponse>();var w=new Wire{Override=q=>q.EndpointPath.EndsWith("/uses/authorize")?gate.Task:Wire.Reply(q)};var j=new AccountActionJournal(new Storage());using var a=new AccountScreenRuntime(w,j,()=>s,()=>g);var task=Use(a);s=Session("b");g++;a.SynchronizeSession();gate.SetResult(RunApiHttpResponse.Success(200,Sample("08-offline-approved")));await task;Assert.That(a.AdIntent,Is.Null);Assert.That(a.Pending,Is.Null);Assert.That(j.Read("a").Kind,Is.EqualTo("reward-approval"));Assert.That(w.Calls.Any(q=>q.EndpointPath.EndsWith("/spin")),Is.False);}
  [Test] public void ActualServerSnapshotsParseAndPreserveBinding(){var a=OfflineAdIntent.Parse(Sample("08-offline-approved"));var b=OfflineAdIntent.Parse(Sample("11-offline-claimed"));b.RequireSame(a);Assert.That(b.Result,Is.Not.Null);var c=CombatAdIntent.Parse(Sample("12-combat-approved"),"test-run","test-preview");CombatAdIntent.Parse(Sample("15-combat-claimed"),"test-run","test-preview").RequireSame(c);Assert.That(AdSkipSnapshot.Parse(Sample("17-revoked")).Owned,Is.False);}
  [Test] public async Task LostSpinRestoresClaimedWithoutReauthorizationOrSecondSpin(){
   var port=new Storage();var s=Session();var w=new Wire{Override=q=>q.EndpointPath.EndsWith("/spin")?Task.FromResult(RunApiHttpResponse.NetworkError("lost spin")):Wire.Reply(q)};
   using(var a=new AccountScreenRuntime(w,new AccountActionJournal(port),()=>s,()=>1)){await Use(a);Assert.That(a.Pending.Kind,Is.EqualTo("ad-spin"));}
   w.Override=q=>q.EndpointPath.Contains("ad-intents/")?Task.FromResult(RunApiHttpResponse.Success(200,Sample("11-offline-claimed"))):Wire.Reply(q);
   using(var a=new AccountScreenRuntime(w,new AccountActionJournal(port),()=>s,()=>1)){await a.RetryPendingAsync();Assert.That(a.AdIntent.Status,Is.EqualTo("CLAIMED"));}
   Assert.That(w.Calls.Count(q=>q.EndpointPath.EndsWith("/spin")),Is.EqualTo(1));Assert.That(w.Calls.Count(q=>q.EndpointPath.EndsWith("/uses/authorize")),Is.EqualTo(1));
  }
  [Test] public async Task DefinitiveRevocationReleasesNewRequestWithoutAdvertising(){var s=Session();var w=new Wire{Override=q=>q.EndpointPath.EndsWith("/uses/authorize")?Task.FromResult(RunApiHttpResponse.Success(403,"{\"accepted\":false,\"errorCode\":\"ad_skip_entitlement_revoked\"}")):Wire.Reply(q)};var ad=new Ad();using var a=new AccountScreenRuntime(w,new AccountActionJournal(new Storage()),()=>s,()=>1);a.ConfigureRewardAds(ad);await Use(a);Assert.That(a.Pending,Is.Null);Assert.That(a.AdSkip.Snapshot,Is.Null,"Authoritative revocation must invalidate the earlier owned observation");Assert.That(ad.Calls,Is.Zero);Assert.That(w.Calls.Any(q=>q.EndpointPath.EndsWith("/spin")),Is.False);}
  [Test] public async Task RepeatedInputAndDisposeCannotDuplicateOrApplyLateApproval(){var s=Session();var gate=new TaskCompletionSource<RunApiHttpResponse>();var w=new Wire{Override=q=>q.EndpointPath.EndsWith("/uses/authorize")?gate.Task:Wire.Reply(q)};var a=new AccountScreenRuntime(w,new AccountActionJournal(new Storage()),()=>s,()=>1);var pending=Use(a);await Use(a);a.Dispose();gate.SetResult(RunApiHttpResponse.Success(200,Sample("08-offline-approved")));await pending;Assert.That(w.Calls.Count(q=>q.EndpointPath.EndsWith("/uses/authorize")),Is.EqualTo(1));Assert.That(w.Calls.Any(q=>q.EndpointPath.EndsWith("/spin")),Is.False);}
  [Test] public async Task PendingAdvertisingConvertsWithOriginalChallengeAndPolicy(){
   var original=JObject.Parse(OfflineAdContractTests.Sample("pending"));var approved=RewardOpportunityContractTests.V2(original,false,false);approved["status"]="AD_VERIFIED";approved["canSpin"]=true;approved["approval"]["cause"]="PURCHASE_ENTITLEMENT";
   var result=(JObject)JObject.Parse(OfflineAdContractTests.Sample("claimed-restored"))["result"];var s=Session();var j=new AccountActionJournal(new Storage());j.Save(new PendingAccountAction("a","ad-presented",(string)original["intentId"],0,"prior-ad-key",original.ToString()));
   var w=new Wire{Override=q=>q.EndpointPath.EndsWith("/spin")?Task.FromResult(RunApiHttpResponse.Success(200,result.ToString())):q.EndpointPath.EndsWith("/uses/authorize")||q.EndpointPath.Contains("ad-intents/")?Task.FromResult(RunApiHttpResponse.Success(200,approved.ToString())):Wire.Reply(q)};var ad=new Ad();using var a=new AccountScreenRuntime(w,j,()=>s,()=>1);a.ConfigureRewardAds(ad);await Use(a);
   Assert.That(a.AdIntent?.Status,Is.EqualTo("CLAIMED"));Assert.That(a.AdIntent.CustomData,Is.EqualTo((string)original["customData"]));Assert.That(ad.Calls,Is.Zero);Assert.That((string)JObject.Parse(w.Calls.Single(q=>q.EndpointPath.EndsWith("/uses/authorize")).BodyJson)["opportunity"]["intentId"],Is.EqualTo((string)original["intentId"]));
  }
  [Test] public async Task ActualSupplyEntryStartsPaidSpinWithoutSecondButton(){
   var w=new Wire();var s=Session();using var a=new AccountScreenRuntime(w,new AccountActionJournal(new Storage()),()=>s,()=>1);await a.RefreshAdSkipAsync();
   var root=new UnityEngine.GameObject("PaidSupplyHost",typeof(UnityEngine.RectTransform),typeof(UnityEngine.Canvas));((UnityEngine.RectTransform)root.transform).sizeDelta=new UnityEngine.Vector2(720,1280);var safe=new UnityEngine.GameObject("SafeArea",typeof(UnityEngine.RectTransform));safe.transform.SetParent(root.transform,false);var rect=(UnityEngine.RectTransform)safe.transform;rect.anchorMin=UnityEngine.Vector2.zero;rect.anchorMax=UnityEngine.Vector2.one;rect.sizeDelta=UnityEngine.Vector2.zero;
   try{var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var deck=LobbyCommandDeck.Attach(root,font,"TEST_ONLY",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(a);root.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="Shortcut3").onClick.Invoke();root.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="OpenOfflineRoulette").onClick.Invoke();Assert.That(w.Calls.Count(q=>q.EndpointPath.EndsWith("/spin")),Is.EqualTo(1));Assert.That(a.AdIntent?.Status,Is.EqualTo("CLAIMED"));}
   finally{UnityEngine.Object.DestroyImmediate(root);}
  }
 }
}
