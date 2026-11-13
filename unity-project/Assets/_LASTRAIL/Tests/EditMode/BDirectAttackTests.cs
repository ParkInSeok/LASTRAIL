#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public class BDirectAttackTests {
  const string Root="D:/Project/LASTRAIL/";
  static JObject Owned()=>(JObject)JObject.Parse(File.ReadAllText(Root+"QA/CommanderS1-20260922/c3-owned-profile-observed-sql-test-20261001.json"))["profile"];
  static CombatDefinitionIndex Definition()=>CombatDefinitionIntake.Read(JObject.Parse(File.ReadAllText(Root+"QA/CommanderS1-20260922/c3-definition-intake-20261001.json")));
  static BCombatFormation Formation(int count=1,BCombatMode mode=BCombatMode.Solo)=>new BCombatFormation("train",mode,BCombatBattlefield.ExternalVertical,
   Enumerable.Range(0,count).Select(i=>new BCombatSlot("car"+i,i==0?"op_mira":null)));
  static JObject Exact(long n)=>new JObject{["numerator"]=n.ToString(),["denominator"]="1"};
  static JObject EmptyReductions()=>new JObject{["equipment"]=new JArray(),["guardStance"]=new JArray(),["independent"]=new JArray()};
  static JObject Entity(string id,string kind,int slot,int hp,JObject attack,int defense,long interval,params string[] targets)=>new JObject{
   ["id"]=id,["definitionId"]=kind=="OPERATOR"?"op_mira":"LOCAL_ORIGINAL_"+id,["kind"]=kind,["slot"]=slot,["maxHp"]=hp,["attack"]=attack==null?JValue.CreateNull():(JToken)attack.DeepClone(),
   ["defense"]=Exact(defense),["basicIntervalMicroseconds"]=interval,["attackSources"]=new JArray(),["defenseSources"]=new JArray(),
   ["effects"]=new JArray(),["weakening"]=new JArray(),["penetration"]=new JArray(),["reductions"]=EmptyReductions(),["allowedTargets"]=new JArray(targets)};
  static string FileHash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
  static JObject Packet(int count=1){
   var o=Owned();var mira=((JArray)o["operators"]).Single(x=>(string)x["operatorId"]=="op_mira")["permanentStats"];
   var originalEnemy=((JArray)JObject.Parse(File.ReadAllText(Root+"Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/tests/fixtures/ce03/combat-stats.json"))["enemies"]).Single(x=>(string)x["id"]=="enemy_scrapshield_brute");
   var entities=new JArray(Entity("train","TRAIN",0,5000,null,111,0));
   for(int i=0;i<count;i++)entities.Add(Entity("car"+i,"CARRIAGE",i,1000,null,87,0));
   entities.Add(Entity("op_mira","OPERATOR",0,(int)mira["maxHp"],(JObject)mira["attack"],(int)mira["defense"],(long)mira["basicIntervalMs"]*1000,"LOCAL_ENEMY"));
   // Only original enemy HP/ATK/period are reference inputs; old armor/kits/AI are not imported.
   entities.Add(Entity("enemy","LOCAL_ENEMY",0,(int)originalEnemy["hp"],Exact((long)originalEnemy["atk"]),500,(long)originalEnemy["attackMs"]*1000,"OPERATOR","CARRIAGE","TRAIN"));
   var geometry=new JArray();
   foreach(var target in entities.Where(x=>(string)x["id"]!="enemy")) {
    geometry.Add(new JObject{["sourceId"]="enemy",["targetId"]=target["id"].DeepClone(),["sourceSlot"]=0,["targetSlot"]=target["slot"].DeepClone(),["distanceMm"]=500,["maximumDistanceMm"]=1000,["observedTick"]=0,["validUntilTick"]=500000,["eligible"]=true});
   }
   geometry.Add(new JObject{["sourceId"]="op_mira",["targetId"]="enemy",["sourceSlot"]=0,["targetSlot"]=0,["distanceMm"]=500,["maximumDistanceMm"]=1000,["observedTick"]=0,["validUntilTick"]=500000,["eligible"]=true});
   var sourceFile=Root+"Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/tests/fixtures/ce03/combat-stats.json";
   return new JObject{["version"]=BDirectAttackInput.Version,["provenance"]="LOCAL_EXPLICIT_ORIGINAL_SOURCE_NOT_START",["formation"]=Formation(count).Capture(),["ownedProfileHash"]=BDirectAttackInput.Digest(o),
    ["definitionHash"]=Definition().ContentHash,["stepTimeSpanTicks"]=10000,["entities"]=entities,["geometry"]=geometry,["initialShields"]=new JArray(),
    ["sourceEvidence"]=new JArray(new JObject{["sourceId"]="REFERENCE_ONLY:ce03.enemy_scrapshield_brute.HP_ATK_PERIOD_NOT_ARMOR_AI",["sha256"]=FileHash(sourceFile)},
     new JObject{["sourceId"]="LOCAL_DISTANCE_500_RANGE_1000_DEF500_NOT_OPERATIONAL",["sha256"]=FileHash(Root+"QA/CoreLogic-20261001/NumericDefense/server-vectors.json")})};
  }
  static JObject Actor(JObject p,string id)=>(JObject)((JArray)p["entities"]).Single(x=>(string)x["id"]==id);
  static BDirectAttackInput Read(JObject p,int count=1)=>new BDirectAttackInput(p,BDirectAttackInput.Digest(p),Formation(count),Owned(),BDirectAttackInput.Digest(Owned()),Definition());
  static BDirectAttackSession Open(JObject p=null,int count=1,int cap=1000)=>new BDirectAttackSession(Read(p??Packet(count),count),cap);
  static void Advance(BDirectAttackSession s,int ms)=>s.Advance((long)ms*10000,ms);
  static JObject Shield(string target,int amount,long expiry=500000)=>new JObject{["targetId"]=target,["sourceEntity"]="LOCAL_ORIGINAL_SHIELD",["sourceAbility"]="LOCAL_SOURCE_RECEIPT",["effectIndex"]=0,["amount"]=amount,["expiryTick"]=expiry};
  [Test] public void ActualOwnedStatsPassiveNumericDefenseAndRealEnemyHpAreConnected(){
   var p=Packet();var input=Read(p);var session=new BDirectAttackSession(input,100);
   var receipt=session.ApplyBasicImpact("basic","op_mira","enemy",0);
   Assert.AreEqual(32,receipt.BeforeShield);Assert.AreEqual(32,receipt.Hp.HpLoss);Assert.AreEqual(668,session.Hp("enemy"));
   Assert.AreEqual("3201",(string)receipt.Calculation["exact"]["numerator"]);Assert.AreEqual("100",(string)receipt.Calculation["exact"]["denominator"]);
   Assert.IsFalse(input.OperationallyExecutable);Assert.IsFalse(Definition().CanExecute);
   File.WriteAllText(Root+"QA/CombatBMigration-20261002/AttackConsumer/local-original-input.json",p.ToString());
  }
  [Test] public void ActualLossTransferAfterDefenseAndShieldUsesOneHpOwner(){
   var p=Packet();Actor(p,"car0")["maxHp"]=20;((JArray)p["initialShields"]).Add(Shield("car0",10));((JArray)p["initialShields"]).Add(Shield("train",100));
   var s=Open(p);var r=s.ApplyBasicImpact("enemy-car","enemy","car0",0);
   Assert.AreEqual(36,r.BeforeShield);Assert.AreEqual(10,r.Hp.Absorbed);Assert.AreEqual(20,r.Hp.HpLoss);
   Assert.AreEqual(6,r.Hp.TransferToTrain);Assert.AreEqual(6,r.Hp.TrainHpLoss);Assert.AreEqual(4994,s.Hp("train"));Assert.AreEqual(1520,s.Hp("op_mira"));
   Assert.IsTrue(r.Hp.CarriageDown);Assert.AreSame(r,s.ApplyBasicImpact("enemy-car","enemy","car0",0));
  }
  [TestCase("train",36)][TestCase("op_mira",36)] public void DirectTrainAndOperatorNeverRouteCarriageTransfer(string target,int damage){
   var s=Open();var r=s.ApplyBasicImpact("direct","enemy",target,0);Assert.AreEqual(damage,r.BeforeShield);Assert.AreEqual(damage,r.Hp.HpLoss);Assert.AreEqual(0,r.Hp.TransferToTrain);
   Assert.AreEqual(1000,s.Hp("car0"));
  }
  [TestCase("enemy")][TestCase("car0")] public void FullShieldAbsorptionProducesNoHpOrTransfer(string target){
   var p=Packet();((JArray)p["initialShields"]).Add(Shield(target,100));var s=Open(p);var r=s.ApplyBasicImpact("shield",target=="enemy"?"op_mira":"enemy",target,0);
   Assert.AreEqual(r.BeforeShield,r.Hp.Absorbed);Assert.AreEqual(0,r.Hp.HpLoss);Assert.AreEqual(0,r.Hp.TransferToTrain);
  }
  [Test] public void FinalRoundingOccursAfterExactStatGroupsAndDefense(){
   var p=Packet();((JArray)Actor(p,"op_mira")["attackSources"]).Add(new JObject{["sourceId"]="LOCAL_EQ_ATK",["ownerId"]="op_mira",["group"]="EQUIPMENT_ATK",["valueBps"]=1000});
   var s=Open(p);Assert.AreEqual(35,s.ApplyBasicImpact("group","op_mira","enemy",0).BeforeShield);
  }
  [Test] public void PeriodAndOriginalClockPauseDebtAndSpeedAreReused(){
   var s=Open();s.ApplyBasicImpact("first","op_mira","enemy",0);var before=s.CaptureLocal();
   Assert.Throws<InvalidOperationException>(()=>s.ApplyBasicImpact("early","op_mira","enemy",0));Assert.AreEqual(before,s.CaptureLocal());
   s.SetPaused(true);Advance(s,1800);Assert.AreEqual(0,s.Tick);Assert.Throws<InvalidOperationException>(()=>s.ApplyBasicImpact("paused","enemy","train",0));
   s.SetPaused(false);s.SetSpeed(4);Advance(s,900);Assert.AreEqual(900,s.Tick);s.Advance(0,900);Assert.AreEqual(1800,s.Tick);
   Assert.AreEqual(32,s.ApplyBasicImpact("second","op_mira","enemy",1800).BeforeShield);
  }
  [TestCase("range")][TestCase("eligible")][TestCase("stale")][TestCase("kind")][TestCase("ability")]
  public void IneligibleDirectActionsAreRejectedBeforeAnyStateChange(string kind){
   var p=Packet();var g=(JObject)((JArray)p["geometry"]).Last();
   if(kind=="range")g["distanceMm"]=1001;else if(kind=="eligible")g["eligible"]=false;else if(kind=="stale")g["observedTick"]=1;
   var s=Open(p);var before=s.CaptureLocal();Assert.Throws<InvalidOperationException>(()=>s.ApplyBasicImpact("bad","op_mira",kind=="kind"?"car0":"enemy",0,kind=="ability"?"ACTIVE":"BASIC"));
   Assert.AreEqual(before,s.CaptureLocal());
  }
  [Test] public void SlotEnvelopeAlsoRequiredDespiteValidPhysicalDistance(){
   var p=Packet(5);Actor(p,"enemy")["slot"]=4;
   foreach(var g in (JArray)p["geometry"])if((string)g["sourceId"]=="enemy")g["sourceSlot"]=4;else if((string)g["targetId"]=="enemy")g["targetSlot"]=4;
   var s=Open(p,5);Assert.Throws<InvalidOperationException>(()=>s.ApplyBasicImpact("slot","op_mira","enemy",0));
  }
  [Test] public void BrokenCarriageDoesNotDeleteLivingOperatorAttack(){
   var p=Packet();Actor(p,"car0")["maxHp"]=20;var s=Open(p);s.ApplyBasicImpact("break","enemy","car0",0);
   Assert.AreEqual(0,s.Hp("car0"));Assert.AreEqual(32,s.ApplyBasicImpact("continue","op_mira","enemy",0).Hp.HpLoss);
  }
  [Test] public void DownAndDeadTargetsAndTerminalTrainCannotIssueNewAttack(){
   var p=Packet();Actor(p,"train")["maxHp"]=20;var s=Open(p);s.ApplyBasicImpact("end","enemy","train",0);
   Assert.IsTrue(s.TrainDefeated);var before=s.CaptureLocal();Assert.Throws<InvalidOperationException>(()=>s.ApplyBasicImpact("after","op_mira","enemy",0));Assert.AreEqual(before,s.CaptureLocal());
   s=Open();for(int i=0;i<43&&s.Hp("op_mira")>0;i++){s.ApplyBasicImpact("down"+i,"enemy","op_mira",s.Tick);Advance(s,2200);}
   Assert.AreEqual(0,s.Hp("op_mira"));Assert.Throws<InvalidOperationException>(()=>s.ApplyBasicImpact("dead-actor","op_mira","enemy",s.Tick));
  }
  [Test] public void OriginalImpactCacheRestoresHpTransferShieldClockAndReceiptOnce(){
   var p=Packet();Actor(p,"car0")["maxHp"]=20;((JArray)p["initialShields"]).Add(Shield("car0",10));var input=Read(p);var s=new BDirectAttackSession(input,100);
   s.ApplyBasicImpact("one","enemy","car0",0);Advance(s,50);s.ApplyBasicImpact("two","op_mira","enemy",s.Tick);
   var json=s.CaptureLocal();var r=BDirectAttackSession.RestoreLocal(json,input,100);Assert.AreEqual(json,r.CaptureLocal());
   var before=r.Hp("train");var receipt=r.ApplyBasicImpact("one","enemy","car0",0);Assert.AreEqual(6,receipt.Hp.TrainHpLoss);Assert.AreEqual(before,r.Hp("train"));
   Assert.Throws<InvalidOperationException>(()=>r.ApplyBasicImpact("one","enemy","train",0));
  }
  [TestCase("enemies")][TestCase("allies")][TestCase("clock")][TestCase("next")][TestCase("inputHash")]
  public void ModifiedSavedStateCannotBeReinterpreted(string key){
   var input=Read(Packet());var s=new BDirectAttackSession(input,100);s.ApplyBasicImpact("x","op_mira","enemy",0);
   var cache=JObject.Parse(s.CaptureLocal());cache[key]=new JObject();Assert.Throws<ArgumentException>(()=>BDirectAttackSession.RestoreLocal(cache.ToString(),input,100));
  }
  [Test] public void CapacityAndWrongTickFailuresAreAtomic(){
   var s=Open(cap:1);s.ApplyBasicImpact("x","op_mira","enemy",0);var before=s.CaptureLocal();
   Assert.Throws<InvalidOperationException>(()=>s.ApplyBasicImpact("y","enemy","train",0));Assert.AreEqual(before,s.CaptureLocal());
   s=Open();before=s.CaptureLocal();Assert.Throws<InvalidOperationException>(()=>s.ApplyBasicImpact("future","op_mira","enemy",1));Assert.AreEqual(before,s.CaptureLocal());
  }
  [Test] public void OriginalIntegerDefenseCannotBeReplacedByFractionWithSameFloor(){
   var p=Packet();Actor(p,"op_mira")["defense"]=new JObject{["numerator"]="223",["denominator"]="2"};
   Assert.Throws<ArgumentException>(()=>Read(p));
  }
  [Test] public void UnsupportedMagnitudeFailsBeforeHpAndClockOrPeriodPublication(){
   var p=Packet();((JArray)Actor(p,"op_mira")["attackSources"]).Add(new JObject{["sourceId"]="LOCAL_RANGE_TEST",["ownerId"]="op_mira",["group"]="EQUIPMENT_ATK",["valueBps"]=9007199254740991L});
   var s=Open(p);var before=s.CaptureLocal();Assert.Throws<OverflowException>(()=>s.ApplyBasicImpact("large","op_mira","enemy",0));
   Assert.AreEqual(before,s.CaptureLocal());
  }
  [Test] public void InputAndOriginalStatsAreImmutableAndTrustedHashIsNotSelfApproval(){
   var p=Packet();var hash=BDirectAttackInput.Digest(p);var input=Read(p);Actor(p,"op_mira")["attack"]=Exact(999);
   Assert.Throws<ArgumentException>(()=>new BDirectAttackInput(p,hash,Formation(),Owned(),BDirectAttackInput.Digest(Owned()),Definition()));
   var s=new BDirectAttackSession(input,100);Assert.AreEqual(32,s.ApplyBasicImpact("still-original","op_mira","enemy",0).BeforeShield);
   Assert.Throws<ArgumentException>(()=>Read(p));
   p=Packet();p["formation"]=Formation(mode:BCombatMode.WorldSolo).Capture();
   Assert.Throws<ArgumentException>(()=>new BDirectAttackInput(p,BDirectAttackInput.Digest(p),Formation(mode:BCombatMode.WorldSolo),Owned(),BDirectAttackInput.Digest(Owned()),Definition()));
  }
 }
}
#endif



