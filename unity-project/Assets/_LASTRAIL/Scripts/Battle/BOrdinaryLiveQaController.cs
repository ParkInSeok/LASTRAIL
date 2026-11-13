using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using LastRail.Run;
namespace LastRail.Battle {
 // LOCAL QA use-case owner. The existing native session owns all combat state and time.
 public sealed class BOrdinaryLiveQaController:IDisposable {
  readonly BDirectAttackBasicLoopBinding binding;readonly BInterceptTimingSnapshot receipt;
  public BOrdinaryGeometryBinding Geometry {get;}
  BDirectAttackBasicLoop native;long command,pendingElapsed;bool disposed,accepted,moved;long movedAt;
  public bool Started=>native!=null;public bool Paused=>native?.Paused??false;
  public string Outcome=>native?.Outcome??"READY";public long Tick=>native?.Tick??0;
  public int Hp=>native?.Hp("op_kyle")??0;
  public JObject Capture(){NeedAlive();return native?.Capture();}
  public static BOrdinaryLiveQaController Read(JObject fixture,string trustedHash){
   if(fixture==null||BDirectAttackInput.Digest(fixture)!=trustedHash||(string)fixture["version"]!="unity.combat-b.live-qa-fixture.v1.20261004"||(string)fixture["provenance"]!="EXPLICIT_LOCAL_TEST_NOT_ADMITTED_RUN")throw new ArgumentException("b_live_qa_fixture_identity");
   var p=(JObject)fixture.DeepClone();var raw=(string)p["buffDefinitionsJson"];string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw))).Replace("-","").ToLowerInvariant();var buffs=new BCarriageBuffDefinitions(raw,hash);var timing=(JObject)p["timingSource"];var r=BInterceptTimingSnapshot.Read(timing,(string)timing["sourceHash"],(JObject)p["trust"],buffs);
   var f=p["attack"]["formation"];var formation=new BCombatFormation((string)f["trainId"],(BCombatMode)Enum.Parse(typeof(BCombatMode),(string)f["mode"]),(BCombatBattlefield)Enum.Parse(typeof(BCombatBattlefield),(string)f["battlefield"]),f["slots"].Select(s=>new BCombatSlot((string)s["carriageId"],(string)s["operatorId"])),(string)f["internalNodeId"]);
   var profile=(JObject)p["profile"];var attack=(JObject)p["attack"];var input=BDirectAttackInput.ReadGunnerInterceptLocalRuntime(attack,BDirectAttackInput.Digest(attack),formation,profile,BDirectAttackInput.Digest(profile),CombatDefinitionIntake.Read((JObject)p["definitionIntake"]),r.OriginalSource.OriginalSource);var composition=(JObject)p["composition"];var c=new BDirectAttackBuffBinding(input,buffs,composition,BDirectAttackInput.Digest(composition));var routes=(JObject)p["routes"];var b=new BDirectAttackBasicLoopBinding(c,routes,BDirectAttackInput.Digest(routes));var definition=(JObject)p["geometryDefinition"];var asset=BOrdinaryGeometrySnapshot.Read(definition,BDirectAttackInput.Digest(definition));var instances=(JObject)p["geometryInstances"];var g=BOrdinaryGeometryBinding.Read(instances,BDirectAttackInput.Digest(instances),asset,c);return new BOrdinaryLiveQaController(b,r,g);
  }
  BOrdinaryLiveQaController(BDirectAttackBasicLoopBinding b,BInterceptTimingSnapshot r,BOrdinaryGeometryBinding g){binding=b;receipt=r;Geometry=g;}
  void NeedAlive(){if(disposed)throw new ObjectDisposedException(nameof(BOrdinaryLiveQaController));}
  string Id()=>"live-qa:"+(++command);
  public void Start(){NeedAlive();if(Started)throw new InvalidOperationException("QA run already started");Restart();}
  public void Restart(){NeedAlive();native=BDirectAttackBasicLoop.CreateOrdinaryGeometryRuntime(binding,receipt.OriginalSource,Geometry,10000,true);command=pendingElapsed=0;accepted=moved=false;movedAt=0;}
  public void SetPaused(bool value){NeedAlive();if(!Started)throw new InvalidOperationException("QA run not started");native.SetPaused(Id(),value);}
  public void MoveKyle(){NeedAlive();if(!Started)throw new InvalidOperationException();native.MoveOrdinaryOperator(Id(),"op_kyle",0,new JArray(6000,2400,0),Geometry.Hash);moved=true;movedAt=Tick;}
  public bool Intercept(){NeedAlive();if(!Started||accepted)return false;var shot=native.Capture()["gunnerIntercept"]["projectiles"].FirstOrDefault(p=>(string)p["terminal"]=="FLYING");if(shot==null)return false;native.AcceptInterceptionSource(Id(),(string)shot["id"],receipt);accepted=true;return true;}
  // Render elapsed debt is input batching, not a second simulation clock.
  public void SubmitElapsed(long elapsed,bool demonstration){NeedAlive();if(elapsed<0)throw new ArgumentOutOfRangeException(nameof(elapsed));if(!Started||Paused||Outcome!="RUNNING")return;pendingElapsed=checked(pendingElapsed+elapsed);while(pendingElapsed>=1000000&&Outcome=="RUNNING"){Advance(1000000,demonstration);pendingElapsed-=1000000;}}
  public void Advance(long elapsed,bool demonstration){NeedAlive();if(!Started||Paused||Outcome!="RUNNING")return;native.Advance(Id(),elapsed,2000);if(!demonstration)return;
   var snapshot=native.Capture();if(!accepted)Intercept();snapshot=native.Capture();if(!moved&&snapshot["gunnerIntercept"]["projectiles"].Any(p=>(string)p["terminal"]=="INTERCEPTED"))MoveKyle();
   if(!moved||Tick-movedAt<2000)return;
   var actions=native.Capture()["gunnerIntercept"]["interception"]["actions"];if(actions.Any(a=>(string)a["phase"]=="PREPARATION"||(string)a["phase"]=="RECOVERY"&&Tick<(long)a["endTick"]))return;
   foreach(var life in native.Capture()["boarding"]["lives"].Where(l=>(bool)l["registered"]&&(bool)l["targetable"]).ToArray()){if((long)native.Capture()["state"]["direct"]["next"]["op_kyle"]>Tick)break;var space=native.Capture()["spatialSource"];string target=(string)life["id"];var frame=native.ObserveOrdinaryGeometry("op_kyle",target,Tick,(long)space["revision"],Geometry.Hash);if((bool)frame["eligible"])native.ApplyWaveBasic(Id(),"op_kyle",target,(long)life["generation"]);}
  }
  public void Dispose(){if(disposed)return;disposed=true;native=null;}
 }
}
