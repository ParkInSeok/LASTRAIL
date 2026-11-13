using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Diagnostics;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class ConfirmedRunReceiptArchiveTests {
  public sealed class Crypto:IAndroidKeystoreNativeStoragePort {
   public bool Fail;public bool IsAndroidRuntime=>true;public bool IsKeystoreAvailable=>!Fail;public bool UsedPlayerPrefsFallback=>false;public string ProviderName=>"fixture_base64_not_encryption";
   public AndroidKeystoreEncryptedTokenPayload Encrypt(string text)=>new AndroidKeystoreEncryptedTokenPayload(Convert.ToBase64String(Encoding.UTF8.GetBytes(text)),"fixture_iv");
   public string Decrypt(AndroidKeystoreEncryptedTokenPayload value)=>Encoding.UTF8.GetString(Convert.FromBase64String(value.CipherTextBase64));
   public bool Save(AndroidKeystoreEncryptedTokenPayload value)=>throw new InvalidOperationException();public AndroidKeystoreEncryptedTokenPayload Load()=>throw new InvalidOperationException();public bool Clear()=>throw new InvalidOperationException();
  }
  static object Archive(Crypto crypto,string path){var type=typeof(DurableRunClient).Assembly.GetType("LastRail.Run.ConfirmedRunReceiptArchive");Assert.That(type,Is.Not.Null,"Missing app durable archive");return Activator.CreateInstance(type,crypto,path);}
  static Task<bool> Save(object archive,PendingRunOperation operation)=>(Task<bool>)archive.GetType().GetMethod("StoreAsync").Invoke(archive,new object[]{operation});
  static Task<PendingRunOperation> Read(object archive,PendingRunOperation operation)=>(Task<PendingRunOperation>)archive.GetType().GetMethod("ReadAsync").Invoke(archive,new object[]{operation.AccountId,operation.TargetId});
  public static PendingRunOperation Claim(bool confirmed=true){var p=(JObject)GameApiJson.ReadObject(File.ReadAllText(Path.GetFullPath("../QA/CommanderS1-20260922/run-complete-claim-example-20261019.json")))["response"];p["commandId"]="archive/original:key/opaque";var op=PendingRunOperation.CompleteClaim((string)p["accountId"],RunRewardClaimRequest.Create((string)p["runId"],(string)p["rewardPreviewId"],(string)p["commandId"]),new AppBuildSnapshot("archive-test",1),DateTimeOffset.Parse("2026-10-21T00:00:00Z"));return confirmed?op.Confirmed(p.ToString()):op;}
  static string DirectoryPath()=>Path.GetFullPath("../QA/OperatorInformation-20261019/RunArchiveFiles/"+Guid.NewGuid().ToString("N"));
  sealed class Wire:IRunApiHttpTransport {public int Calls;public string Response;public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest request){Calls++;return Task.FromResult(RunApiHttpResponse.Success(200,Response));}}
  sealed class DelayedRemove:IRunOperationJournal {
   readonly IRunOperationJournal inner;public bool Delay;public readonly TaskCompletionSource<bool> Entered=new TaskCompletionSource<bool>(),Release=new TaskCompletionSource<bool>();
   public DelayedRemove(IRunOperationJournal inner){this.inner=inner;}
   public Task<System.Collections.Generic.IReadOnlyList<PendingRunOperation>> ReadAsync(string account)=>inner.ReadAsync(account);
   public Task<bool> PutAsync(PendingRunOperation op)=>inner.PutAsync(op);public Task<bool> ConfirmAsync(PendingRunOperation op,string response)=>inner.ConfirmAsync(op,response);
   public async Task<bool> RemoveConfirmedAsync(string account,PendingRunKind kind,string key){if(Delay){Entered.TrySetResult(true);await Release.Task;}return await inner.RemoveConfirmedAsync(account,kind,key);}
  }
  [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)] public async Task ResultOwnerArchivesAndStorageRetryDoesNotReadHttp(int scenario){
   bool fail=scenario==1||scenario==2;
   var op=Claim();var pending=Claim(false);var crypto=new Crypto();var path=DirectoryPath();var journal=new EncryptedRunOperationJournal(new AndroidEncryptedRunFilePort(crypto,Path.Combine(path,"pending.enc")));Assert.That(await journal.PutAsync(pending),Is.True);Assert.That(await journal.ConfirmAsync(pending,op.AcceptedResponseJson),Is.True);
   var session=new GameApiAuthSessionSnapshot(op.AccountId,"ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddMinutes(5),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddMinutes(10));
   var data=(JObject)typeof(RunLifecycleTests).GetMethod("Data",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"CLAIMED"});var response=GameApiJson.ReadObject(op.AcceptedResponseJson);data["runId"]=op.TargetId;data["startResponse"]["runId"]=op.TargetId;data["finish"]["rewardPreviewId"]=response["rewardPreviewId"];data["reward"]["rewardPreviewId"]=response["rewardPreviewId"];data["reward"]["claimId"]=response["rewardClaimId"];data["reward"]["receipt"]=response["transactionReceipt"].DeepClone();data["reward"]["receipt"]["serverClaimedAt"]=DateTimeOffset.Parse((string)response["serverClaimedAt"]).ToUnixTimeMilliseconds();
   var wire=new Wire{Response=data.ToString()};var delayed=new DelayedRemove(journal){Delay=scenario==3};using var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),delayed,()=>session,()=>1);using var state=new AccountScreenRuntime(wire,null,()=>session,()=>1);state.BindRunRecovery(client);
   var archiveCrypto=new Crypto{Fail=fail};var archive=Archive(archiveCrypto,Path.Combine(path,"archive"));var bind=typeof(AccountScreenRuntime).GetMethod("BindRunReceiptArchive");Assert.That(bind,Is.Not.Null,"App result owner must bind durable archive");bind.Invoke(state,new[]{archive});
   var reading=state.ReadRunResultsAsync(op.TargetId);
   var other=(JObject)data.DeepClone();other["runId"]="run_other";other["startResponse"]["runId"]="run_other";other["reward"]["receipt"]["runId"]="run_other";
   if(scenario==3){Assert.That(await Task.WhenAny(delayed.Entered.Task,Task.Delay(5000)),Is.SameAs(delayed.Entered.Task),"Handoff never reached pending removal");wire.Response=other.ToString();var otherReading=state.ReadRunResultsAsync("run_other");delayed.Release.TrySetResult(true);await Task.WhenAll(reading,otherReading);Assert.That(state.RunReceiptHandoffLoading,Is.False,"Old handoff must not leave the new result loading");Assert.That(state.Lifecycle.RunId,Is.EqualTo("run_other"));}
   else {await reading;Assert.That(state.VerifiedRunReceipt,Is.Not.Null,state.RunResultNotice);Assert.That(wire.Calls,Is.EqualTo(3),"Lifecycle plus missing account/progress recovery");Assert.That(state.RunClaimApplicationComplete,Is.False);}
   if(fail){Assert.That((await journal.ReadAsync(op.AccountId)).Count,Is.EqualTo(1));Assert.That(state.RunResultNotice,Does.Contain("저장"));archiveCrypto.Fail=false;if(scenario==2){wire.Response=other.ToString();await state.ReadRunResultsAsync("run_other");wire.Response=data.ToString();await state.EnsureRunResultsAsync(op.TargetId);Assert.That(state.Lifecycle.RunId,Is.EqualTo(op.TargetId));}else{var retry=typeof(AccountScreenRuntime).GetMethod("RetryRunReceiptHandoffAsync");Assert.That(retry,Is.Not.Null);await (Task)retry.Invoke(state,new object[]{op.TargetId});Assert.That(wire.Calls,Is.EqualTo(3),"Storage retry must not perform HTTP");}}
   Assert.That((await journal.ReadAsync(op.AccountId)).Count,Is.Zero);Assert.That((await Read(archive,op)).AcceptedResponseJson,Is.EqualTo(op.AcceptedResponseJson));if(scenario!=3){int beforeRecovery=wire.Calls;await state.EnsureRunResultsAsync(op.TargetId);Assert.That(wire.Calls,Is.EqualTo(beforeRecovery+2),"Only deficient account/progress should retry, not lifecycle");}
  }
  [Test] public async Task UnreadableLocalArchiveDoesNotEraseVerifiedServerReceipt(){
   var op=Claim();var crypto=new Crypto();var archive=new ConfirmedRunReceiptArchive(crypto,DirectoryPath());Assert.That(await archive.StoreAsync(op),Is.True);crypto.Fail=true;
   var session=new GameApiAuthSessionSnapshot(op.AccountId,"ACTIVE",new GameApiAccessToken("lst1.fixture"),DateTime.UtcNow.AddMinutes(5),new GameApiRefreshToken("fixture"),DateTime.UtcNow.AddMinutes(10));
   var data=(JObject)typeof(RunLifecycleTests).GetMethod("Data",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"CLAIMED"});var response=GameApiJson.ReadObject(op.AcceptedResponseJson);data["runId"]=op.TargetId;data["startResponse"]["runId"]=op.TargetId;data["finish"]["rewardPreviewId"]=response["rewardPreviewId"];data["reward"]["rewardPreviewId"]=response["rewardPreviewId"];data["reward"]["claimId"]=response["rewardClaimId"];data["reward"]["receipt"]=response["transactionReceipt"].DeepClone();data["reward"]["receipt"]["serverClaimedAt"]=DateTimeOffset.Parse((string)response["serverClaimedAt"]).ToUnixTimeMilliseconds();
   var wire=new Wire{Response=data.ToString()};using var state=new AccountScreenRuntime(wire,null,()=>session,()=>1);state.BindRunReceiptArchive(archive);await state.ReadRunResultsAsync(op.TargetId);
   Assert.That(state.VerifiedRunReceipt,Is.Not.Null,"Server receipt must survive local storage failure");Assert.That(state.RunResultNotice,Does.Contain("기기"));Assert.That(wire.Calls,Is.EqualTo(1));crypto.Fail=false;Assert.That((await archive.ReadAsync(op.AccountId,op.TargetId)).AcceptedResponseJson,Is.EqualTo(op.AcceptedResponseJson));
  }
  [Test] public async Task ReopenedArchivePreservesExactOriginalAndOpaqueKey(){var path=DirectoryPath();var crypto=new Crypto();var op=Claim();Assert.That(await Save(Archive(crypto,path),op),Is.True);var restored=await Read(Archive(crypto,path),op);Assert.That(restored.AcceptedResponseJson,Is.EqualTo(op.AcceptedResponseJson));Assert.That(restored.BodyJson,Is.EqualTo(op.BodyJson));Assert.That(restored.ComposedAt,Is.EqualTo(op.ComposedAt));Assert.That(restored.ResponseVersion,Is.EqualTo(op.ResponseVersion));Assert.That(restored.IdempotencyKey,Is.EqualTo(op.IdempotencyKey));Assert.That(Directory.GetFiles(path,"*",SearchOption.AllDirectories).Length,Is.EqualTo(1));}
  [Test] public async Task DuplicateIsIdempotentAndConflictingOriginalIsNeverOverwritten(){var path=DirectoryPath();var archive=Archive(new Crypto(),path);var op=Claim();Assert.That(await Save(archive,op),Is.True);Assert.That(await Save(archive,op),Is.True);var altered=GameApiJson.ReadObject(op.AcceptedResponseJson);altered["balanceVersion"]="other-version";var conflict=Claim(false).Confirmed(altered.ToString());Assert.That(await Save(archive,conflict),Is.False);Assert.That((await Read(archive,op)).AcceptedResponseJson,Is.EqualTo(op.AcceptedResponseJson));}
  [Test] public async Task UnconfirmedAndUnavailableStorageNeverCountAsDurable(){var path=DirectoryPath();var crypto=new Crypto();var archive=Archive(crypto,path);Assert.That(await Save(archive,Claim(false)),Is.False);crypto.Fail=true;Assert.That(await Save(archive,Claim()),Is.False);Assert.That(Directory.Exists(path),Is.False);crypto.Fail=false;Assert.That(await Save(archive,Claim()),Is.True);}
  [Test] public async Task DifferentAccountCannotReadOrReplaceOriginal(){var archive=Archive(new Crypto(),DirectoryPath());var op=Claim();Assert.That(await Save(archive,op),Is.True);var other=PendingRunOperation.CompleteClaim("other_account",RunRewardClaimRequest.Create(op.TargetId,(string)GameApiJson.ReadObject(op.BodyJson)["rewardPreviewId"],op.IdempotencyKey),op.Build,op.ComposedAt);Assert.That(await Read(archive,other),Is.Null);Assert.That((await Read(archive,op)).AccountId,Is.EqualTo(op.AccountId));}
 }
}
