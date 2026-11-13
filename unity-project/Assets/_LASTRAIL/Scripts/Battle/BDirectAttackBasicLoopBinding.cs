using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Supplied single target per actor, frozen and hashed. This is not an AI target selector.
 public sealed class BDirectAttackBasicLoopBinding {
  public const string WaveVersion="unity.combat-b.wave-loop-binding-local.v1.20261004";
  public const string Version="unity.combat-b.basic-loop-binding-local.v1";
  readonly JObject original;readonly Dictionary<string,string> targets=new Dictionary<string,string>(StringComparer.Ordinal);
  readonly System.Collections.ObjectModel.ReadOnlyCollection<string> ordered;readonly string[] missing;
  public BDirectAttackBuffBinding Composition {get;}public string OriginalHash {get;}
  internal IEnumerable<string> OrderedSources=>ordered;
  internal string Target(string source)=>targets[source];
  public IReadOnlyList<string> UnsuppliedActors=>System.Array.AsReadOnly(missing);
  public JObject Capture()=>(JObject)original.DeepClone();
  public BDirectAttackBasicLoopBinding(BDirectAttackBuffBinding composition,JObject packet,string trustedHash){
   bool wave=composition?.Attack.IsWaveRuntime==true;
   Composition=composition??throw new ArgumentNullException(nameof(composition));
   BuffWire.Need(packet!=null&&BasicCombatSimulation.StableJson(packet).Length<=65536&&BDirectAttackInput.Digest(packet)==trustedHash,"b_basic_loop_binding_hash");
   BuffWire.Fields(packet,"version","provenance","compositionHash","targets");
   BuffWire.Need((string)packet["version"]==(wave?WaveVersion:Version)&&(string)packet["provenance"]=="BOUNDED_LOCAL_NOT_FULL_RUN"&&(string)packet["compositionHash"]==composition.OriginalHash,"b_basic_loop_binding_version");
   var input=composition.Attack;
   foreach(var token in BuffWire.Array(packet["targets"],wave?0:1,wave?43:16)){
    var row=BuffWire.Object(token);BuffWire.Fields(row,"sourceId","targetId");var source=BuffWire.Text(row["sourceId"]);var target=BuffWire.Text(row["targetId"]);
    BuffWire.Need(input.Entities.TryGetValue(source,out var actor)&&actor.Attack!=null&&input.Entities.ContainsKey(target)&&source!=target&&!targets.ContainsKey(source),"b_basic_loop_actor");
    var victim=input.Entities[target];
   BuffWire.Need(((JArray)actor.Original["allowedTargets"]).Any(t=>(string)t==victim.Kind)&&(wave||input.TryGetGeometry(source,target,out _)),"b_basic_loop_target_source");
    targets.Add(source,target);
   }
   // Original Step allocates sequence in explicit ally array then explicit enemy array order.
   var sources=((JArray)input.Capture()["entities"]).Select(e=>input.Entities[(string)e["id"]]).Where(e=>e.Attack!=null).OrderBy(e=>e.Kind=="LOCAL_ENEMY"?1:0).Select(e=>e.Id).ToArray();
   ordered=System.Array.AsReadOnly(sources.Where(targets.ContainsKey).ToArray());missing=sources.Where(s=>!targets.ContainsKey(s)).ToArray();
   original=(JObject)packet.DeepClone();OriginalHash=trustedHash;
  }
 }
}
