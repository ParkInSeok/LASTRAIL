using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace LastRail.Presentation.MissionShell {
 // Adapter for EquipmentInventoryStore.readInventory, not a new HTTP contract.
 public sealed class EquipmentInventoryModel {
  public sealed class Item {
   public string Id {get;} public string DefinitionId {get;} public string DefinitionVersion {get;}
   public string Category {get;} public string SetTag {get;} public int Star {get;} public bool Locked {get;}
   public string Location {get;} public string WearerKind {get;} public string WearerId {get;} public int? Slot {get;}
   public long Revision {get;} internal string Signature {get;}
   public string Name {get;} public string IconKey {get;} public string Effects {get;}
   internal Item(JObject r){
    Id=Text(r,"instance_id");DefinitionId=Text(r,"definition_id");DefinitionVersion=Text(r,"definition_version");
    Category=Text(r,"category_id");SetTag=Text(r,"set_tag");Need(new[]{"SET01","SET02","SET03","SET04","SET05","SET06"}.Contains(SetTag));
    Star=(int)Number(r,"star",1,5);Locked=Number(r,"locked",0,1)==1;Revision=Number(r,"revision",1);Location=Text(r,"location_kind");
    if(Location=="STORED"){foreach(var key in new[]{"wearer_kind","wearer_id","slot_index"})Need(r[key]?.Type==JTokenType.Null);}
    else {Need(Location=="EQUIPPED");WearerKind=Text(r,"wearer_kind");Need(WearerKind=="operator"||WearerKind=="turret");WearerId=Text(r,"wearer_id");Slot=(int)Number(r,"slot_index",0,2);}
    if(r["presentation"] is JObject p){Name=Text(p,"name");Need(p["iconKey"]?.Type==JTokenType.Null||p["iconKey"]?.Type==JTokenType.String);IconKey=(string)p["iconKey"];Need(p["effects"] is JArray);var lines=(JArray)p["effects"];Need(lines.Count<=20&&lines.All(t=>t.Type==JTokenType.String&&((string)t).Length<=512));Effects=string.Join("\n",lines.Select(t=>(string)t));}
    Signature=new JArray(Id,Text(r,"source_item_id"),DefinitionId,DefinitionVersion,Category,SetTag,Star,Locked,Location,WearerKind,WearerId,Slot,Revision,Name,IconKey,Effects).ToString(Newtonsoft.Json.Formatting.None);
   }
  }
  public string AccountId {get;} public string RunId {get;} public long Revision {get;}
  public int Capacity {get;} public int Used {get;} public IReadOnlyList<Item> Items {get;}
  internal string Signature {get;}
  public static EquipmentInventoryModel Read(string account,string run,string json){var p=LastRail.Run.SafeTransitionWire.Parse(json);Need((string)p["version"]=="equipment.inventory-display.v1.20261111"&&(string)p["accountId"]==account&&(string)p["runId"]==run);Need(p["items"] is JArray&&((JArray)p["items"]).All(t=>t["presentation"] is JObject));return new EquipmentInventoryModel(account,run,p);}
  static void Need(bool valid){if(!valid)throw new ArgumentException("Invalid equipment inventory source");}
  static string Text(JObject r,string key){Need(r?[key]?.Type==JTokenType.String);string s=(string)r[key];Need(!string.IsNullOrWhiteSpace(s)&&s.Length<=256);return s;}
  static long Number(JObject r,string key,long min,long max=9007199254740991L){Need(r?[key]?.Type==JTokenType.Integer);long n=(long)r[key];Need(n>=min&&n<=max);return n;}
  public EquipmentInventoryModel(string accountId,string runId,JObject source){
   Need(!string.IsNullOrWhiteSpace(accountId)&&!string.IsNullOrWhiteSpace(runId));AccountId=accountId;RunId=runId;
   Revision=Number(source,"revision",1);Capacity=(int)Number(source,"capacitySlots",0,int.MaxValue);Used=(int)Number(source,"usedSlots",0,Capacity);
   Need(source["items"] is JArray);var items=((JArray)source["items"]).Select(t=>new Item(t as JObject??throw new ArgumentException("Equipment item must be object"))).ToArray();
   Need(items.Select(i=>i.Id).Distinct().Count()==items.Length&&items.Count(i=>i.Location=="STORED")==Used);
   var equipped=items.Where(i=>i.Location=="EQUIPPED").ToArray();Need(equipped.Select(i=>new JArray(i.WearerKind,i.WearerId,i.Slot).ToString()).Distinct().Count()==equipped.Length);
   Items=Array.AsReadOnly(items);Signature=new JArray(Capacity,Used,new JArray(items.OrderBy(i=>i.Id,StringComparer.Ordinal).Select(i=>i.Signature))).ToString(Newtonsoft.Json.Formatting.None);
  }
 }
}
