using System;
using System.Linq;
using System.Reflection;
using LastRail.Battle;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class OrdinaryLocalExecutionDeliveryTests {
 sealed class Fixture {
  internal readonly OrdinaryFiveCombatFactory Factory;
  internal readonly BasicCombatSimulation Model;
  internal readonly OrdinaryLocalExecutionDelivery Delivery;
  internal bool Current=true;
  internal Fixture(){
   var f=((OrdinaryFiveStartInput Start,BRouteCommand Command,JObject Response))typeof(OrdinaryNodeCombatFactoryTests).GetMethod("Fixture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
   var parsed=BRouteCommandResult.Read(200,f.Response.ToString(),f.Command);Factory=OrdinaryFiveCombatFactory.FromNodeStart(f.Start,parsed.NodeStart,parsed.Scaling,170000);Model=Factory.Create();
   Delivery=new OrdinaryLocalExecutionDelivery(Factory.Attempt,()=>Current);
  }
  internal JObject Checkpoint()=>JObject.Parse(Model.CaptureLocalState());
  internal void Pause(bool value){long before=Model.Tick;Model.SetPaused(value);Delivery.Queue(before,new JObject{["kind"]="PAUSE",["value"]=value});}
  internal JObject Result(OrdinaryLocalExecutionDelivery.Batch batch,JObject checkpoint)=>new JObject{["version"]=OrdinaryLocalExecutionReplay.ResultVersion,["accountId"]=Factory.Attempt.AccountId,["runId"]=Factory.Attempt.RunId,["nodeId"]=Factory.Attempt.NodeId,["attemptGeneration"]=Factory.Attempt.Generation,["snapshotHash"]=Factory.Attempt.SnapshotHash,["lastSequence"]=batch.LastSequence,["tick"]=batch.Tick,["checkpoint"]=checkpoint.DeepClone(),["obstacleResolution"]=null,["evidence"]=null,["healthChanges"]=new JArray()};
 }
 const string First="execution-batch-one",Second="execution-batch-two",Third="execution-batch-three";
 [TestCase(13053L,true)][TestCase(13054L,false)] public void JsonIntegralFloatRoundTripPreservesExactValue(long received,bool accepted){
  var f=new Fixture();var state=f.Checkpoint();state["obstacleExecution"]=new JObject{["events"]=new JArray(new JObject{["atMs"]=13053d})};
  var batch=f.Delivery.Prepare(First,state);var result=f.Result(batch,state);result["checkpoint"]["obstacleExecution"]["events"][0]["atMs"]=received;
  if(accepted)Assert.IsTrue(f.Delivery.Accept(First,result));else {Assert.Throws<ArgumentException>(()=>f.Delivery.Accept(First,result));Assert.AreSame(batch,f.Delivery.Pending);}
 }
 [Test] public void EmptyOpenFreezesNoHpUploadAndAcceptsOriginalCheckpoint(){
  var f=new Fixture();var checkpoint=f.Checkpoint();var batch=f.Delivery.Prepare(First,checkpoint);var body=JObject.Parse(batch.BodyJson);
  CollectionAssert.AreEquivalent(new[]{"runId","nodeId","attemptGeneration","snapshotHash","expectedSequence","inputs"},body.Properties().Select(p=>p.Name));
  Assert.AreEqual(0,((JArray)body["inputs"]).Count);Assert.AreEqual(0,batch.LastSequence);
  Assert.IsTrue(f.Delivery.Accept(First,f.Result(batch,checkpoint)));Assert.AreEqual(0,f.Delivery.AcceptedSequence);Assert.IsNull(f.Delivery.Pending);
 }
 [Test] public void PendingLossRetainsKeyBodyAndFrozenCheckpointWhileLaterCommandsQueue(){
  var f=new Fixture();f.Pause(true);var firstState=f.Checkpoint();var batch=f.Delivery.Prepare(First,firstState);string body=batch.BodyJson;
  f.Pause(false);Assert.AreSame(batch,f.Delivery.Prepare(First,f.Checkpoint()));Assert.AreEqual(body,f.Delivery.Pending.BodyJson);Assert.AreEqual(1,((JArray)JObject.Parse(body)["inputs"]).Count);
  Assert.Throws<ArgumentException>(()=>f.Delivery.Prepare(Second,f.Checkpoint()));Assert.AreSame(batch,f.Delivery.Pending);
  var result=f.Result(batch,firstState);Assert.IsTrue(f.Delivery.Accept(First,result));Assert.AreEqual(1,f.Delivery.QueuedCount);Assert.AreEqual(1,f.Delivery.AcceptedSequence);
  var second=f.Delivery.Prepare(Second,f.Checkpoint());Assert.AreEqual(1,second.ExpectedSequence);Assert.AreEqual(2,second.LastSequence);
  Assert.IsFalse(f.Delivery.Accept(First,result));Assert.AreSame(second,f.Delivery.Pending);
  Assert.IsTrue(f.Delivery.Accept(Second,f.Result(second,f.Checkpoint())));Assert.AreEqual(2,f.Delivery.AcceptedSequence);
  f.Pause(true);Assert.Throws<ArgumentException>(()=>f.Delivery.Prepare(First,f.Checkpoint()));Assert.IsNotNull(f.Delivery.Prepare(Third,f.Checkpoint()));
 }
 [TestCase("key")][TestCase("account")][TestCase("attempt")][TestCase("sequence")][TestCase("tick")][TestCase("checkpoint")][TestCase("obstacle")]
 public void MismatchedResultCannotRetirePendingRequest(string fault){
  var f=new Fixture();f.Pause(true);var state=f.Checkpoint();var batch=f.Delivery.Prepare(First,state);var result=f.Result(batch,state);
  if(fault=="account")result["accountId"]="other";if(fault=="attempt")result["attemptGeneration"]=2;if(fault=="sequence")result["lastSequence"]=0;if(fault=="tick")result["tick"]=1;
  if(fault=="checkpoint")result["checkpoint"]["motion"]["front"]=(long)result["checkpoint"]["motion"]["front"]+1;
  if(fault=="obstacle")result["obstacleResolution"]=new JObject{["outcome"]="DESTROYED"};
  Assert.Throws<ArgumentException>(()=>f.Delivery.Accept(fault=="key"?Second:First,result));Assert.AreSame(batch,f.Delivery.Pending);Assert.AreEqual(0,f.Delivery.AcceptedSequence);Assert.AreEqual(1,f.Delivery.QueuedCount);
  Assert.IsTrue(f.Delivery.Accept(First,f.Result(batch,state)));
 }
 [TestCase(false)][TestCase(true)] public void RetiredOrReplacedAttemptNeverAcceptsOldResponse(bool retire){
  var f=new Fixture();var state=f.Checkpoint();var batch=f.Delivery.Prepare(First,state);if(retire)f.Delivery.Retire();else f.Current=false;
  Assert.Throws<InvalidOperationException>(()=>f.Delivery.Accept(First,f.Result(batch,state)));Assert.AreSame(batch,f.Delivery.Pending);
  Assert.Throws<InvalidOperationException>(()=>f.Delivery.Queue(0,new JObject{["kind"]="PAUSE",["value"]=true}));
 }
 [Test] public void QueueCopiesActualOperationAndRejectsOldTickOrInjectedHp(){
  var f=new Fixture();var command=new JObject{["kind"]="SPEED",["value"]=3};f.Model.SetSpeed(3);f.Delivery.Queue(f.Model.Tick,command);command["value"]=4;
  var batch=f.Delivery.Prepare(First,f.Checkpoint());Assert.AreEqual(3,(int)JObject.Parse(batch.BodyJson)["inputs"][0]["command"]["value"]);
  Assert.Throws<ArgumentException>(()=>f.Delivery.Queue(-1,new JObject{["kind"]="PAUSE",["value"]=true}));
  Assert.Throws<ArgumentException>(()=>f.Delivery.Queue(0,new JObject{["kind"]="PAUSE",["value"]=true,["hp"]=0}));Assert.AreEqual(1,f.Delivery.QueuedCount);
 }
 [Test] public void PostCheckpointMustBelongToSameAttemptAndNotPrecedeObservedTick(){
  var f=new Fixture();var checkpoint=f.Checkpoint();checkpoint["health"]["definition"]["generation"]=2;
  Assert.Throws<ArgumentException>(()=>f.Delivery.Prepare(First,checkpoint));Assert.IsNull(f.Delivery.Pending);
  f.Delivery.Queue(1,new JObject{["kind"]="PAUSE",["value"]=true});Assert.Throws<ArgumentException>(()=>f.Delivery.Prepare(First,f.Checkpoint()));
 }
 [TestCase("ADVANCE",10000001L,1)][TestCase("ADVANCE",1L,1001)][TestCase("SPEED",5L,0)][TestCase("THROTTLE",6999L,0)]
 public void OutOfContractCommandLimitsCannotEnterPendingBatch(string kind,long value,int budget){
  var f=new Fixture();var command=kind=="ADVANCE"?new JObject{["kind"]=kind,["elapsed"]=value,["budget"]=budget}:new JObject{["kind"]=kind,["value"]=value};
  Assert.Throws<ArgumentException>(()=>f.Delivery.Queue(0,command));Assert.AreEqual(0,f.Delivery.QueuedCount);
 }
}
