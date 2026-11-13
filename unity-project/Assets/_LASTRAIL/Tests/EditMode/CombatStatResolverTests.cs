#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class CombatStatResolverTests {
  static Type Resolver {get {var t=typeof(CombatVitals).Assembly.GetType("LastRail.Battle.CombatStatResolver");Assert.NotNull(t,"Approved stat resolver missing");return t;}}
  static CombatExactValue V(string n,string d="1")=>CombatExactValue.From(new JObject{["numerator"]=n,["denominator"]=d});
  static CombatExactValue Call(string method,params object[] args){try{return (CombatExactValue)Resolver.GetMethod(method).Invoke(null,args);}catch(TargetInvocationException e){throw e.InnerException;}}
  static JObject S(string id,string group,long bps,string owner="a")=>new JObject{["sourceId"]=id,["ownerId"]=owner,["group"]=group,["valueBps"]=bps};
  static JArray Sources(params JObject[] s)=>new JArray(s);
  static void Exact(CombatExactValue v,string n,string d="1")=>Assert.IsTrue(JToken.DeepEquals(v.Snapshot(),V(n,d).Snapshot()));
  [TestCase(1,"100")][TestCase(30,"201")][TestCase(50,"271")]
  public void OperatorAttackLevelGrowthIsAdditiveAndFloored(int level,string value)=>Exact(Call("OperatorLevelAttack",100,level),value);
  [TestCase(1,"100")][TestCase(30,"216")][TestCase(50,"296")]
  public void OperatorHpLevelGrowthIsAdditiveAndFloored(int level,string value)=>Exact(Call("OperatorLevelMaxHp",100,level),value);
  [Test] public void OnlyExplicitAnchorDefenseUsesLevelGrowth(){Exact(Call("LevelDefense",220,50,false),"220");Exact(Call("LevelDefense",220,30,true),"443");Exact(Call("LevelDefense",220,50,true),"597");}
  [Test] public void TurretUsesOwnLevelAndKeepsAttackFraction(){Exact(Call("TurretLevelAttack",34,6),"221","5");Exact(Call("TurretLevelMaxHp",601,6),"841");}
  [Test] public void AttackGroupsSumWithinAndMultiplyAcrossWithNoEquipmentCap(){var s=Sources(S("e1","EQUIPMENT_ATK",35000),S("e2","EQUIPMENT_ATK",35000),S("e3","EQUIPMENT_ATK",35000),S("x","AUGMENT_ATK",5000),S("c","COMMANDER_ATK",1000));Exact(Call("Attack","a",V("100"),s),"3795","2");}
  [Test] public void EffectiveAttackKeepsFractionsUntilDamageBoundary(){var atk=Call("Attack","a",V("1"),Sources(S("e","EQUIPMENT_ATK",2500),S("x","AUGMENT_ATK",5000)));Exact(atk,"15","8");var input=new JObject{["version"]="combat.numeric-defense-budget.v1",["base"]=atk.Multiply(V("2")).Snapshot(),["defense"]=V("0").Snapshot(),["effects"]=new JArray(),["weakening"]=new JArray(),["penetration"]=new JArray(),["reductions"]=new JObject{["equipment"]=new JArray(),["guardStance"]=new JArray(),["independent"]=new JArray()}};Assert.AreEqual(3,CombatNumericDefenseBudget.Resolve(input).ToVitalsMagnitude());}
  [Test] public void DuplicatesAreOnceConflictingSourceOrWrongOwnerFails(){var s=S("e","EQUIPMENT_ATK",35000);Exact(Call("Attack","a",V("100"),Sources(s,(JObject)s.DeepClone())),"450");Assert.Throws<ArgumentException>(()=>Call("Attack","a",V("100"),Sources(s,S("e","EQUIPMENT_ATK",1000))));Assert.Throws<ArgumentException>(()=>Call("Attack","a",V("100"),Sources(s,S("e","AUGMENT_ATK",35000))));Assert.Throws<ArgumentException>(()=>Call("Attack","a",V("100"),Sources(S("b","EQUIPMENT_ATK",35000,"b"))));}
  [Test] public void EquipmentHpReusesExistingMaxHpOwnerAndRestore(){var hp=Call("EquipmentMaxHp","a",V("1000"),Sources(S("e1","EQUIPMENT_HP",1000),S("e2","EQUIPMENT_HP",2500)));var v=new CombatVitals(1000,4000,true);v.Damage(400,0,false);v.RebaseMaxHp(checked((int)hp.Floor()));Assert.AreEqual(810,v.Hp);v.RebaseMaxHp(1000);Assert.AreEqual(600,v.Hp);}
  [Test] public void EquipmentDefenseDoesNotBecomeArmorPercentage(){var def=Call("EquipmentDefense","a",V("443"),Sources(S("d","EQUIPMENT_DEF",35000)));Exact(def,"3987","2");}
  [TestCase(1000,"110")][TestCase(2500,"125")][TestCase(6000,"160")][TestCase(15000,"250")][TestCase(35000,"450")]
  public void ApprovedEquipmentAttackGradesUseTheirPublishedValues(long bps,string expected)=>Exact(Call("Attack","a",V("100"),Sources(S("e","EQUIPMENT_ATK",bps))),expected);
  [Test] public void ResolvedMaxHpContinuesThroughExistingModelSaveRestore(){
   Func<BasicCombatSimulation> fresh=()=>new BasicCombatSimulation("TEST_ONLY_stat_resolver",10000,1u,new[]{new BasicCombatActor("a",false,true,false,0,1000,0,0,100,1,0,BasicCombatTarget.EnemyAny,4000)},new[]{new[]{new BasicCombatActor("e",true,false,false,0,1000,100,0,100,1,0,BasicCombatTarget.Train,4000)}},true);
   var hp=Call("EquipmentMaxHp","a",V("1000"),Sources(S("h","EQUIPMENT_HP",35000)));var model=fresh();model.ApplyResolvedMaxHp("a",checked((int)hp.Floor()));var restored=BasicCombatSimulation.RestoreLocalState(model.CaptureLocalState(),fresh);model.Advance(10000000,1000);restored.Advance(10000000,1000);Assert.AreEqual(model.CaptureLocalState(),restored.CaptureLocalState());Assert.Greater(model.Events.Count,0);
  }
  [Test] public void MissingUnsafeOrExtraSourceFieldsFail(){var s=S("e","EQUIPMENT_ATK",0);s["valueBps"]=1000.0;Exact(Call("Attack","a",V("100"),Sources(s)),"110");s["valueBps"]=9007199254740992L;Assert.Throws<ArgumentException>(()=>Call("Attack","a",V("100"),Sources(s)));s["valueBps"]=1.5;Assert.Throws<ArgumentException>(()=>Call("Attack","a",V("100"),Sources(s)));s["valueBps"]=0;s["slot"]=1;Assert.Throws<ArgumentException>(()=>Call("Attack","a",V("100"),Sources(s)));Assert.Throws<ArgumentException>(()=>Call("Attack","a",V("100"),new JArray(JValue.CreateNull())));Exact(Call("EquipmentMaxHp","a",V("3"),Sources(S("h","EQUIPMENT_HP",2500))),"3");}
  [Test] public void DecimalFractionsCannotRoundIntoIntegralBonuses(){foreach(var value in new[]{1.0000000000000000000000000001m,9007199254740990.5m}){var s=S("e","EQUIPMENT_ATK",0);s["valueBps"]=new JValue(value);Assert.Throws<ArgumentException>(()=>Call("Attack","a",V("100"),Sources(s)));}}
  [Test] public void SafeIntegerAndNonFiniteBoundariesAreExplicit(){var s=S("e","EQUIPMENT_ATK",9007199254740991L);Assert.NotNull(Call("Attack","a",V("100"),Sources(s)));foreach(var value in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity}){s["valueBps"]=new JValue(value);Assert.Throws<ArgumentException>(()=>Call("Attack","a",V("100"),Sources(s)));}}
  [Test] public void EmptyGroupsPreserveAlreadyGrownStats(){Exact(Call("Attack","a",V("221","5"),new JArray()),"221","5");Exact(Call("EquipmentDefense","a",V("443"),new JArray()),"443");}
  [Test] public void InvalidInputsFailWithoutMutatingSourceJson(){var s=Sources(S("e","EQUIPMENT_ATK",1000));var copy=s.DeepClone();Call("Attack","a",V("100"),s);Assert.IsTrue(JToken.DeepEquals(copy,s));Assert.Throws<ArgumentException>(()=>Call("Attack","a",V("100"),Sources(S("h","EQUIPMENT_HP",1000))));Assert.Throws<ArgumentException>(()=>Call("Attack","a",V("100"),Sources(S("e","EQUIPMENT_ATK",-1))));Assert.Throws<ArgumentException>(()=>Call("Attack","a",V("100"),new JArray(Enumerable.Range(0,1025).Select(i=>S(i.ToString(),"EQUIPMENT_ATK",0)))));Assert.Throws<ArgumentException>(()=>Call("OperatorLevelAttack",100,0));Assert.Throws<ArgumentException>(()=>Call("OperatorLevelMaxHp",100,51));Assert.Throws<ArgumentException>(()=>Call("TurretLevelAttack",100,11));Assert.Throws<ArgumentException>(()=>Call("EquipmentDefense","a",null,new JArray()));}
 }
}
#endif
