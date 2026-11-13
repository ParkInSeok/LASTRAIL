using System;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
// Disposable opt-in session identity. It carries no START or settlement authority.
public sealed class Ce03RailSession {
 readonly Ce03RailPreparation preparation;readonly JObject identity;readonly uint seed;
 string configuration;
 public string Owner=>preparation.Owner;
 public string SessionId=>(string)identity["sessionId"];
 public Ce03RailSession(Ce03RailPreparation preparation,string sessionId,uint seed){
  this.preparation=preparation??throw new ArgumentNullException(nameof(preparation));
  if(string.IsNullOrWhiteSpace(sessionId)||sessionId.Length>128)throw new ArgumentException("Local session ID required.");
  this.seed=seed;
  identity=new JObject{["version"]="ce03-rail-session-v1",["owner"]=Owner,["sessionId"]=sessionId,["preparationSha256"]=preparation.PreparationSha256,["railJourneySha256"]=preparation.RailSha256,["seed"]=seed};
 }
 internal JObject Copy()=>(JObject)identity.DeepClone();
 public BasicCombatSimulation CreateModel()=>preparation.CreateModel(seed);
 internal void ValidateModel(BasicCombatSimulation model){
  if(configuration==null)configuration=(string)JObject.Parse(CreateModel().CaptureLocalState())["configurationSha256"];
  if(model==null||model.LocalOwnerAccount!=Owner||(string)JObject.Parse(model.CaptureLocalState())["configurationSha256"]!=configuration)throw new InvalidOperationException("Rail session model identity changed.");
 }
 public CombatSceneDriver CreateDriver(CombatCheckpointStore store,Func<bool> ownerCurrent){
  if(store==null)throw new ArgumentNullException(nameof(store));
  return new CombatSceneDriver(CreateModel,()=>store.RestoreRailSession(this),(s,r)=>store.SaveRailSession(this,s,r),ownerCurrent);
 }
}
}
