#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LastRail.Diagnostics {
 // Explicit development-only protocol probe, never attached to production scenes.
 public sealed class RunRecoveryPlayerProbe:MonoBehaviour {
  [SerializeField]TMP_FontAsset font;GameObject view;
  public void Initialize(TMP_FontAsset value){font=value;}
  readonly JArray checks=new JArray();
  void Check(bool ok,string name){if(!ok)throw new InvalidOperationException(name);checks.Add(name);}
  async Task OpenView(AccountScreenRuntime state){
   view=new GameObject("ResultRecoveryPlayerUI",typeof(RectTransform),typeof(Canvas));((RectTransform)view.transform).sizeDelta=new Vector2(720,1280);
   new GameObject("SafeArea",typeof(RectTransform)).transform.SetParent(view.transform,false);
   var deck=LobbyCommandDeck.Attach(view,font,"LOCAL QA",0,new MissionShellMissionRowState[0],null,null);deck.BindAccount(state);deck.OpenRunLifecycle("run_probe");await Wait(state);
  }
  async Task Wait(AccountScreenRuntime state){for(int i=0;i<10000&&(state.Busy||state.RunResultsLoading);i++)await Task.Delay(1);Check(!state.Busy&&!state.RunResultsLoading,"bounded_ui_operation_finished");}
  async Task Recover(AccountScreenRuntime state){view.GetComponentsInChildren<Button>().Single(b=>b.name=="RecoverRunFinish").onClick.Invoke();await Wait(state);}
  string BodyText()=>view.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="DialogBody").text;
  sealed class FixtureCrypto:IAndroidKeystoreNativeStoragePort {
   public bool IsAndroidRuntime=>true;public bool IsKeystoreAvailable=>true;public bool UsedPlayerPrefsFallback=>false;public string ProviderName=>"QA_BASE64_NOT_ENCRYPTION";
   public AndroidKeystoreEncryptedTokenPayload Encrypt(string s)=>new AndroidKeystoreEncryptedTokenPayload(Convert.ToBase64String(Encoding.UTF8.GetBytes(s)),"fixture");
   public string Decrypt(AndroidKeystoreEncryptedTokenPayload p)=>Encoding.UTF8.GetString(Convert.FromBase64String(p.CipherTextBase64));
   public bool Save(AndroidKeystoreEncryptedTokenPayload p)=>throw new NotSupportedException();public AndroidKeystoreEncryptedTokenPayload Load()=>throw new NotSupportedException();public bool Clear()=>throw new NotSupportedException();
  }
  sealed class Actions:IAccountActionJournal {public PendingAccountAction Read(string id)=>null;public bool Save(PendingAccountAction a)=>throw new NotSupportedException();public bool Remove(PendingAccountAction a)=>throw new NotSupportedException();}
  const string Finish="{\"accepted\":false,\"runId\":\"run_probe\",\"resultStatus\":\"DUPLICATE_REPLAY\",\"rewardClaimId\":null,\"rewardPreviewId\":\"preview_probe\",\"serverReceivedAt\":\"2026-10-19T00:00:00.000Z\",\"balanceVersion\":\"1\",\"message\":\"Run finish was already accepted.\",\"errorCode\":\"duplicate_replay\"}";
  const string Claim="{\"runId\":\"run_probe\",\"rewardPreviewId\":\"preview_probe\",\"rewardClaimId\":\"claim_probe\",\"status\":\"CLAIMED\",\"serverClaimedAt\":\"2026-10-19T00:00:00.000Z\",\"balanceVersion\":\"1\",\"rewardLines\":[]}";
  static string Lifecycle()=>new JObject{["accepted"]=true,["version"]="run-lifecycle-v1",["serverTime"]=2000,["runId"]="run_probe",["runStatus"]="FAILED",["startResponse"]=new JObject{["accepted"]=true,["startStatus"]="STARTED",["runId"]="run_probe",["balanceVersion"]="1"},["finish"]=new JObject{["status"]="RECORDED",["outcome"]="FAILURE",["submittedAt"]=1000,["rewardPreviewId"]="preview_probe"},["reward"]=new JObject{["status"]="CLAIMED",["rewardPreviewId"]="preview_probe",["expiresAt"]=3000,["claimId"]="claim_probe",["receipt"]=new JObject{["rewardTransactionId"]="claim_probe",["runId"]="run_probe",["serverClaimedAt"]=1500,["rewardPlan"]=new JObject{["rewardLines"]=new JArray()}}},["nextAction"]="RETURN_TO_LOBBY",["disposition"]="REWARDED",["grantsOnRead"]=false,["newAttemptAuthorized"]=false,["uiReturnAllowed"]=true}.ToString(Newtonsoft.Json.Formatting.None);
  sealed class LocalServer:IDisposable {
   readonly TcpListener listener=new TcpListener(IPAddress.Loopback,0);public readonly JArray Requests=new JArray();public int FinishStatus=409;public bool LoseFinish;public string FirstBody,FirstKey;public bool OriginalsMatch=true;public int Errors;Task loop;
   public string Url{get;private set;}
   public void Start(){listener.Start();Url="http://127.0.0.1:"+((IPEndPoint)listener.LocalEndpoint).Port;loop=Serve();}
   async Task Serve(){try{while(true){using(var client=await listener.AcceptTcpClientAsync()){using(var stream=client.GetStream()){var reader=new StreamReader(stream,Encoding.UTF8,false,1024,true);var first=await reader.ReadLineAsync();if(first==null)continue;string key=null,line;int length=0;while(!string.IsNullOrEmpty(line=await reader.ReadLineAsync())){if(line.StartsWith("Content-Length:",StringComparison.OrdinalIgnoreCase))length=int.Parse(line.Substring(15).Trim());if(line.StartsWith("Idempotency-Key:",StringComparison.OrdinalIgnoreCase))key=line.Substring(16).Trim();}var chars=new char[length];int read=0;while(read<length){int n=await reader.ReadAsync(chars,read,length-read);if(n==0)break;read+=n;}string body=new string(chars,0,read),path=first.Split(' ')[1];Requests.Add(new JObject{["path"]=path,["key"]=key});int status=200;string response="{}";
    if(path.EndsWith("/finish")){if(FirstBody==null){FirstBody=body;FirstKey=key;}else OriginalsMatch&=FirstBody==body&&FirstKey==key;if(LoseFinish){LoseFinish=false;continue;}status=FinishStatus;response=status==200?Finish:new JObject{["accepted"]=false,["errorCode"]=status==409?"run_finish_request_conflict":"run_finish_receipt_unavailable"}.ToString();}
    else if(path.EndsWith("/lifecycle"))response=Lifecycle();
    else if(path=="/v1/client-errors"){Errors++;status=400;response="{\"accepted\":false,\"errorCode\":\"contract_violation\"}";}
    else status=404;
    byte[] payload=Encoding.UTF8.GetBytes(response),header=Encoding.ASCII.GetBytes("HTTP/1.1 "+status+" QA\r\nContent-Type: application/json\r\nConnection: close\r\nContent-Length: "+payload.Length+"\r\n\r\n");await stream.WriteAsync(header,0,header.Length);await stream.WriteAsync(payload,0,payload.Length);
   }}}}catch(ObjectDisposedException){}catch(SocketException){} }
   public void Dispose(){listener.Stop();}
  }
  async void Start(){
   var arg=Environment.GetCommandLineArgs().FirstOrDefault(x=>x.StartsWith("--recovery-qa="));if(arg==null)return;
   string output=Path.GetFullPath(arg.Substring(14));if(!output.StartsWith("D:\\Project\\LASTRAIL\\QA\\",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("D QA only");Directory.CreateDirectory(output);
   Exception failure=null;JArray requests=null;
   try{using(var server=new LocalServer()){server.Start();using(var adapter=new UnityWebRequestRunApiHttpAdapter(server.Url,10)){
    var wire=new UnityWebRequestRunApiHttpTransport(adapter);var session=new GameApiAuthSessionSnapshot("account_probe","ACTIVE",new GameApiAccessToken("lst1.synthetic"),DateTime.UtcNow.AddHours(1),new GameApiRefreshToken("synthetic"),DateTime.UtcNow.AddHours(2));long generation=1;
    var journal=new EncryptedRunOperationJournal(new AndroidEncryptedRunFilePort(new FixtureCrypto(),output+"/run-journal.json"));
    using(var runs=new DurableRunClient(new GameApiAuthenticatedHttpTransport(wire),journal,()=>session,()=>generation))using(var state=new AccountScreenRuntime(wire,new Actions(),()=>session,()=>generation)){
     state.BindRunRecovery(runs);await runs.RestoreAsync();
     if(Environment.GetCommandLineArgs().Contains("--restart")){
      Check(runs.Operations.Count==2&&runs.Operations.All(o=>o.AcceptedResponseJson!=null),"actual_process_restart_restores_both_receipts");await OpenView(state);await Recover(state);Check(server.Requests.All(r=>!((string)r["path"]).EndsWith("/finish")),"restart_confirmed_finish_posts_zero");Check(state.VerifiedRunReceipt!=null,"restart_paid_receipt_preserved");
     }else{
      var body=new JObject{["runId"]="run_probe",["encounterType"]="ORDINARY_15_WAVE",["outcome"]="FAILURE",["currentStage"]="Complete",["currentWaveNumber"]=0,["completedWaveCount"]=5,["runtimeStateJson"]="{\"runId\":\"run_probe\"}",["tacticSelections"]=new JArray(),["clientCompletedAt"]="2026-10-19T00:00:00.000Z"}.ToString(Newtonsoft.Json.Formatting.None);
      var pending=new PendingRunOperation(PendingRunKind.Finish,session.AccountId,"run_probe","original_finish_probe",body,new AppBuildSnapshot("QA",1),DateTimeOffset.UtcNow);
      Check(await journal.PutAsync(pending),"pending_saved_before_send");var claim=PendingRunOperation.Claim(session.AccountId,RunRewardClaimRequest.Create("run_probe","preview_probe","original_claim_probe"),new AppBuildSnapshot("QA",1),DateTimeOffset.UtcNow);Check(await journal.PutAsync(claim)&&await journal.ConfirmAsync(claim,Claim),"seeded_paid_claim_receipt");await runs.RestoreAsync();
      await OpenView(state);await Recover(state);Check(BodyText().Contains("일치하지")&&state.SavedFinish("run_probe").AcceptedResponseJson==null,"409_distinct_ui_pending_preserved");Check(state.VerifiedRunReceipt!=null,"409_paid_receipt_preserved");
      server.FinishStatus=503;await Recover(state);Check(BodyText().Contains("영수증")&&state.SavedFinish("run_probe").AcceptedResponseJson==null,"503_distinct_ui_pending_preserved");
      generation++;state.SynchronizeSession();await runs.RestoreAsync();Check(runs.LastErrorCode==null,"restored_generation_does_not_republish_old_error");
      server.FinishStatus=200;server.LoseFinish=true;await Recover(state);Check(state.SavedFinish("run_probe").AcceptedResponseJson==null,"lost_response_remains_pending");
      var retry=Recover(state);var duplicate=Recover(state);await Task.WhenAll(retry,duplicate);
      Check(state.SavedFinish("run_probe").AcceptedResponseJson==Finish,"duplicate_false_accepted_durably_confirmed");Check(server.OriginalsMatch&&server.Requests.Count(r=>(string)r["path"]=="/v1/runs/run_probe/finish")==4,"four_explicit_attempts_original_key_body_no_double_input");Check(state.VerifiedRunReceipt!=null&&state.SavedRewardClaim("run_probe").AcceptedResponseJson==Claim,"paid_claim_unchanged_no_claim_post");
      generation++;state.SynchronizeSession();Check(runs.LastErrorCode==null&&runs.Operations.Count==0,"old_generation_hidden");
      using(var errors=new CurrentSessionErrorRuntime(new CurrentSessionErrorClient(new GameApiAuthenticatedHttpTransport(wire),()=>session,()=>generation,()=>new AppBuildSnapshot("QA",1)))){Check(errors.Record(SafeClientErrorKind.UnityError,generation),"explicit_safe_producer");await errors.FlushAsync();await errors.FlushAsync();Check(errors.RejectedDeliveryCount==1&&errors.PendingCount==0&&server.Errors==1,"400_report_once_no_recursive_or_rekey");}
     }
    }if(view!=null)Destroy(view);requests=(JArray)server.Requests.DeepClone();
   }}}
   catch(Exception error){failure=error;}
   File.WriteAllText(output+(Environment.GetCommandLineArgs().Contains("--restart")?"/restart.json":"/player.json"),new JObject{["status"]=failure==null?"Passed":"Failed",["error"]=failure?.ToString(),["checks"]=checks,["requests"]=requests,["scope"]="WINDOWS_PLAYER_PRODUCTION_CLIENT_WITH_LOOPBACK_SYNTHETIC_LEGACY_PROTOCOL_NOT_SERVER_GAMEPLAY",["crypto"]="BASE64_FIXTURE_NOT_ANDROID_KEYSTORE",["nativeCrashTested"]=false}.ToString());Application.Quit(failure==null?0:1);
  }
 }
}
#endif
