using System;
namespace LastRail.Battle {
 // First native ability slice over all five original actors. No basic/ultimate/operation admission capability.
 public sealed class OrdinaryFiveSeraFactory {
  public const string Version="combat.ordinary-five-sera-factory.v1.20261026";
  public const string CheckpointVersion="combat.ordinary-five-sera-owner.v1.20261026";
  readonly CombatSeraInput input;readonly long step;
  public OrdinaryFiveStartInput Original{get;}
  public string InitialPacketHash{get;}
  public string ConfigurationVersion=>Version+"/"+input.Hash+"/"+step;
  public static OrdinaryFiveSeraFactory FromInitialization(OrdinaryFiveStartInput original,string receipt,long stepTimeSpanTicks){
   var stats=LastRail.Run.OrdinaryInitializationReceipt.Validate(receipt,original)??throw new InvalidOperationException("Historical initialization lacks its initial source state; recovery is required.");
   var train=original.Train?.SourceVersion==OrdinaryTrainSource.InitialHealthVersion?new OrdinaryTrainInitialState((Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.Linq.JObject.Parse(receipt)["initializationReceipt"],original.AccountId,original.RunId,original):null;
   return new OrdinaryFiveSeraFactory(original,stats,stepTimeSpanTicks,train);
  }
  public OrdinaryFiveSeraFactory(OrdinaryFiveStartInput original,OrdinaryFiveCoreStatSnapshot initialStats,long stepTimeSpanTicks):this(original,initialStats,stepTimeSpanTicks,null){}
  OrdinaryFiveSeraFactory(OrdinaryFiveStartInput original,OrdinaryFiveCoreStatSnapshot initialStats,long stepTimeSpanTicks,OrdinaryTrainInitialState train){
   if(stepTimeSpanTicks<=0||stepTimeSpanTicks%10000!=0)throw new ArgumentException("Explicit integer-ms logical step required.");
   Original=original??throw new ArgumentNullException(nameof(original));
   if(original.Train?.SourceVersion==OrdinaryTrainSource.InitialHealthVersion&&train==null)throw new InvalidOperationException("Original train initialization required.");
   input=new CombatSeraInput(original,initialStats,train);InitialPacketHash=initialStats.PacketHash;step=stepTimeSpanTicks;
  }
  public BasicCombatSimulation Create()=>BasicCombatSimulation.CreateOrdinarySera(input,step);
  internal void Validate(BasicCombatSimulation model){if(model==null||model.LocalOwnerAccount!=Original.AccountId||model.ConfigurationVersion!=ConfigurationVersion)throw new InvalidOperationException("Original ordinary factory differs.");}
 }
}
