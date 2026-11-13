using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // The retained origin generation stays one; the restart wrapper owns the current attempt.
 public sealed class OrdinaryNodeGeometryBinding {
  public OrdinaryGeometryReference Definition {get;}
  public long OriginGeneration {get;}
  internal OrdinaryNodeGeometryBinding(JToken token,JObject node){
   var value=token as JObject;BDirectAttackInput.Fields(value,"version","accountId","runId","nodeId","entryMapRevision","originGeneration","definitionRef");
   OrdinaryNodeStart.Need((string)value["version"]=="ordinary.node-geometry-binding.v1.20261031");
   foreach(var field in new[]{"accountId","runId","nodeId","entryMapRevision"})OrdinaryNodeStart.Need(JToken.DeepEquals(value[field],node[field]));
   OriginGeneration=OrdinaryNodeStart.Revision(value,"originGeneration");OrdinaryNodeStart.Need(OriginGeneration==1);
   Definition=new OrdinaryGeometryReference(value["definitionRef"]);
  }
 }
}
