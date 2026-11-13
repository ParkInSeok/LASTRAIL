using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class CombatElectromagneticStateTests {
 [Test] public void MissingInvalidOrChangedClockCannotAlterBoundReader(){
  foreach(var value in new JToken[]{null,new JValue(0),new JValue(-1),new JValue(.5)}){var invalid=Hud(0,0);if(value==null)invalid.Remove("stepTimeSpanTicks");else invalid["stepTimeSpanTicks"]=value;var error=Assert.Throws<System.Reflection.TargetInvocationException>(()=>Make(invalid));Assert.That(error.InnerException,Is.TypeOf<ArgumentException>());}
  var initial=Hud(0,0);var reader=Make(initial);var changed=Hud(2,100,20);changed["stepTimeSpanTicks"]=1000000;var failure=Assert.Throws<System.Reflection.TargetInvocationException>(()=>Read(reader,changed));Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());Assert.That(Read(reader,initial).ToString(),Is.EqualTo(Read(Make(initial),initial).ToString()));
 }
 [Test] public void ApproachUsesSameClockAsAbsorption(){
  var go=new UnityEngine.GameObject("ClockField",typeof(UnityEngine.RectTransform));var train=new UnityEngine.GameObject("Train",typeof(UnityEngine.RectTransform));
  try{var field=go.AddComponent<LastRail.Battle.CombatElectromagneticField>();var hud=Hud(0,100);hud["stepTimeSpanTicks"]=1000000;hud["motionSeconds"]=.33;hud["actors"][0]["enemy"]=false;
   var enemy=new JObject{["id"]="enemy",["train"]=false,["enemy"]=true,["attackTarget"]="train_a",["attackDue"]=4};((JArray)hud["actors"]).Add(enemy);
   field.Bind((UnityEngine.RectTransform)train.transform,hud);
   Func<int> vertices=()=>{field.Refresh(hud,_=>new UnityEngine.Vector3(1000,100,0));using(var mesh=new UnityEngine.UI.VertexHelper()){typeof(LastRail.Battle.CombatElectromagneticField).GetMethod("OnPopulateMesh",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.DeclaredOnly).Invoke(field,new object[]{mesh});return mesh.currentVertCount;}};
   int approaching=vertices();enemy["attackTarget"]="other";Assert.That(approaching,Is.EqualTo(vertices()+8),"At 330ms a reserved 400ms impact should show its approach line.");
  }finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(train);}
 }
 [TestCase(200000)][TestCase(1000000)]
 public void RealClockPauseAndRestorePreserveOwnerShieldTiming(long step){
  Func<LastRail.Battle.BasicCombatSimulation> fresh=()=>new LastRail.Battle.BasicCombatSimulation("TEST_ONLY_clock",step,1,new[]{new LastRail.Battle.BasicCombatActor("train",false,true,false,0,1000,0,0,100,1,0,LastRail.Battle.BasicCombatTarget.EnemyAny,4000)},new[]{new[]{new LastRail.Battle.BasicCombatActor("enemy",true,false,false,0,1000,100,0,100,1,0,LastRail.Battle.BasicCombatTarget.Train,4000)}},true);
  var model=fresh();model.ApplyResolvedShield("train",new LastRail.Battle.CombatShieldSource("train","TEST",0),1000,3);var reader=Make(model.ReadHudState());model.Advance(step*2,2);
  var frame=Read(reader,model.ReadHudState());Assert.That((double)frame["absorptions"][0]["age"],Is.Zero.Within(.000001));model.SetPaused(true);model.Advance(step*100,100);Assert.That(Read(reader,model.ReadHudState()).ToString(),Is.EqualTo(frame.ToString()));
  model=LastRail.Battle.BasicCombatSimulation.RestoreLocalState(model.CaptureLocalState(),fresh);Assert.That(((JArray)Read(Make(model.ReadHudState()),model.ReadHudState())["absorptions"]).Count,Is.Zero);
  model.SetPaused(false);model.Advance(step,1);Assert.That((int)((JArray)model.ReadHudState()["actors"]).Single(a=>(bool)a["train"])["shield"],Is.Zero);
 }
 static JObject Hud(int tick,int shield,int absorbed=0,int hpLoss=0)=>new JObject{["stepTimeSpanTicks"]=500000,["motionSeconds"]=tick/20d,["outcome"]="Running",["actors"]=new JArray(new JObject{["id"]="train_a",["train"]=true,["hp"]=1000,["shield"]=shield}),["hits"]=absorbed+hpLoss==0?new JArray():new JArray(new JObject{["Tick"]=2,["Source"]="enemy",["Target"]="train_a",["Absorbed"]=absorbed,["HpLoss"]=hpLoss})};
 [Test] public void ActualHudSuppliesOriginalStepDuration(){Assert.That((long?)CombatPresentationConnectionTests.Fresh().ReadHudState()["stepTimeSpanTicks"],Is.EqualTo(500000));}
 [TestCase(200000)][TestCase(1000000)]
 public void AbsorptionTailUsesOwnerStepAndPausedClockInsteadOfAssuming50Milliseconds(long step){
  var initial=Hud(0,20);initial["stepTimeSpanTicks"]=step;var state=Make(initial);
  var impact=Hud(2,0,20);impact["stepTimeSpanTicks"]=step;impact["motionSeconds"]=step*2/10000000d;
  var frame=Read(state,impact);Assert.That((double)frame["absorptions"][0]["age"],Is.EqualTo(0).Within(0.000001));
  impact["paused"]=true;Assert.That(Read(state,impact).ToString(),Is.EqualTo(frame.ToString()));
  var later=(JObject)impact.DeepClone();later["paused"]=false;later["motionSeconds"]=(double)impact["motionSeconds"]+.33;
  Assert.That((bool)Read(state,later)["active"],Is.False);Assert.That((int)later["actors"][0]["shield"],Is.Zero);
  Assert.That(((JArray)Read(Make(later),later)["absorptions"]).Count,Is.Zero);
 }
 object Make(JObject hud){var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("LastRail.Battle.CombatElectromagneticState")).FirstOrDefault(t=>t!=null);Assert.IsNotNull(type);return Activator.CreateInstance(type,new object[]{hud});}
 static JObject Read(object state,JObject hud)=>(JObject)state.GetType().GetMethod("Read").Invoke(state,new object[]{hud});
 [Test] public void FieldFollowsLiveShieldAndOnlyActualAbsorptionPulses(){
  var state=Make(Hud(0,0));var a=Read(state,Hud(1,400));Assert.IsTrue((bool)a["active"]);Assert.AreEqual(0,(double)a["issueAge"]);a=Read(state,Hud(2,300,100));Assert.AreEqual(1,((JArray)a["absorptions"]).Count);Assert.AreEqual(a.ToString(),Read(state,Hud(2,300,100)).ToString());Assert.IsFalse((bool)Read(state,Hud(200,0,100))["active"]);
 }
 [Test] public void RestoreKeepsFaintFieldWithoutReplayingHistoryOrIssueFlash(){var hud=Hud(3,200,100);var a=Read(Make(hud),hud);Assert.IsTrue((bool)a["active"]);Assert.IsEmpty((JArray)a["absorptions"]);Assert.Greater((double)a["issueAge"],1);}
 [Test] public void ExhaustedShieldPreservesPartialPenetrationInsteadOfInventingInvulnerability(){var state=Make(Hud(0,20));var a=Read(state,Hud(2,0,20,60));Assert.AreEqual(60,(int)a["absorptions"][0]["hpLoss"]);Assert.AreEqual(0,(int)a["shield"]);Assert.IsTrue((bool)a["active"]);var none=Read(Make(Hud(0,0)),Hud(2,0,0,60));Assert.IsFalse((bool)none["active"]);}
 [Test] public void TerminalHidesFieldAndDoesNotMutateHud(){var hud=Hud(2,100,20);hud["outcome"]="Failure";hud["actors"][0]["hp"]=0;var before=hud.ToString();Assert.IsFalse((bool)Read(Make(hud),hud)["active"]);Assert.AreEqual(before,hud.ToString());}
}
