using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace LastRail.Battle {
 // Frozen server publications. Their profile alternatives do not select a run's risk/progress.
 public sealed class FirstBattleSource {
  static readonly IReadOnlyDictionary<string,string> Shapes=new Dictionary<string,string>{
   {"combat.first-battle-encounter-source.v1.20261026","449dae32a3f3a92e8b315e4c0a4fc852d51f4f2512a54f0635e1f697a4477de0"},
   {"combat.first-battle-encounter-source.v2.20261026","918af6b5cc624cbed476692b37478d055d6e5da43e5a505322412ab2c735d3a5"},
   {"combat.first-battle-encounter-source.v3.20261027","be904478f15de551e705d9e9b71a108868b37930847f1066a7bb8f2ba65d0615"},
   {"combat.first-battle-encounter-source.v4.20261027","10257e139e6ba111f1cd160bf1441df73d65c44f0ad37c5a0c97cbe133ca1df0"},
   {"combat.first-battle-encounter-source.v5.20261027","799ce48059c445b282ea342444201287ed0da0df47410b40ab37b037554a363e"},
   {"combat.first-battle-encounter-source.v6.20261028","4eeb37d9c2f005e2e75298d4b89b6bf4b89d82a4dbb81a194234b869e80d9acb"},
   {"combat.first-battle-encounter-source.v7.20261028","ef33bc5cd576988d71bcd2c78f73e3706708a24f647864a89625f8db0983c802"},
   {"combat.first-battle-encounter-source.v8.20261028","81b2615bba640a9be0e61356a938fac412140e904f25921da997c99d16abdf79"}};
  readonly JObject definition;
  public string SourceVersion{get;}
  public string SourceHash{get;}
  public string Status{get;}
  public string ProfileHash=>(string)definition?["definitionHash"];
  public JObject Definition=>definition==null?null:(JObject)definition.DeepClone();
  public bool HasAppliedScaling{get;}
  public string SelectedRisk{get;}
  internal FirstBattleSource(JToken raw,OrdinaryFiveStartInput original,string startRisk){
   var source=raw as JObject;BDirectAttackInput.Fields(source,"packet","sourceHash");
   var packet=source["packet"] as JObject;Need(packet!=null&&Encoding.UTF8.GetByteCount(BasicCombatSimulation.StableJson(packet))<=131072);
   SourceVersion=(string)packet["version"];SourceHash=(string)source["sourceHash"];
   Need(SourceVersion!=null&&Shapes.ContainsKey(SourceVersion)&&SourceHash==Hash(packet));
   Need((string)packet["accountId"]==original.AccountId&&(string)packet["runId"]==original.RunId&&(string)packet["originHash"]==original.OriginHash
    &&packet["formationRevision"]?.Type==JTokenType.Integer&&(long)packet["formationRevision"]==original.FormationRevision);
   var body=(JObject)packet.DeepClone();foreach(var key in new[]{"accountId","runId","originHash","formationRevision"})body.Remove(key);
   bool referenced=SourceVersion=="combat.first-battle-encounter-source.v8.20261028";
   if(referenced||SourceVersion=="combat.first-battle-encounter-source.v4.20261027"||SourceVersion=="combat.first-battle-encounter-source.v5.20261027"||SourceVersion=="combat.first-battle-encounter-source.v6.20261028"||SourceVersion=="combat.first-battle-encounter-source.v7.20261028"){
    SelectedRisk=(string)packet["selectedRisk"];Need(new[]{"I","II","III","IV","V"}.Contains(SelectedRisk)&&SelectedRisk==startRisk);body.Remove("selectedRisk");
   }
   Need(Hash(body)==Shapes[SourceVersion]);
   var resolved=referenced?FirstPlayableDefinitions.ResolveFirstBattleStatic(packet["definitionRef"] as JObject):packet;
   definition=resolved["definition"] as JObject;definition=definition==null?null:(JObject)definition.DeepClone();
   if(referenced||SourceVersion=="combat.first-battle-encounter-source.v7.20261028")FirstPlayableDefinitions.RequireProfile(definition);
   Status=(string)resolved["status"];HasAppliedScaling=resolved["appliedScaling"] is JObject;
  }
  static string Hash(JToken value)=>BDirectAttackInput.Digest(value).Substring(7);
  static void Need(bool valid){if(!valid)throw new ArgumentException("first_battle_source_invalid");}
 }
}
