using System;
using LastRail.Run;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // Only the server-owned START/entry/registry can supply this envelope; commands carry no result or HP.
 public static class OrdinaryLocalExecutionReplay {
  public const string InputVersion="ordinary.local-execution-input.v1.20261106";
  public const string BoardingInputVersion="ordinary.local-execution-input.v2.20261107";
  public const string OwnershipInputVersion="ordinary.local-execution-input.v3.20261109";
  public const string ResultVersion="ordinary.local-execution-result.v1.20261106";
  public static JObject Execute(JObject input){
   var source=ReadInput(input);long step=source.Node.ReadActions(source.Original).StepTimeSpanTicks;
   var factory=OrdinaryFiveCombatFactory.FromNodeStart(source.Original,source.Node,source.Scaling,step,source.Attempt).WithLiveSources(source.GeometryJson);
   if(source.BoardingSource!=null)factory=factory.WithBoardingSource(source.BoardingSource,source.Node.BoardingSourceHash);
   if(source.AugmentOwnership!=null)factory=factory.WithAugmentOwnership(source.AugmentOwnership);
   var model=factory.Create();long sequence=ReplayCommands(model,source.Commands);var checkpoint=JObject.Parse(model.CaptureLocalState());
   return new JObject{["version"]=ResultVersion,["accountId"]=source.Attempt.AccountId,["runId"]=source.Attempt.RunId,["nodeId"]=source.Attempt.NodeId,["attemptGeneration"]=source.Attempt.Generation,["snapshotHash"]=source.Attempt.SnapshotHash,
    ["lastSequence"]=sequence,["tick"]=model.Tick,["checkpoint"]=checkpoint,["obstacleResolution"]=checkpoint["obstacleSequence"]?["resolution"]?.DeepClone(),["evidence"]=null,["healthChanges"]=model.CaptureOrdinaryHealthChanges()};
  }
  internal sealed class Input {
   internal readonly OrdinaryFiveStartInput Original;
   internal readonly OrdinaryNodeStart Node;
   internal readonly FirstPlayableScaling Scaling;
   internal readonly OrdinaryNodeAttempt Attempt;
   internal readonly string GeometryJson;
   internal readonly JArray Commands;
   internal readonly JObject BoardingSource,AugmentOwnership;
   internal Input(OrdinaryFiveStartInput original,OrdinaryNodeStart node,FirstPlayableScaling scaling,OrdinaryNodeAttempt attempt,string geometry,JArray commands,JObject boardingSource=null,JObject augmentOwnership=null){
    Original=original;Node=node;Scaling=scaling;Attempt=attempt;GeometryJson=geometry;Commands=(JArray)commands.DeepClone();BoardingSource=(JObject)boardingSource?.DeepClone();AugmentOwnership=(JObject)augmentOwnership?.DeepClone();
   }
  }
  internal static Input ReadInput(JObject input){
   bool ownership=(string)input?["version"]==OwnershipInputVersion;
   bool boarding=(string)input?["version"]==BoardingInputVersion||ownership&&input["boardingSource"]!=null;
   var keys=new System.Collections.Generic.List<string>(new[]{"version","accountId","runId","originalStart","initializationResponse","nodeStart","firstPlayableScaling","currentAttempt","geometryDefinition","inputs"});if(boarding)keys.Add("boardingSource");if(ownership)keys.Add("augmentOwnership");BDirectAttackInput.Fields(input,keys.ToArray());
   Need((ownership||boarding||(string)input["version"]==InputVersion)&&input["initializationResponse"] is JObject&&input["inputs"] is JArray&&input["geometryDefinition"]?.Type==JTokenType.String);
   string account=BDirectAttackInput.Text(input,"accountId",200),run=BDirectAttackInput.Text(input,"runId",200);
   var original=OrdinaryFiveStartInput.Read(input["originalStart"] as JObject,account,run);
   var scaling=FirstPlayableScaling.ReadTrusted(input["firstPlayableScaling"] as JObject,original,input["initializationResponse"].ToString(),input["nodeStart"] as JObject);
   var node=OrdinaryNodeStart.ReadTrusted(input["nodeStart"] as JObject,original,scaling);
   var attempt=OrdinaryNodeAttempt.FromCurrent(node,new OrdinaryCurrentAttempt(input["currentAttempt"] as JObject));
   string geometry=(string)input["geometryDefinition"];var reference=node.GeometryBinding?.Definition;
   Need(reference!=null&&geometry.Length<=524288&&BasicCombatSimulation.Digest(geometry)==reference.DefinitionHash);
   var definition=JObject.Parse(geometry);Need((string)definition["version"]==OrdinaryGeometryReference.Version&&(string)definition["definitionId"]==reference.DefinitionId);
   Need(boarding==(node.BoardingSourceHash!=null));
   if(boarding)OrdinaryLocalBoardingSource.Read(input["boardingSource"] as JObject,node.BoardingSourceHash,original,node);
   if(ownership)_=new OrdinaryAugmentOwnership(input["augmentOwnership"] as JObject,original);
   return new Input(original,node,scaling,attempt,geometry,(JArray)input["inputs"],input["boardingSource"] as JObject,input["augmentOwnership"] as JObject);
  }
  internal static long ReplayCommands(BasicCombatSimulation model,JArray inputs){
   if(model==null||inputs==null)throw new ArgumentNullException();long sequence=0;
   foreach(var token in inputs){
    var row=token as JObject;BDirectAttackInput.Fields(row,"sequence","tick","command");
    Need(BDirectAttackInput.Nat(row,"sequence",1,OrdinaryNodeStart.SafeMaximum)==sequence+1&&BDirectAttackInput.Nat(row,"tick",0,OrdinaryNodeStart.SafeMaximum)==model.Tick);
    var command=row["command"] as JObject;string kind=BDirectAttackInput.Text(command,"kind",40);
    if(kind=="ADVANCE"){
     BDirectAttackInput.Fields(command,"kind","elapsed","budget");
     model.Advance(BDirectAttackInput.Nat(command,"elapsed",0,OrdinaryNodeStart.SafeMaximum),(int)BDirectAttackInput.Nat(command,"budget",0,int.MaxValue));
    }else{
     BDirectAttackInput.Fields(command,"kind","value");
     if(kind=="PAUSE"||kind=="ULTIMATE_AUTO"){
      Need(command["value"]?.Type==JTokenType.Boolean);
      if(kind=="PAUSE")model.SetPaused((bool)command["value"]);else model.SetUltimateAuto((bool)command["value"]);
     }else if(kind=="SPEED"||kind=="THROTTLE"){
      int value=(int)BDirectAttackInput.Nat(command,"value",0,int.MaxValue);
      if(kind=="SPEED")model.SetSpeed(value);else model.SetRailThrottle(value);
     }else throw new ArgumentException("ordinary_local_execution_command_unsupported");
    }
    sequence++;
   }
   return sequence;
  }
  static void Need(bool valid){if(!valid)throw new ArgumentException("ordinary_local_execution_input_conflict");}
 }
}
