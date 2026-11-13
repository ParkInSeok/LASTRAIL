using System;
using LastRail.Run;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // New execution identity: historical Sera checkpoints retain their original reader and factory.
 public sealed class OrdinaryFiveCombatFactory {
  public const string Version="combat.ordinary-five-execution.v1.20261028";
  public const string CheckpointVersion="combat.ordinary-five-execution-state.v1.20261028";
  public OrdinaryFiveStartInput Original {get;}
  public string InitialPacketHash=>Stats?.PacketHash??throw new InvalidOperationException("Node factory has no initialization packet.");
  public string ConfigurationVersion {get;}
  public OrdinaryNodeAttempt Attempt {get;}
  internal OrdinaryFiveCoreStatSnapshot Stats {get;}
  internal OrdinaryTrainInitialState Train {get;}
  internal long Step {get;}
  internal CombatSeraInput HealthInput {get;}
  internal FirstPlayableScaling Encounter {get;}
  internal OrdinaryNodeCombatInput NodeInput {get;}
  internal OrdinaryNodeBasicSources BasicSources {get;}
  internal OrdinaryNodeGunnerSources GunnerSources {get;}
  internal OrdinaryReinaBarrageSources ReinaBarrageSources {get;}
  internal Action<long,long> SceneMotion {get;}
  internal string LiveDefinitionJson {get;}
  internal NyxZoneSources NyxSources {get;}
  internal OrdinaryLocalBoardingSource BoardingSource {get;}
  internal bool LocalInterceptorUninstalled {get;}
  internal OrdinaryAugmentOwnership AugmentOwnership {get;}
  internal System.Collections.Generic.IEnumerable<string> EnemyIds=>System.Linq.Enumerable.Concat(System.Linq.Enumerable.Select(NodeInput.Source.Enemies.Gunners,g=>g.InstanceId),BoardingSource==null?System.Array.Empty<string>():System.Linq.Enumerable.Select(System.Linq.Enumerable.SelectMany(BoardingSource.Boarding.OriginalSource.Waves,w=>w.Instances),i=>i.InstanceId));
  internal OrdinaryFiveCoreStatSnapshot.ActorStats ActorStats(string id)=>NodeInput==null?Stats.Actor(id):NodeInput.Actor(id);
  internal string TrainId=>NodeInput==null?Train.TrainId:NodeInput.TrainId;
  internal int TrainMaxHp=>NodeInput==null?Train.MaxHp:NodeInput.TrainMaxHp;
  // No current source provides the required measured muzzle, hit-volume and generation binding.
  internal IOrdinaryFiveNativeGeometry Geometry=>null;
  OrdinaryFiveCombatFactory(OrdinaryFiveStartInput original,string receipt,long step,FirstPlayableScaling encounter) {
   if(step<=0||step%10000!=0)throw new ArgumentException("Integer millisecond logical step required.");
   Original=original??throw new ArgumentNullException(nameof(original));
   Stats=OrdinaryInitializationReceipt.Validate(receipt,original)??throw new InvalidOperationException("Original initialization state required.");
   if(original.Basic?.SourceVersion!=(original.KitSourceVersion==OrdinaryFiveStartInput.KitV3Version?OrdinaryBasicSource.V5Version:OrdinaryBasicSource.V3Version)||original.Roof==null||original.Rail?.MotionBound!=true)
    throw new InvalidOperationException("Original basic timing, roof layout and initial motion required.");
   Train=new OrdinaryTrainInitialState((JObject)JObject.Parse(receipt)["initializationReceipt"],original.AccountId,original.RunId,original);
   encounter?.RequireOriginal(original,receipt);Encounter=encounter;
   Step=step;HealthInput=new CombatSeraInput(original,Stats,Train);
   ConfigurationVersion=Version+"/"+original.StartSnapshotHash+"/"+Stats.PacketHash+"/"+step+"/"+(encounter?.BindingHash??"pending-arrival");
  }
  public static OrdinaryFiveCombatFactory FromInitialization(OrdinaryFiveStartInput original,string receipt,long stepTimeSpanTicks)
   =>new OrdinaryFiveCombatFactory(original,receipt,stepTimeSpanTicks,null);
  public static OrdinaryFiveCombatFactory FromInitialization(OrdinaryFiveStartInput original,string receipt,long stepTimeSpanTicks,FirstPlayableScaling encounter)
   =>new OrdinaryFiveCombatFactory(original,receipt,stepTimeSpanTicks,encounter??throw new ArgumentNullException(nameof(encounter)));
  OrdinaryFiveCombatFactory(OrdinaryFiveStartInput original,OrdinaryNodeStart node,FirstPlayableScaling encounter,long step,OrdinaryNodeAttempt attempt,OrdinaryNodeBasicSources basicSources=null,OrdinaryNodeGunnerSources gunnerSources=null,OrdinaryReinaBarrageSources reinaBarrageSources=null,Action<long,long> sceneMotion=null,string liveDefinitionJson=null,NyxZoneSources nyxSources=null,OrdinaryLocalBoardingSource boardingSource=null,bool localInterceptorUninstalled=false,OrdinaryAugmentOwnership augmentOwnership=null) {
   Attempt=attempt??throw new ArgumentNullException(nameof(attempt));Attempt.RequireNode(node);
   NodeInput=new OrdinaryNodeCombatInput(original,node,encounter,step,attempt);Original=original;Encounter=encounter;Step=step;BasicSources=basicSources;GunnerSources=gunnerSources;ReinaBarrageSources=reinaBarrageSources;SceneMotion=sceneMotion;LiveDefinitionJson=liveDefinitionJson;NyxSources=nyxSources;BoardingSource=boardingSource;LocalInterceptorUninstalled=localInterceptorUninstalled;AugmentOwnership=augmentOwnership;
   if(original.Basic?.SourceVersion!=(original.KitSourceVersion==OrdinaryFiveStartInput.KitV3Version?OrdinaryBasicSource.V5Version:OrdinaryBasicSource.V3Version)||original.Roof==null||original.Rail?.MotionBound!=true)
    throw new ArgumentException("Original basic timing, roof layout and initial motion required.");
   ConfigurationVersion=Version+"/node/basic-hp-charge-v1.20261109/projectile-receipts-v2.20261109/"+original.StartSnapshotHash+"/"+node.NodeId+"/"+attempt.Generation+"/"+attempt.Revision+"/"+node.SnapshotHash+"/"+step+"/"+encounter.BindingHash+(boardingSource==null?"":"/boarding/"+boardingSource.SourceHash)+(localInterceptorUninstalled?"/explicit-local-interceptor-uninstalled.v1.20261109":"")+(augmentOwnership==null?"":"/augments/"+augmentOwnership.Hash);
  }
  public static OrdinaryFiveCombatFactory FromNodeStart(OrdinaryFiveStartInput original,OrdinaryNodeStart node,FirstPlayableScaling encounter,long stepTimeSpanTicks)
   =>new OrdinaryFiveCombatFactory(original,node,encounter,stepTimeSpanTicks,OrdinaryNodeAttempt.FromEntry(node));
  public static OrdinaryFiveCombatFactory FromNodeStart(OrdinaryFiveStartInput original,OrdinaryNodeStart node,FirstPlayableScaling encounter,long stepTimeSpanTicks,OrdinaryNodeAttempt attempt)
   =>new OrdinaryFiveCombatFactory(original,node,encounter,stepTimeSpanTicks,attempt);
  public BasicCombatSimulation Create()=>new BasicCombatSimulation(this);
  // The scene copy binds live callbacks once, after installing its isolated health/state.
  internal BasicCombatSimulation CreateForSceneCopy()=>new BasicCombatSimulation(this,false);
  internal OrdinaryFiveCombatFactory WithLiveSources(string definitionJson){
   if(NodeInput?.Source.Obstacle==null)throw new ArgumentException("Issued live obstacle node required.");
   OrdinaryLiveCombatSources.ReadDefinition(definitionJson,NodeInput.Source.GeometryBinding?.Definition.DefinitionHash);
   return new OrdinaryFiveCombatFactory(Original,NodeInput.Source,Encounter,Step,Attempt,BasicSources,GunnerSources,ReinaBarrageSources,SceneMotion,definitionJson,NyxSources,BoardingSource,LocalInterceptorUninstalled,AugmentOwnership);
  }
  // Only the owning native adapter supplies original observations and resolved effect inputs.
  // Installing BASIC sources does not grant full-combat readiness or server admission.
  internal OrdinaryFiveCombatFactory WithBasicSources(OrdinaryNodeBasicSources source){
   if(NodeInput==null||source==null)throw new ArgumentException("Original node BASIC sources required.");
   return new OrdinaryFiveCombatFactory(Original,NodeInput.Source,Encounter,Step,Attempt,source,GunnerSources,ReinaBarrageSources,SceneMotion,LiveDefinitionJson,NyxSources,BoardingSource,LocalInterceptorUninstalled,AugmentOwnership);
  }
  internal OrdinaryFiveCombatFactory WithGunnerSources(OrdinaryNodeGunnerSources source){
   if(NodeInput==null||source==null)throw new ArgumentException("Original node gunner sources required.");
   return new OrdinaryFiveCombatFactory(Original,NodeInput.Source,Encounter,Step,Attempt,BasicSources,source,ReinaBarrageSources,SceneMotion,LiveDefinitionJson,NyxSources,BoardingSource,LocalInterceptorUninstalled,AugmentOwnership);
  }
  internal OrdinaryFiveCombatFactory WithReinaBarrageSources(OrdinaryReinaBarrageSources source){
   bool relocation=Original.KitSourceVersion==OrdinaryFiveStartInput.KitV3Version?(Original.Relocation?.SourceVersion==OrdinaryRelocationSource.V9||Original.Relocation?.SourceVersion==OrdinaryRelocationSource.V10):Original.Relocation?.SourceVersion==OrdinaryRelocationSource.V7;
   if(NodeInput==null||source==null||(Original.KitSourceVersion!=OrdinaryFiveStartInput.KitV2Version&&Original.KitSourceVersion!=OrdinaryFiveStartInput.KitV3Version)||Original.ReinaBarrage==null||!relocation)throw new ArgumentException("Original new-definition Reina barrage sources required.");
   return new OrdinaryFiveCombatFactory(Original,NodeInput.Source,Encounter,Step,Attempt,BasicSources,GunnerSources,source,SceneMotion,LiveDefinitionJson,NyxSources,BoardingSource,LocalInterceptorUninstalled,AugmentOwnership);
  }
  internal OrdinaryFiveCombatFactory WithSceneMotion(Action<long,long> apply){
   if(NodeInput==null||apply==null)throw new ArgumentException("Original node scene motion required.");
   return new OrdinaryFiveCombatFactory(Original,NodeInput.Source,Encounter,Step,Attempt,BasicSources,GunnerSources,ReinaBarrageSources,apply,LiveDefinitionJson,NyxSources,BoardingSource,LocalInterceptorUninstalled,AugmentOwnership);
  }
  internal OrdinaryFiveCombatFactory WithNyxSources(NyxZoneSources source){
   if(NodeInput==null||LiveDefinitionJson==null||source==null||Original.KitSourceVersion!=OrdinaryFiveStartInput.KitV3Version||Original.Relocation?.SourceVersion!=OrdinaryRelocationSource.V10||source.PolicyHash!=Original.Relocation.ZoneSelectionHash||source.BindingId!="nyx-owner:"+BasicCombatSimulation.Digest(ConfigurationVersion))throw new ArgumentException("Original Nyx node geometry and source binding required.");
   return new OrdinaryFiveCombatFactory(Original,NodeInput.Source,Encounter,Step,Attempt,BasicSources,GunnerSources,ReinaBarrageSources,SceneMotion,LiveDefinitionJson,source,BoardingSource,LocalInterceptorUninstalled,AugmentOwnership);
  }
  public OrdinaryFiveCombatFactory WithBoardingSource(JObject envelope,string independentlyExpectedHash){
   if(NodeInput==null||LiveDefinitionJson==null||BoardingSource!=null)throw new ArgumentException("One original boarding binding on live node required.");
   var source=OrdinaryLocalBoardingSource.Read(envelope,independentlyExpectedHash,Original,NodeInput.Source);
   if(NodeInput.Source.BoardingSourceHash!=source.SourceHash)throw new ArgumentException("Boarding must be pinned in the original node snapshot.");
   if(!(JObject.Parse(LiveDefinitionJson)["source"]?["boardingGeometry"] is JObject))throw new ArgumentException("Actual authored boarding geometry required.");
   return new OrdinaryFiveCombatFactory(Original,NodeInput.Source,Encounter,Step,Attempt,BasicSources,GunnerSources,ReinaBarrageSources,SceneMotion,LiveDefinitionJson,NyxSources,source,LocalInterceptorUninstalled,AugmentOwnership);
  }
#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
  // Explicit QA equipment selection only; production missing equipment sources remain unresolved.
  internal OrdinaryFiveCombatFactory WithLocalUninstalledInterceptor(){
   if(NodeInput==null||LiveDefinitionJson==null)throw new InvalidOperationException("Live local node required.");
   return new OrdinaryFiveCombatFactory(Original,NodeInput.Source,Encounter,Step,Attempt,BasicSources,GunnerSources,ReinaBarrageSources,SceneMotion,LiveDefinitionJson,NyxSources,BoardingSource,true,AugmentOwnership);
  }
#endif
  internal OrdinaryFiveCombatFactory WithAugmentOwnership(JObject packet){
   if(NodeInput==null||LiveDefinitionJson==null||ReinaBarrageSources!=null)throw new ArgumentException("Owned live node required.");
   var ownership=new OrdinaryAugmentOwnership(packet,Original);
   return new OrdinaryFiveCombatFactory(Original,NodeInput.Source,Encounter,Step,Attempt,BasicSources,GunnerSources,ReinaBarrageSources,SceneMotion,LiveDefinitionJson,NyxSources,BoardingSource,LocalInterceptorUninstalled,ownership);
  }
  internal void Validate(BasicCombatSimulation model) {
   if(model==null||model.LocalOwnerAccount!=Original.AccountId||model.ConfigurationVersion!=ConfigurationVersion)
    throw new InvalidOperationException("Original ordinary execution factory differs.");
  }
 }
}
