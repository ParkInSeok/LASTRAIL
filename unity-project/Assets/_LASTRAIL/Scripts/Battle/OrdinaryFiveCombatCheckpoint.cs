using System;
using LastRail.Diagnostics;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using LastRail.Run;
using System.Threading.Tasks;
namespace LastRail.Battle {
 // Bridges the product account journal to the existing encrypted/CAS store, without changing admission.
 public sealed class OrdinaryFiveCombatCheckpoint {
  readonly CombatCheckpointStore store;readonly JObject identity;
  string initializationKey;Task<CombatSceneDriver> opening;Func<bool> openingOwner;
  public OrdinaryFiveCombatFactory Factory{get;}
  public const string Version="combat.ordinary-five-execution-store-binding.v1.20261028";
  public OrdinaryFiveCombatCheckpoint(CombatCheckpointStore store,PendingAccountAction original,OrdinaryFiveCombatFactory factory,AppBuildSnapshot build){
   this.store=store??throw new ArgumentNullException(nameof(store));Factory=factory??throw new ArgumentNullException(nameof(factory));
   if(factory.NodeInput!=null)throw new InvalidOperationException("Node entry cannot use an initialization checkpoint; restart from its node owner.");
   if(original==null||original.Kind!="departure-start"||original.StartResultJson==null||original.Account!=factory.Original.AccountId||build?.AppVersion==null)throw new ArgumentException("Retained product START and installed build required.");
   var receipt=new DepartureStartResult(200,original.StartResultJson,original.Target);var input=factory.Original;
   // Reuse validated immutable definitions only after matching the entire retained START.
   if(receipt.RunStatus!="ACTIVE"||receipt.RunId!=input.RunId||BDirectAttackInput.Digest(receipt.StartResponse).Substring(7)!=input.StartSnapshotHash)throw new ArgumentException("Product START and factory differ; preserve checkpoint for recovery.");
   identity=new JObject{["version"]=Version,["account"]=original.Account,["run"]=input.RunId,["preview"]=original.Target,["key"]=original.Key,["body"]=original.Payload,
    ["response"]=original.StartResultJson,["appVersion"]=build.AppVersion,["appVersionCode"]=build.AppVersionCode.HasValue?new JValue(build.AppVersionCode.Value):JValue.CreateNull(),
    ["factory"]=factory.ConfigurationVersion,["originHash"]=input.OriginHash,["kitHash"]=input.KitHash};
  }
  public long Save(BasicCombatSimulation state,long expectedRevision){Factory.Validate(state);return store.SaveIdentity(identity,Factory.Original.AccountId,state,expectedRevision);}
  public OrdinaryFiveCombatCheckpoint(CombatCheckpointStore store,PendingAccountAction original,OrdinaryFiveCombatFactory factory,AppBuildSnapshot build,PendingRunOperation initialization):this(store,original,factory,build){
   if(initialization?.Kind!=PendingRunKind.InitializeOrdinary||initialization.AccountId!=original.Account||initialization.TargetId!=factory.Original.RunId||initialization.AcceptedResponseJson==null)throw new ArgumentException("Confirmed original initialization required.");
   OrdinaryInitializationReceipt.Validate(initialization.AcceptedResponseJson,factory.Original);
   if((string)JObject.Parse(initialization.AcceptedResponseJson)["initializationReceipt"]["stats"]["packetHash"]!=factory.InitialPacketHash)throw new ArgumentException("Initial factory stats differ from receipt.");
   initializationKey=initialization.IdempotencyKey;identity["version"]="combat.ordinary-five-execution-store-binding.v2.20261028";
   identity["initializationKey"]=initializationKey;identity["initializationReceiptHash"]=BDirectAttackInput.Digest(JObject.Parse(initialization.AcceptedResponseJson));
  }
  // Opening commits the prepared original roster; encounter readiness remains owned by the execution model.
  public Task<CombatSceneDriver> OpenAfterFirstCommitAsync(DurableRunClient client,Func<bool> ownerCurrent){
   if(initializationKey==null||client==null||ownerCurrent==null)throw new InvalidOperationException("Initialization-bound checkpoint required.");
   if(!ownerCurrent()||openingOwner!=null&&!openingOwner())throw new InvalidOperationException("Combat owner changed.");
   if(opening!=null&&!opening.IsFaulted&&!opening.IsCanceled)return opening;
   openingOwner=ownerCurrent;
   return opening=OpenCommitted(client,ownerCurrent);
  }
  async Task<CombatSceneDriver> OpenCommitted(DurableRunClient client,Func<bool> ownerCurrent){
   if(!ownerCurrent())throw new InvalidOperationException("Combat owner changed.");
   var committed=await client.CommitFirstOrdinaryCheckpointAsync(Factory.Original,initializationKey,receipt=>{
    if(!ownerCurrent())throw new InvalidOperationException("Combat owner changed.");
    ValidateCanonicalInitialization(receipt);
    // Validated durable progress outranks an unfinished initialization marker.
    var existing=Restore();if(existing!=null)return Task.FromResult(existing.Revision);
    return Task.FromResult(Save(Factory.Create(),0));
   });
   if(committed==null||!ownerCurrent())throw new InvalidOperationException("First checkpoint is not confirmed.");
   ValidateCanonicalInitialization(committed);
   var saved=Restore();if(saved==null||saved.Revision<committed.OrdinaryCheckpointRevision.Value)throw new InvalidOperationException("Committed checkpoint is missing or stale; initial HP cannot replace it.");
   var driver=CombatSceneDriver.ForOrdinaryCombatStart(this,ownerCurrent);driver.Open();return driver;
  }
  void ValidateCanonicalInitialization(PendingRunOperation receipt){
   if(receipt?.AcceptedResponseJson==null||receipt.IdempotencyKey!=initializationKey||BDirectAttackInput.Digest(JObject.Parse(receipt.AcceptedResponseJson))!=(string)identity["initializationReceiptHash"])throw new InvalidOperationException("Canonical initialization receipt differs from prepared checkpoint.");
  }
  public RestoredCombatCheckpoint Restore(){var restored=store.RestoreIdentity(identity,Factory.Original.AccountId,Factory.Create);if(restored!=null)Factory.Validate(restored.Simulation);return restored;}
 }
}
