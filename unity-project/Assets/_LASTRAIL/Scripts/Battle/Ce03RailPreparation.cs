using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
// Opt-in loopback preparation, never admission or legacy finish authority.
public sealed class Ce03RailPreparation {
 readonly JObject wrapper,prepared,rail;readonly Ce03CommandContract commands;
 public Ce03RailPresentation Presentation{get;}
 public string Owner{get;}public string PreparationSha256=>(string)wrapper["preparationSha256"];
 public string RailSha256=>(string)wrapper["railJourneySha256"];
 public JObject Copy()=>(JObject)wrapper.DeepClone();
 public JObject RailConfiguration=>(JObject)rail.DeepClone();
 public Ce03RailPreparation(JObject input,string owner,long supportRevision){
  try{
   if(input==null||(string)input["version"]!="ce03-rail-preparation-v1"||(bool?)input["ready"]!=false||(string)input["domainProjection"]!="LEGACY_COMBAT_GUN_ONLY")throw new ArgumentException("Rail wrapper required");
   var fields=new[]{"version","ready","basePreparation","railJourney","railJourneySha256","domainProjection","selectedCarriageOwnership","preparationSha256"};Ce03CommandContract.Fields(input,input.Property("railPresentation")==null?fields:fields.Concat(new[]{"railPresentation"}).ToArray());
   wrapper=(JObject)input.DeepClone();var payload=(JObject)wrapper.DeepClone();payload.Remove("preparationSha256");if(Ce03CommandContract.Sha(Ce03CommandContract.Canonical(payload))!=(string)wrapper["preparationSha256"])throw new ArgumentException("Rail wrapper digest mismatch");
   if(wrapper.Property("railPresentation")!=null)Presentation=new Ce03RailPresentation(wrapper["railPresentation"] as JObject);
   prepared=wrapper["basePreparation"] as JObject??throw new ArgumentException();rail=wrapper["railJourney"] as JObject??throw new ArgumentException();
   if(Ce03CommandContract.Sha(Ce03CommandContract.Canonical(rail))!=RailSha256)throw new ArgumentException("Rail config digest mismatch");
   commands=new Ce03CommandContract(prepared["commandContract"] as JObject);commands.ValidatePrepared(prepared,owner,supportRevision);Owner=owner;
   if((string)prepared["commandContract"]["version"]!="ce03-command-contract-v3"||(string)rail["catalogVersion"]!="rail-augment-catalog-v1"||(int?)rail["totalWaves"]!=15||rail["timeoutMs"]?.Type!=JTokenType.Null)throw new ArgumentException("Unsupported rail profile");
   _=new CombatRailJourney(rail);_=new CombatRailArtillery(rail["artillery"] as JObject);
   var cars=rail["carriages"] as JArray;var ownership=wrapper["selectedCarriageOwnership"]?["carriages"] as JArray;if(cars==null||cars.Count<2||cars.Count>4||ownership==null||cars.Count!=ownership.Count)throw new ArgumentException("Selected carriages required");
   for(int i=0;i<cars.Count;i++){var car=cars[i];var owned=ownership[i];if((int?)car["slot"]!=i||(int?)owned["slot"]!=i||(string)car["id"]!=(string)owned["carriageId"]||(string)car["type"]!=(string)owned["type"]||!new[]{"Combat","Gun","Cargo","Defense","Medical"}.Contains((string)car["type"]))throw new ArgumentException("Carriage identity/order mismatch");}
   if(cars.Select(c=>(string)c["id"]).Distinct().Count()!=cars.Count)throw new ArgumentException("Duplicate carriages");
   var template=prepared["draft"]["template"];var augments=(JArray)template["package"]["augments"];var stats=new[]{"SQUAD_DAMAGE","TRAIN_DAMAGE","SHIELD_MAGNITUDE"};var filters=new[]{"OPERATOR_DAMAGE","LOCOMOTIVE_GUN_DAMAGE_INCLUDING_SUPPORT_EXCLUDING_COMBAT","SHIELD_ISSUED"};
   for(int i=0;i<stats.Length;i++){var found=augments.SingleOrDefault(a=>(string)a["eventFilter"]==filters[i]);if(found==null||(int?)rail["baseBonusesBps"][stats[i]]!=(int?)found["valueBps"]||(int?)rail["capsBps"][stats[i]]!=(int?)template["rules"][i==2?"magnitudeBonusCapBps":"outgoingBuffCapBps"])throw new ArgumentException("Base bonus/cap mismatch");}
   foreach(var offer in (JArray)rail["offers"])foreach(var choice in (JArray)offer["options"]){var id=(string)choice["augmentId"];var expected=id=="rail_aug_squad_5"?stats[0]:id=="rail_aug_train_5"?stats[1]:id=="rail_aug_shield_5"?stats[2]:null;if(expected==null||(string)choice["effect"]["stat"]!=expected||(int?)choice["effect"]["valueBps"]!=500)throw new ArgumentException("Unapproved augment effect");}
  }catch(Exception e)when(!(e is ArgumentException)){throw new ArgumentException("Invalid rail preparation",e);}
 }
 // The constructor already authenticated the complete private immutable wrapper.
 // Retain the model's source/ownership validation without rehashing the large presentation manifest every frame.
 public BasicCombatSimulation CreateModel(uint seed){var model=BasicCombatSimulation.FromPreparedProfileForLocalTest(prepared,(JObject)prepared["ownedSelection"],Owner,seed);model.ConfigureRailJourney(rail);return model;}
}
}
