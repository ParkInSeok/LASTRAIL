#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Diagnostics {
 public sealed partial class DataLifecycleLocalPlayerProbe {
  bool SingleArchiveObservation=>Environment.GetEnvironmentVariable("LASTRAIL_ARCHIVE_WEAK_SINGLE_OBSERVATION")=="1";
  readonly List<KeyValuePair<string,WeakReference>> archiveWeak=new List<KeyValuePair<string,WeakReference>>();
  async Task RunConfirmTrial(string directory,JObject bootstrap){
   Require((string)bootstrap["version"]=="qa.run-confirm.v1","Run trial contract mismatch");
   string restartSource=Environment.GetEnvironmentVariable("LASTRAIL_RUN_ARCHIVE_RESTART_SOURCE");
   if(!string.IsNullOrEmpty(restartSource)){await RunArchiveRestartTrial(directory,bootstrap,restartSource);return;}
   if(Environment.GetEnvironmentVariable("LASTRAIL_RUN_NORMAL_CLAIM_TRIAL")=="1"){await RunNormalClaimTrial(directory,bootstrap);return;}
   bool baseline=variant=="RUN_CONFIRM_BASELINE",diagnostic=Environment.GetEnvironmentVariable("LASTRAIL_MEMORY_COLLECT_DIAGNOSTIC")=="1";
   memory=SceneMemoryRecorder.CreateExternal(directory);memory.BeginWindow();await Task.Delay(1000);memory.Checkpoint("00-resident");
   adapter=new UnityWebRequestRunApiHttpAdapter((string)bootstrap["baseUrl"],10);
   var wire=new Wire(new UnityWebRequestRunApiHttpTransport(adapter));
   var session=new GameApiAuthSessionSnapshot((string)bootstrap["accountId"],"ACTIVE",new GameApiAccessToken((string)bootstrap["accessToken"]),DateTime.UtcNow.AddMinutes(10),new GameApiRefreshToken("qa-not-login"),DateTime.UtcNow.AddMinutes(15));
   long generation=1;
   var journal=new RunConfirmJournal();
   var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>session,()=>generation,()=>new AppBuildSnapshot("run-confirm-qa",1));
   PendingRunOperation confirmed=null;
   try{
    var request=RunRewardClaimRequest.Create((string)bootstrap["runId"],(string)bootstrap["previewId"],"qa-run-confirm-original-key");
    memory.BeginWindow();Require(await client.ClaimCompleteAsync(request)==null,"Injected local failure did not remain pending");
    Require(journal.Row!=null&&journal.Row.AcceptedResponseJson==null,"Premature durable completion");
    await Task.Delay(500);memory.Checkpoint("01-validated-response-save-failed");
    int retryStart=wire.Rows.Count;
    memory.BeginWindow();await client.RestoreAsync();Require(await client.RetryAsync(journal.Row)==null,"Second local failure was not retained");
    journal.FailConfirm=false;confirmed=await client.RetryAsync(journal.Row);
    Require(confirmed!=null,"Original response was not saved: "+client.Notice);
    int retryHttp=wire.Rows.Count-retryStart;
    Require(retryHttp==(baseline?2:0),"Unexpected retry HTTP count");
    var receipt=RunCompleteClaimReceipt.Read(confirmed.AcceptedResponseJson,session.AccountId,request.RunId,request.RewardPreviewId,request.IdempotencyKey).Capture();
    Require((long)receipt["owners"]["account"]["currencies"][0]["balance"]==110,"Unexpected server balance");
    for(int i=0;i<5;i++)Require(await client.RetryAsync(confirmed)!=null,"Confirmed display retry failed");
    Require(wire.Rows.Count==retryStart+retryHttp,"Confirmed display retry transmitted");
    receipt=null;await Task.Delay(500);memory.Checkpoint("02-confirmed-local-retries");
    if(Environment.GetEnvironmentVariable("LASTRAIL_RUN_TERMINAL_TRIAL")=="1")await RunTerminalEntryTrial(wire,session,client,request.RunId,directory);
    bool archiveTrial=Environment.GetEnvironmentVariable("LASTRAIL_RUN_ARCHIVE_TRIAL")=="1";
    memory.BeginWindow();
    if(archiveTrial)await RunArchiveEntryTrial(wire,session,client,journal,confirmed,directory);
    else {
    Require(!await client.HandoffAsync(confirmed,_=>Task.FromResult(false)),"Failed sink removed receipt");
    Require(journal.Row!=null,"Original record missing before durable handoff");
    Require(await client.HandoffAsync(confirmed,record=>{
     var packet=new JObject{["kind"]=record.Kind.ToString(),["accountId"]=record.AccountId,["targetId"]=record.TargetId,["idempotencyKey"]=record.IdempotencyKey,["bodyJson"]=record.BodyJson,["responseVersion"]=record.ResponseVersion,["appVersion"]=record.Build.AppVersion,["appVersionCode"]=record.Build.AppVersionCode,["composedAt"]=record.ComposedAt.ToString("O"),["acceptedResponseJson"]=record.AcceptedResponseJson};
     string path=Path.Combine(directory,"durable-receipt.json"),json=packet.ToString();var bytes=Encoding.UTF8.GetBytes(json);
     using(var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None)){output.Write(bytes,0,bytes.Length);output.Flush(true);}
     return Task.FromResult(File.ReadAllText(path)==json);
    }),"Durable sink failed");
    Require(journal.Row==null&&journal.Removes==1,"Handoff did not remove exactly once");
    Require(!await client.HandoffAsync(confirmed,_=>throw new InvalidOperationException("Duplicate handoff sink")),"Removed operation handed off twice");
    }
    await Task.Delay(500);memory.Checkpoint("03-durable-handoff");
    memory.BeginWindow();client.Dispose();client=null;journal=null;confirmed=null;request=null;session=null;generation++;bootstrap=null;
    await Task.Delay(1500);memory.Checkpoint("04-references-released-natural");
    if(diagnostic){GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();memory.Checkpoint("05-diagnostic-gc",false);}
    if(archiveWeak.Count>0&&!SingleArchiveObservation)WriteArchiveWeak(directory,"archive-weak-references.json",diagnostic);
    File.WriteAllText(Path.Combine(directory,"result.json"),new JObject{["status"]="Passed",["variant"]=variant,["classification"]="ACTUAL_PLAYER_AUTHENTICATED_CLAIM_HTTP_REAL_APP_SYNTHETIC_TERMINAL_SQL_MEMORY_ONLY",["calls"]=new JArray(wire.Rows),["localRetryHttpCalls"]=retryHttp,["normalFollowupGets"]=wire.Rows.Count(r=>(string)r["method"]=="GET"),["durableSink"]=archiveTrial?"APP_ARCHIVE_ADAPTER_WITH_QA_CRYPTO_SEE_ARCHIVE_ENTRY":"QA_FILE_FLUSH_AND_READBACK_BEFORE_SINGLE_JOURNAL_REMOVE",["journalPersistence"]="MEMORY_ONLY_FAILURE_INJECTION",["forcedGC"]=diagnostic,["naturalMeasurementsBeforeDiagnosticGc"]=true,["notExecuted"]=new JArray("public START","actual finish","combat scene return","account owner v2 application","process restart recovery","device encrypted storage","mobile","texture CPU/GPU split"),["unity"]=Application.unityVersion}.ToString());
   }finally{client?.Dispose();}
  }
  async Task RunArchiveRestartTrial(string directory,JObject bootstrap,string source){
   source=Path.GetFullPath(source);
   Require(source.Replace('\\','/').StartsWith("D:/Project/LASTRAIL/QA/OperatorInformation-20261019/",StringComparison.OrdinalIgnoreCase),"Explicit QA restart source required");
   memory=SceneMemoryRecorder.CreateExternal(directory);memory.BeginWindow();await Task.Delay(1000);memory.Checkpoint("00-resident");
   adapter=new UnityWebRequestRunApiHttpAdapter((string)bootstrap["baseUrl"],10);var wire=new Wire(new UnityWebRequestRunApiHttpTransport(adapter));
   var session=new GameApiAuthSessionSnapshot((string)bootstrap["accountId"],"ACTIVE",new GameApiAccessToken((string)bootstrap["accessToken"]),DateTime.UtcNow.AddMinutes(10),new GameApiRefreshToken("qa-not-login"),DateTime.UtcNow.AddMinutes(15));
   var archive=new ConfirmedRunReceiptArchive(new ArchiveQaCrypto(),Path.Combine(source,"app-receipts"));string run=(string)bootstrap["runId"];
   state=new AccountScreenRuntime(wire,null,()=>session,()=>1);state.BindRunReceiptArchive(archive);
   try{
    Require(state.Account==null&&state.Wallet==null&&state.StoryArchive.State==null,"Restart must begin with empty owners and no definition");
    var original=await archive.ReadAsync(session.AccountId,run);Require(original!=null,"Original archive absent");
    memory.Checkpoint("01-empty-owners");memory.BeginWindow();
    bool applied=await state.ConsumeArchivedRunClaimAsync(run);
    File.WriteAllText(Path.Combine(directory,"owner-recovery.json"),new JObject{["applied"]=applied,["completion"]=state.RunClaimApplicationComplete,["accountStatus"]=state.AccountStatus.ToString(),["accountError"]=state.AccountErrorCode,["financialSequence"]=state.Account?.FinancialMutationSequence,["walletStatus"]=state.Wallet?.Status.ToString(),["walletSequence"]=state.Wallet?.MutationSequence,["progressStatus"]=state.StoryArchive.Status.ToString(),["progressError"]=state.StoryArchive.ErrorCode,["progressSequence"]=state.StoryArchive.ConfirmedProgress?.MutationSequence,["calls"]=new JArray(wire.Rows)}.ToString());
    Require(applied&&state.RunClaimApplicationComplete,"Restart owner recovery incomplete");
    Require(state.Account.Currencies["CREDITS"]==110&&state.Account.Items.Any(r=>r.Text("itemId")=="item-fixture"&&r.Number("quantity")==5),"Restart financial mismatch");
    Require(state.Wallet.TotalBalance==7&&state.StoryArchive.State.Order==2&&state.StoryArchive.State.UnlockedOperations.Contains("opn_03_water_stop"),"Restart wallet/progression mismatch");
    Require(wire.Rows.Count==3&&wire.Rows.All(r=>(string)r["method"]=="GET")&&wire.Rows.Select(r=>(string)r["path"]).OrderBy(x=>x).SequenceEqual(new[]{"/v1/account/state","/v1/story-progress","/v1/wallet/crystals"}),"Restart must recover only three missing owners");
    int before=wire.Rows.Count;for(int i=0;i<4;i++)Require(await state.ConsumeArchivedRunClaimAsync(run),"Restart repeat incomplete");Require(wire.Rows.Count==before,"Restart repeat issued HTTP");
    var retained=await archive.ReadAsync(session.AccountId,run);Require(retained.SameRequest(original)&&retained.AcceptedResponseJson==original.AcceptedResponseJson,"Restart changed original receipt");
    memory.Checkpoint("02-restart-applied");
    File.WriteAllText(Path.Combine(directory,"result.json"),new JObject{["status"]="Passed",["classification"]="NEW_WINDOWS_PLAYER_PROCESS_SAME_LIVE_QA_SERVER_QA_BASE64_CRYPTO",["processRestart"]=true,["calls"]=new JArray(wire.Rows),["credits"]=110,["itemQuantity"]=5,["walletBalance"]=7,["progressOrder"]=2,["originalReceiptUnchanged"]=true,["completion"]=state.RunClaimApplicationComplete,["repeatHttp"]=wire.Rows.Count-before,["notExecuted"]=new JArray("server restart","Android Keystore","mobile","native UI","combat START")}.ToString());
    original=null;retained=null;state.Dispose();state=null;archive=null;session=null;
    memory.BeginWindow();await Task.Delay(1500);memory.Checkpoint("03-released-natural");
    if(Environment.GetEnvironmentVariable("LASTRAIL_MEMORY_COLLECT_DIAGNOSTIC")=="1"){GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();memory.Checkpoint("04-diagnostic-gc",false);}
   }finally{state?.Dispose();state=null;}
  }
  async Task RunNormalClaimTrial(string directory,JObject bootstrap){
   bool progressTrial=Environment.GetEnvironmentVariable("LASTRAIL_RUN_PROGRESS_CLAIM_TRIAL")=="1";
   bool baseline=variant=="NORMAL_CLAIM_BASELINE",walletTrial=Environment.GetEnvironmentVariable("LASTRAIL_RUN_WALLET_CLAIM_TRIAL")=="1";
   memory=SceneMemoryRecorder.CreateExternal(directory);memory.BeginWindow();await Task.Delay(1000);memory.Checkpoint("00-resident");
   adapter=new UnityWebRequestRunApiHttpAdapter((string)bootstrap["baseUrl"],10);var wire=new Wire(new UnityWebRequestRunApiHttpTransport(adapter));
   var session=new GameApiAuthSessionSnapshot((string)bootstrap["accountId"],"ACTIVE",new GameApiAccessToken((string)bootstrap["accessToken"]),DateTime.UtcNow.AddMinutes(10),new GameApiRefreshToken("qa-not-login"),DateTime.UtcNow.AddMinutes(15));
   var crypto=new ArchiveQaCrypto();var journal=new EncryptedRunOperationJournal(new AndroidEncryptedRunFilePort(crypto,Path.Combine(directory,"pending.enc")));var archive=new ConfirmedRunReceiptArchive(crypto,Path.Combine(directory,"app-receipts"));
   var client=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>session,()=>1,()=>new AppBuildSnapshot("normal-claim-qa",1));
   state=new AccountScreenRuntime(wire,null,()=>session,()=>1);state.BindRunRecovery(client);state.BindRunReceiptArchive(archive);
   try{
    await state.RefreshAccountAsync();await state.StoryArchive.RefreshAsync();if(walletTrial){await state.RefreshWalletAsync();Require(state.Wallet?.Status==CrystalWalletReadStatus.Ready&&state.Wallet.TotalBalance==0,"Initial wallet observation missing");}
    Require(state.AccountStatus==AccountStateReadStatus.Ready,"Actual account read unavailable: "+state.AccountErrorCode);Require(state.StoryArchive.Status==LastRail.Narrative.StoryArchiveReadStatus.Ready,"Actual story read unavailable");
    string run=(string)bootstrap["runId"];await state.ReadRunResultsAsync(run);Require(state.CanClaimRunReward(run),"Normal claim unavailable: "+state.RunResultNotice);
    memory.Checkpoint("01-ready");int before=wire.Rows.Count;bool financeAppliedBeforeWholeRead=false,walletAppliedBeforeWholeRead=false,progressAppliedBeforeWholeRead=false;
    state.Changed+=Observe;
    void Observe(){if(state.Account!=null&&state.Account.FinancialMutationSequence>state.Account.MutationSequence&&state.Account.Currencies.TryGetValue("CREDITS",out var amount)&&amount==110){financeAppliedBeforeWholeRead=true;if(walletTrial&&state.Wallet?.TotalBalance==7)walletAppliedBeforeWholeRead=true;if(progressTrial&&state.StoryArchive.State?.Order==2&&state.StoryArchive.State.UnlockedOperations.Contains("opn_03_water_stop"))progressAppliedBeforeWholeRead=true;}}
    memory.BeginWindow();await state.ClaimRunRewardAsync(run);state.Changed-=Observe;
    Require(state.VerifiedRunReceipt!=null,"Normal result receipt missing: "+state.RunResultNotice);
    if(!baseline)Require(state.RunClaimApplicationComplete,"Normal owner/durable completion missing: "+state.RunResultNotice);
    Require(state.Account!=null&&state.Account.Currencies["CREDITS"]==110&&state.Account.Items.Any(r=>r.Text("itemId")=="item-fixture"&&r.Number("quantity")==5),"Actual owner values missing");
    Require(financeAppliedBeforeWholeRead==!baseline,"Financial receipt owner consumption mismatch");if(walletTrial)Require(walletAppliedBeforeWholeRead&&!baseline&&state.Wallet?.TotalBalance==7,"Actual wallet receipt application missing");
    if(progressTrial)Require(progressAppliedBeforeWholeRead&&state.StoryArchive.ConfirmedProgress!=null,"Nonempty progression receipt application missing");
    var saved=await archive.ReadAsync(session.AccountId,run);Require(saved!=null&&saved.ResponseVersion==(baseline?null:RunCompleteClaimReceipt.Version),"Original response/version not archived");
    Require((await journal.ReadAsync(session.AccountId)).Count==0,"Archived pending source remains");
    int reentryBefore=wire.Rows.Count;for(int i=0;i<4;i++)await state.EnsureRunResultsAsync(run);Require(wire.Rows.Count==reentryBefore,"Terminal entry repeats HTTP");
    memory.Checkpoint("02-normal-claim-and-archive");
    File.WriteAllText(Path.Combine(directory,"result.json"),new JObject{["status"]="Passed",["variant"]=variant,["baseline"]=baseline,["classification"]="ACTUAL_PLAYER_CONTROLLER_AUTHENTICATED_HTTP_REAL_APP_SYNTHETIC_TERMINAL_SQL",["calls"]=new JArray(wire.Rows),["claimPhaseHttp"]=wire.Rows.Count-before,["followupGets"]=wire.Rows.Skip(before).Count(r=>(string)r["method"]=="GET"),["financeAppliedBeforeWholeRead"]=financeAppliedBeforeWholeRead,["credits"]=110,["itemQuantity"]=5,["archiveReopened"]=true,["responseVersion"]=saved.ResponseVersion,["legacyFallbackRetained"]=true,["walletSnapshotChanged"]=walletTrial,["walletAppliedBeforeWholeRead"]=walletAppliedBeforeWholeRead,["progressChanged"]=progressTrial,["progressAppliedBeforeWholeRead"]=progressAppliedBeforeWholeRead,["progressOrder"]=state.StoryArchive.State.Order,["progressSequence"]=state.StoryArchive.ConfirmedProgress?.MutationSequence,["unlockedOperations"]=new JArray(state.StoryArchive.State.UnlockedOperations),["crypto"]="QA_BASE64_NOT_ANDROID_KEYSTORE",["processRestart"]=false,["nativeUiTouch"]=false}.ToString());
    saved=null;state.Dispose();state=null;client.Dispose();client=null;session=null;archive=null;journal=null;crypto=null;
    memory.BeginWindow();await Task.Delay(1500);memory.Checkpoint("03-released-natural");
    if(Environment.GetEnvironmentVariable("LASTRAIL_MEMORY_COLLECT_DIAGNOSTIC")=="1"){GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();memory.Checkpoint("04-diagnostic-gc",false);}
   }finally{state?.Dispose();state=null;client?.Dispose();}
  }
  void WriteArchiveWeak(string directory,string filename,bool collected){var references=new JObject();foreach(var pair in archiveWeak)references[pair.Key]=pair.Value.IsAlive;File.WriteAllText(Path.Combine(directory,filename),new JObject{["afterDiagnosticGc"]=collected,["onlyFinalWeakObservation"]=SingleArchiveObservation,["alive"]=references,["scope"]="Selected owner/archive/crypto/original objects only; not a complete heap-root analysis"}.ToString());}
  async Task CompleteArchiveLifetimeDiagnostic(string directory){
   if(archiveWeak.Count==0)return;
   if(Environment.GetEnvironmentVariable("LASTRAIL_MEMORY_COLLECT_DIAGNOSTIC")=="1"){
    await Task.Delay(500);GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
    memory.Checkpoint("06-after-trial-return-diagnostic-gc",false);if(!SingleArchiveObservation)WriteArchiveWeak(directory,"archive-weak-after-trial-return.json",true);
    adapter?.Dispose();adapter=null;await Task.Yield();await Task.Delay(500);GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();memory.Checkpoint("07-after-adapter-release-diagnostic-gc",false);WriteArchiveWeak(directory,"archive-weak-after-adapter-release.json",true);
   }
   if(Environment.GetEnvironmentVariable("LASTRAIL_ARCHIVE_HEAP_SNAPSHOT")=="1"){
    var completed=new TaskCompletionSource<bool>();
    Unity.Profiling.Memory.MemoryProfiler.TakeSnapshot(Path.Combine(directory,"archive-managed.snap"),(path,success)=>completed.TrySetResult(success),Unity.Profiling.Memory.CaptureFlags.ManagedObjects|Unity.Profiling.Memory.CaptureFlags.NativeObjects);
    Require(await completed.Task,"Managed snapshot capture failed");WriteArchiveWeak(directory,"archive-weak-after-snapshot.json",false);
   }
   archiveWeak.Clear();
  }
  sealed class ArchiveQaCrypto:IAndroidKeystoreNativeStoragePort {
   public bool Fail;public bool IsAndroidRuntime=>true;public bool IsKeystoreAvailable=>!Fail;public bool UsedPlayerPrefsFallback=>false;public string ProviderName=>"QA_BASE64_NOT_DEVICE_ENCRYPTION";
   public AndroidKeystoreEncryptedTokenPayload Encrypt(string text)=>new AndroidKeystoreEncryptedTokenPayload(Convert.ToBase64String(Encoding.UTF8.GetBytes(text)),"qa_iv");
   public string Decrypt(AndroidKeystoreEncryptedTokenPayload value)=>Encoding.UTF8.GetString(Convert.FromBase64String(value.CipherTextBase64));
   public bool Save(AndroidKeystoreEncryptedTokenPayload value)=>throw new InvalidOperationException();public AndroidKeystoreEncryptedTokenPayload Load()=>throw new InvalidOperationException();public bool Clear()=>throw new InvalidOperationException();
  }
  async Task RunArchiveEntryTrial(Wire wire,GameApiAuthSessionSnapshot session,DurableRunClient client,RunConfirmJournal journal,PendingRunOperation original,string directory){
   bool baseline=variant=="RUN_ARCHIVE_BASELINE";var crypto=new ArchiveQaCrypto{Fail=true};string path=Path.Combine(directory,"app-receipts");var archive=new ConfirmedRunReceiptArchive(crypto,path);
   state=new AccountScreenRuntime(wire,null,()=>session,()=>1);state.BindRunRecovery(client);state.BindRunReceiptArchive(archive);
   int before=wire.Rows.Count;await state.ReadRunResultsAsync(original.TargetId);Require(state.VerifiedRunReceipt!=null,"Actual lifecycle receipt absent");Require(journal.Row!=null,"Failed archive removed pending source");
   crypto.Fail=false;int retryBefore=wire.Rows.Count;
   if(!baseline)await state.RetryRunReceiptHandoffAsync(original.TargetId);
   for(int i=0;i<4;i++)await state.EnsureRunResultsAsync(original.TargetId);
   Require(wire.Rows.Count==retryBefore,"Archive retry/re-entry performed HTTP");
   var restored=await new ConfirmedRunReceiptArchive(crypto,path).ReadAsync(session.AccountId,original.TargetId);
   if(baseline)Require(restored==null&&journal.Row!=null&&journal.Removes==0,"Baseline unexpectedly archived");
   else Require(restored?.AcceptedResponseJson==original.AcceptedResponseJson&&restored.BodyJson==original.BodyJson&&restored.IdempotencyKey==original.IdempotencyKey&&journal.Row==null&&journal.Removes==1,"App durable archive/cleanup mismatch");
   File.WriteAllText(Path.Combine(directory,"archive-entry.json"),new JObject{["status"]="Passed",["baseline"]=baseline,["lifecycleGets"]=retryBefore-before,["storageRetryHttp"]=wire.Rows.Count-retryBefore,["pendingRows"]=journal.Row==null?0:1,["pendingRemoves"]=journal.Removes,["archiveReopenedAndExact"]=restored!=null,["nativeUiTouch"]=false,["crypto"]="QA_BASE64_NOT_ANDROID_KEYSTORE",["processRestart"]=false}.ToString());
   if(!SingleArchiveObservation)File.WriteAllText(Path.Combine(directory,"claim-response-identity.json"),new JObject{["bodyIsOriginalResponse"]=ReferenceEquals(wire.ClaimBody?.Target,original.AcceptedResponseJson),["strongReferenceUsedBeforeReleaseOnly"]=true}.ToString());
   archiveWeak.Add(new KeyValuePair<string,WeakReference>("httpClaimResponse",wire.ClaimResponse));archiveWeak.Add(new KeyValuePair<string,WeakReference>("httpClaimBody",wire.ClaimBody));archiveWeak.Add(new KeyValuePair<string,WeakReference>("qaWire",new WeakReference(wire)));archiveWeak.Add(new KeyValuePair<string,WeakReference>("durableClient",new WeakReference(client)));
   archiveWeak.Add(new KeyValuePair<string,WeakReference>("resultOwner",new WeakReference(state)));
   archiveWeak.Add(new KeyValuePair<string,WeakReference>("archive",new WeakReference(archive)));
   archiveWeak.Add(new KeyValuePair<string,WeakReference>("crypto",new WeakReference(crypto)));
   archiveWeak.Add(new KeyValuePair<string,WeakReference>("restoredOperation",new WeakReference(restored)));
   archiveWeak.Add(new KeyValuePair<string,WeakReference>("originalOperation",new WeakReference(original)));
   archiveWeak.Add(new KeyValuePair<string,WeakReference>("originalResponse",new WeakReference(original.AcceptedResponseJson)));
   restored=null;state.Dispose();state=null;
  }
  async Task RunTerminalEntryTrial(Wire wire,GameApiAuthSessionSnapshot session,DurableRunClient client,string run,string directory){
   state=new AccountScreenRuntime(wire,null,()=>session,()=>1);state.BindRunRecovery(client);
   bool baseline=variant=="RUN_TERMINAL_BASELINE";
   // The baseline UI called ReadRunResultsAsync directly. Reflection allows the
   // same QA assembly to exercise that pre-change entry without adding a shim
   // to production code. This is owner/API entry, not a native touch test.
   var entry=typeof(AccountScreenRuntime).GetMethod("EnsureRunResultsAsync");
   Require(baseline||entry!=null,"Current result entry missing");
   Func<Task> open=()=>baseline?state.ReadRunResultsAsync(run):(Task)entry.Invoke(state,new object[]{run});
   int before=wire.Rows.Count;memory.BeginWindow();await Task.WhenAll(open(),open());
   Require(state.Lifecycle?.NextAction=="RETURN_TO_LOBBY"&&state.VerifiedRunReceipt!=null,"Actual lifecycle receipt was not consumed");
   for(int i=0;i<4;i++){await open();await Task.Delay(100);}
   int entries=wire.Rows.Count-before;Require(entries==(baseline?6:1),"Unexpected result entry reads");
   await Task.Delay(500);memory.Checkpoint("02a-terminal-entry-reads");
   before=wire.Rows.Count;await state.ReadRunResultsAsync(run);Require(wire.Rows.Count-before==1,"Explicit refresh lost");
   Require(state.Lifecycle.ConfirmedOutcome=="VICTORY"&&state.VerifiedRunReceipt!=null,"Explicit terminal receipt lost");
   File.WriteAllText(Path.Combine(directory,"terminal-entry.json"),new JObject{["status"]="Passed",["entryLifecycleGets"]=entries,["explicitRefreshGets"]=1,["nativeUiTouchTest"]=false,["classification"]="ACTUAL_PLAYER_OWNER_ENTRY_REAL_HTTP_LIFECYCLE_SYNTHETIC_TERMINAL_SQL"}.ToString());
   state.Dispose();state=null;
  }
  sealed class RunConfirmJournal:IRunOperationJournal {
   public PendingRunOperation Row;public bool FailConfirm=true;public int Removes;
   public Task<IReadOnlyList<PendingRunOperation>> ReadAsync(string account)=>Task.FromResult<IReadOnlyList<PendingRunOperation>>(Row?.AccountId==account?new[]{Row}:Array.Empty<PendingRunOperation>());
   public Task<bool> PutAsync(PendingRunOperation value){if(Row!=null)return Task.FromResult(false);Row=value;return Task.FromResult(true);}
   public Task<bool> ConfirmAsync(PendingRunOperation value,string response){if(FailConfirm||Row==null)return Task.FromResult(false);Row=Row.Confirmed(response);return Task.FromResult(true);}
   public Task<bool> RemoveConfirmedAsync(string account,PendingRunKind kind,string key){if(Row?.AccountId!=account||Row.Kind!=kind||Row.IdempotencyKey!=key||Row.AcceptedResponseJson==null)return Task.FromResult(false);Row=null;Removes++;return Task.FromResult(true);}
  }
 }
}
#endif
