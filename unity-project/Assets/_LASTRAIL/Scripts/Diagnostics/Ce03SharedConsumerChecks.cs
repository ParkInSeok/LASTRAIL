#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
namespace LastRail.Diagnostics {
// Shared executed QA assertions, never an admission issuer. Each instance owns one prepare.
public sealed class Ce03SharedConsumerChecks {
 const long Step=500000;
 readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
 readonly JObject prepared;readonly Ce03CommandContract commands;readonly string owner;
 readonly JArray cases=new JArray(),results=new JArray();JObject initial;bool ran;
 public JArray Cases=>(JArray)cases.DeepClone();public JArray Results=>(JArray)results.DeepClone();
 public Ce03SharedConsumerChecks(JObject source){prepared=(JObject)source.DeepClone();owner=(string)prepared["ownedSelection"]?["accountId"];commands=new Ce03CommandContract(prepared["commandContract"] as JObject);commands.ValidatePrepared(prepared,owner,commands.Revision);if((string)prepared["commandContract"]["version"]!=Ce03CommandContract.Version)throw new NotSupportedException("This runner verifies legacy command v1 only.");}
 string C(JToken j)=>Ce03CommandContract.Canonical(j);
 void Check(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
 void Reject(Action action){try{action();}catch(ArgumentException){return;}catch(FormatException){return;}catch(NotSupportedException){return;}throw new InvalidOperationException("Expected rejection did not occur.");}
 BasicCombatSimulation Fresh()=>commands.CreateLocalTest(prepared,owner,commands.Revision,1);
 void Case(string id,string coverage,JObject input,JObject expected,Action check){check();cases.Add(new JObject{["id"]=id,["coverage"]=coverage,["input"]=input,["expected"]=expected});results.Add(new JObject{["caseId"]=id,["coverage"]=coverage,["status"]="PASS"});}
 JObject Values(params string[] values)=>new JObject{["checks"]=new JArray(values)};
 public JObject CheckpointSchema {get{if(!ran||initial==null||results.Count!=10)throw new InvalidOperationException("Checks have not completed.");return new JObject{["schemaVersion"]="ce03-checkpoint-implementation-shape-v1",["implementation"]="Assets/_LASTRAIL/Scripts/Battle/CombatLocalCheckpoint.cs",["stateVersion"]=initial["schema"]?.DeepClone(),["topLevelTypes"]=new JObject(initial.Properties().OrderBy(p=>p.Name,StringComparer.Ordinal).Select(p=>new JProperty(p.Name,p.Value.Type.ToString()))),["scope"]="Observed actual checkpoint types; strict recursive validation is in pinned implementation; roundtrip/corruption executed."};}}
 public void Run(string output,string recoveryFixture){if(ran)throw new InvalidOperationException("Checks cannot be replayed on an existing instance.");ran=true;
   initial=JObject.Parse(Fresh().CaptureLocalState());
   Case("strict-prepared-profile","STRICT_PROFILE",Values("supplied owner/carriage/draft/commands/manifest and prepareSha256"),Values("real prepared model loads","unknown policy and changed ownership reject"),()=>{
    Fresh();var p=(JObject)prepared.DeepClone();p["draft"]["template"]["execution"]["policyVersion"]="unknown";Reject(()=>BasicCombatSimulation.FromPreparedProfileForLocalTest(p,(JObject)p["ownedSelection"],owner,1));
    p=(JObject)prepared.DeepClone();p["carriageOwnership"]["carriages"][0]["revision"]=9007199254740991L;Reject(()=>BasicCombatSimulation.FromPreparedProfileForLocalTest(p,(JObject)p["ownedSelection"],owner,1));
    p=(JObject)prepared.DeepClone();if(p["playerAssetBinding"]!=null)p["playerAssetBinding"]["manifestSha256"]=new string('0',64);else p["presentationManifest"]["sourceFiles"][0]["rawSha256"]=new string('0',64);Reject(()=>commands.ValidatePrepared(p,owner,commands.Revision));
   });
   RunEffects();
   Case("ordered-three-manuals","SUPPORT_SELECTION",new JObject{["slots"]=prepared["commandContract"]["support"]["slots"].DeepClone()},Values("HUD order equals saved slots","duplicate and unknown effect rejected"),()=>{
    var s=Fresh();Check(((JArray)s.ReadHudState()["manuals"]).Select(x=>(string)x["ability"]).SequenceEqual(commands.SupportIds),"Saved manual order lost.");
    var cc=(JObject)prepared["commandContract"].DeepClone();cc["support"]["slots"][0]["skillId"]=cc["support"]["slots"][1]["skillId"].DeepClone();Reject(()=>new Ce03CommandContract(cc));cc=(JObject)prepared["commandContract"].DeepClone();cc["support"]["slots"][0]["skillId"]="ce03.role.smoke";Reject(()=>new Ce03CommandContract(cc));
   });
   Case("manual-apply-and-idempotence","MANUAL_COMMANDS",new JObject{["manuals"]=new JArray(commands.SupportIds),["seed"]=1},Values("each manual applies","original repeated command does not change model"),()=>{
    foreach(string ability in commands.SupportIds){var s=Fresh();s.Advance(Step*12,12);var row=((JArray)s.ReadHudState()["manuals"]).Single(x=>(string)x["ability"]==ability);Check(s.RequestSkill("manual",(string)row["actor"],ability)=="QUEUED","Manual not queued.");for(int i=0;i<400&&s.CommandStatus("manual")!="APPLIED";i++)s.Advance(Step,1);Check(s.CommandStatus("manual")=="APPLIED","Manual not applied.");var before=s.CaptureLocalState();s.RequestSkill("manual",(string)row["actor"],ability);Check(before==s.CaptureLocalState(),"Repeated manual changed model.");}
   });
   string final=null;
   Case("fifteen-wave-script","FIFTEEN_WAVES",Values("test seed 1; 2000 blocks maximum, ten 50ms steps per block","every block%50==1: five actives, selected ultimate, support_fire"),Values("Victory","15 completed waves","more than 100 real hit events"),()=>{var s=Fresh();Script(s);Check(s.Outcome==BasicCombatOutcome.Victory&&s.CompletedWaves==15&&s.Events.Count>100,"Fifteen-wave completion failed.");final=s.CaptureLocalState();});
   Case("repeat-full-script","DETERMINISM",Values("same prepared bytes, test seed and commands"),Values("byte-identical final checkpoint including event history"),()=>{var s=Fresh();Script(s);Check(final==s.CaptureLocalState(),"Determinism mismatch.");});
   Case("mid-cast-checkpoint","CHECKPOINT_RESTORE",Values("12 steps; five active requests; nine steps; serialize/restore","paused debt and speed roundtrip"),Values("byte-identical continuation and completed 15 waves","corrupt fingerprint rejected"),()=>{
    var s=Fresh();s.Advance(Step*12,12);foreach(string id in Ce03PresentationManifest.Actors.Where(x=>x.StartsWith("op_",StringComparison.Ordinal)))s.RequestSkill("restore-"+id,id,id+".active");s.Advance(Step*9,9);string raw=s.CaptureLocalState();var r=BasicCombatSimulation.RestoreLocalState(raw,()=>Fresh());Check(raw==r.CaptureLocalState(),"Initial restore mismatch.");s.Advance(Step*20000,20000);r.Advance(Step*20000,20000);Check(s.CaptureLocalState()==r.CaptureLocalState()&&r.CompletedWaves==15,"Restored continuation mismatch.");
    var bad=JObject.Parse(raw);bad["configurationSha256"]=new string('0',64);Reject(()=>BasicCombatSimulation.RestoreLocalState(C(bad),()=>Fresh()));
    s=Fresh();s.SetSpeed(3);s.Advance(Step*3,1);s.SetPaused(true);r=BasicCombatSimulation.RestoreLocalState(s.CaptureLocalState(),()=>Fresh());Check(r.PendingSteps==3,"Debt lost.");r.Advance(Step,10);Check(r.Tick==1,"Paused clock moved.");r.SetPaused(false);r.Advance(0,10);Check(r.Tick==4,"Debt not resumed.");
   });
   Case("original-owner-boundary","OWNER_ISOLATION",Values("owner from full prepare","foreign owner is only a negative case"),Values("foreign caller/revision rejects","driver owner switch blocks input"),()=>{
    Reject(()=>commands.ValidatePrepared(prepared,owner+".foreign",commands.Revision));Reject(()=>commands.ValidatePrepared(prepared,owner,commands.Revision+1));
    Reject(()=>BasicCombatSimulation.FromPreparedProfileForLocalTest(prepared,(JObject)prepared["ownedSelection"],owner+".foreign",1));
    bool current=true;var d=new CombatSceneDriver(()=>Fresh(),()=>null,(s,r)=>r+1,()=>current);d.Open();current=false;bool rejected=false;try{d.SetPaused(true);}catch(InvalidOperationException){rejected=true;}Check(rejected&&d.RecoveryRequired,"Owner change not latched.");
   });
   Case("journal-loss-and-save-boundary","JOURNAL_RECOVERY",Values("fault-injected local transport; same supplied owner and ordered skills","actual temporary file/write-through/replace/reload; not OS power-loss proof","separate labeled Server synthetic r6 fixture: original START request/lookup/replay/seed; never model execution"),Values("request preserved before send","lost response keeps original key/body","failed save leaves model unchanged"),()=>{Journal(output);Ce03R6RecoveryChecks.Run(Ce03R6RecoveryChecks.ReadJson(recoveryFixture));});
   Case("charge-full-consume-restore","LEADER_CHARGE",Values("unmodified prepared model; explicit local basic HP-hit policy4","pause before advancing, restore full gauge, cast selected ultimate"),Values("0 during pause","100 cap","accepted cast consumes once"),()=>{
    var s=Fresh();Check((int)s.ReadHudState()["ultimate"]["charge"]==0,"Initial charge.");s.SetPaused(true);s.Advance(Step*200,200);Check((int)s.ReadHudState()["ultimate"]["charge"]==0,"Paused charge changed.");s.SetPaused(false);for(int i=0;i<1200&&(int)s.ReadHudState()["ultimate"]["charge"]<100;i++)s.Advance(Step,1);Check((int)s.ReadHudState()["ultimate"]["charge"]==100,"Charge did not fill.");var r=BasicCombatSimulation.RestoreLocalState(s.CaptureLocalState(),()=>Fresh());var u=r.ReadHudState()["ultimate"];r.RequestSkill("charged",(string)u["actor"],(string)u["ability"]);for(int i=0;i<150&&r.CommandStatus("charged")=="QUEUED";i++)r.Advance(Step,1);Check(r.CommandStatus("charged")=="CASTING"&&(int)r.ReadHudState()["ultimate"]["charge"]<100,"Full gauge did not consume.");string before=r.CaptureLocalState();r.RequestSkill("charged",(string)u["actor"],(string)u["ability"]);Check(before==r.CaptureLocalState(),"Duplicate consumed again.");
   });
   Check(results.Count==10&&results.Select(x=>(string)x["coverage"]).Distinct().Count()==10,"Incomplete model coverage.");
 }
 void Script(BasicCombatSimulation s){for(int block=0;block<2000&&s.Outcome==BasicCombatOutcome.Running;block++){if(block%50==1){var hud=s.ReadHudState();foreach(var a in (JArray)hud["skills"])s.RequestSkill(block+"-"+(string)a["actor"],(string)a["actor"],(string)a["ability"]);var u=hud["ultimate"];s.RequestSkill(block+"-ultimate",(string)u["actor"],(string)u["ability"]);var fire=((JArray)hud["manuals"]).Single(x=>(string)x["ability"]=="support_fire");s.RequestSkill(block+"-fire",(string)fire["actor"],"support_fire");}s.Advance(Step*10,10);}}
 void RunEffects(){Case("isolated-exact-effects","FULL_EFFECTS",Values("supplied prepared template skill/effect definitions","isolated allies hp1000 atk50; train maxHp5000 atk60; enemy hp100000 atk0","initial Kyle hp300/train hp3000/skillReady0; non-live numeric fixture"),Values("Reina active240 ultimate540; Sera active108 ultimate189; support259 HP loss","Kyle shields198 self and242 all5; taunt1 interior","Eve resultingHP685/740; Mira train3275/3550 buff1500bps","train shield440, repair3400; mark1000bps and pending-attack interruption"),()=>{
   var e=new Ce03ConsumerEffectChecks(prepared);e.AuthoredDamageProducesExactHpLoss("op_reina","active",240);e.AuthoredDamageProducesExactHpLoss("op_reina","ultimate",540);e.AuthoredDamageProducesExactHpLoss("op_sera","active",108);e.AuthoredDamageProducesExactHpLoss("op_sera","ultimate",189);e.AuthoredDamageProducesExactHpLoss("train","support_fire",259);e.ShieldHasExactMagnitudeAndCorrectRecipients("active",198);e.ShieldHasExactMagnitudeAndCorrectRecipients("ultimate",242);e.HealChangesDamagedHpExactly("active",685);e.HealChangesDamagedHpExactly("ultimate",740);e.RepairAndBuffApplyToTrain("active",3275);e.RepairAndBuffApplyToTrain("ultimate",3550);e.TrainShieldAndRepairHaveDistinctOutcomes();e.MarkValueAndInterruptRemoveFutureEnemyAttack();
  });}
 sealed class FaultWire:IRunApiHttpTransport {public bool Lost;public readonly List<RunApiHttpRequest> Requests=new List<RunApiHttpRequest>();public Task<RunApiHttpResponse> SendAsync(RunApiHttpRequest r){Requests.Add(r);if(Lost)return Task.FromResult(RunApiHttpResponse.NetworkError("injected"));var b=JObject.Parse(r.BodyJson);return Task.FromResult(RunApiHttpResponse.Success(200,Ce03CommandContract.Canonical(new JObject{["rulesVersion"]=b["rulesVersion"],["catalogVersion"]=b["catalogVersion"],["revision"]=(long)b["expectedRevision"]+1,["slots"]=b["slots"]})));}}
 void Journal(string output){
  string file=Path.Combine(output,"journal-check.tmp.json");void Persist(string raw){string temp=file+".pending";using(var s=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){byte[] bytes=Utf8.GetBytes(raw);s.Write(bytes,0,bytes.Length);s.Flush(true);}if(File.Exists(file))File.Replace(temp,file,null);else File.Move(temp,file);}
  var wire=new FaultWire{Lost=true};var uri=new Uri("http://127.0.0.1:12345/");var token=new GameApiAccessToken("lst1.test-runner");var client=new Ce03LocalClient(uri,owner,wire,token,Persist);bool lost=false;try{client.SaveSupportAsync(commands.Revision,commands.SupportIds).GetAwaiter().GetResult();}catch(InvalidOperationException){lost=true;}Check(lost&&client.HasPending&&wire.Requests.Count==1,"Lost reply boundary.");string saved=File.ReadAllText(file);Reject(()=>new Ce03LocalClient(uri,owner+".foreign",wire,token,Persist,saved));
  wire.Lost=false;var restored=new Ce03LocalClient(uri,owner,wire,token,Persist,saved);restored.RetryAsync().GetAwaiter().GetResult();Check(!restored.HasPending&&wire.Requests.Count==2&&wire.Requests[0].BodyJson==wire.Requests[1].BodyJson&&wire.Requests[0].Headers.Select(h=>h.Value).SequenceEqual(wire.Requests[1].Headers.Select(h=>h.Value)),"Original request changed.");
  var silent=new FaultWire();var failed=new Ce03LocalClient(uri,owner,silent,token,_=>throw new IOException("injected"));try{failed.SaveSupportAsync(commands.Revision,commands.SupportIds).GetAwaiter().GetResult();throw new InvalidOperationException("Expected persistence failure.");}catch(IOException){}Check(silent.Requests.Count==0,"Sent before save.");
  bool fail=false;var driver=new CombatSceneDriver(()=>Fresh(),()=>null,(s,r)=>{if(fail)throw new IOException("injected");return r+1;},()=>true);driver.Open();string before=driver.StateJson;fail=true;try{driver.Advance(Step,1);throw new InvalidOperationException("Expected driver save failure.");}catch(IOException){}Check(driver.StateJson==before&&driver.RecoveryRequired,"Failed driver save published state.");
 }
}
}
#endif
