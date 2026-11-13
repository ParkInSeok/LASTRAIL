using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Presentation.Battle {
 // Immutable visual clips. No direction cycling, interpolation, combat or filesystem authority.
 public sealed class FourAgentV03Catalog {
  public const string Version="four-agent.visual.v03.20261019";
  readonly JObject data;
  FourAgentV03Catalog(JObject normalized){data=(JObject)normalized.DeepClone();}
  public JObject Capture()=>(JObject)data.DeepClone();
  public string[] TexturePaths=>data["clips"].SelectMany(c=>c["frames"]).Select(f=>(string)f["path"]).Distinct(StringComparer.Ordinal).ToArray();
  public static FourAgentV03Catalog Restore(JObject normalized){if(normalized==null||(string)normalized["version"]!=Version||normalized["clips"] is not JArray rows||rows.Count!=32)throw new ArgumentException("Complete v03 catalog required.");return new FourAgentV03Catalog(normalized);}
  public static FourAgentV03Catalog Read(JObject source,JObject crops,string cropRoot,Func<string,Vector2> size){
   if(source==null||(string)source["version"]!=Version||source["clips"] is not JArray originals||originals.Count!=28||crops?["directions"] is not JArray directions||directions.Count!=4||size==null||string.IsNullOrWhiteSpace(cropRoot))throw new ArgumentException("Pinned approved v03 source required.");
   var ids=new Dictionary<string,string>{{"레이나","op_reina"},{"카일","op_kyle"},{"이브","op_eve"},{"세라","op_sera"}};
   var clips=new JArray();var keys=new HashSet<string>();
   foreach(var c in originals){
    if(!ids.TryGetValue((string)c["character"],out var id))throw new ArgumentException("Unknown approved actor.");int d=(int)c["direction"];if(d<1||d>4)throw new ArgumentException("Direction must be 1..4.");
    var originalFrames=(JArray)c["frames"];if(originalFrames==null||originalFrames.Sum(f=>(long)f["duration_ms"])!=(long)c["duration_ms"])throw new ArgumentException("Original exposure sum mismatch.");
    foreach(string motion in id=="op_kyle"?new[]{"idle","attack"}:new[]{(string)c["motion"]=="대기"?"idle":(string)c["motion"]=="공격"?"attack":throw new ArgumentException("Unknown motion.")}){
     if(!keys.Add(id+":"+d+":"+motion))throw new ArgumentException("Duplicate clip.");
     int start=id=="op_kyle"&&motion=="attack"?12:0,count=id=="op_reina"?(motion=="idle"?4:24):12;
     if(originalFrames.Count!=(id=="op_kyle"?24:count))throw new ArgumentException("Approved exposure count mismatch.");
     JToken idle=originals.SingleOrDefault(x=>(string)x["character"]==(string)c["character"]&&(int)x["direction"]==d&&(string)x["motion"]=="대기");
     var bounds=(JArray)(idle??c)["frames"][0]["alpha_bbox"];
     float scale=1,ax=0,ay=0;
     bool cropped=id=="op_kyle"||id=="op_reina"&&motion=="attack";
     if(cropped){scale=.9f;float offset=id=="op_kyle"?new[]{-1.4123f,8.9397f,9.3789f,11.8013f}[d-1]:new[]{-15f,-12f,-3f,-2f}[d-1];ax=550-offset;ay=id=="op_kyle"?577.7f:732.6f;}
     else if(id=="op_sera"){scale=114f/1536;ax=motion=="attack"?648.5f:548.5f;ay=motion=="attack"?1680:1510;}
     else{float height=id=="op_reina"&&(d==1||d==4)?107.1f:108;scale=height/((float)bounds[3]-(float)bounds[1]);ax=((float)bounds[0]+(float)bounds[2])/2;ay=(float)bounds[3];}
     if(float.IsNaN(scale)||float.IsInfinity(scale)||scale<=0)throw new ArgumentException("Invalid fixed anchor.");
     var frames=new JArray();for(int n=0;n<count;n++){
      var raw=originalFrames[start+n];long ms=(long)raw["duration_ms"];if(ms<=0)throw new ArgumentException("Positive exposure required.");
      string path=(string)raw["source"];float x=0,y=0;
      if(cropped){var item=id=="op_kyle"?directions[d-1]["kyle"][start+n]:directions[d-1]["reina"][n][0];path=cropRoot+(string)item["src"];x=(float)item["x"];y=(float)item["y"];}
      var dimensions=size(path);if(dimensions.x<=0||dimensions.y<=0)throw new ArgumentException("Image dimensions required.");
      frames.Add(new JObject{["path"]=path,["durationMs"]=ms,["rawSource"]=raw["source"].DeepClone(),["rawSha256"]=raw["sha256"].DeepClone(),["x"]=(x-ax)*scale,["y"]=(y-ay)*scale,["w"]=dimensions.x*scale,["h"]=dimensions.y*scale,["canvas"]=new JArray(dimensions.x,dimensions.y)});
     }
     clips.Add(new JObject{["actorId"]=id,["direction"]=d,["motion"]=motion,["durationMs"]=frames.Sum(f=>(long)f["durationMs"]),["scale"]=scale,["anchor"]=new JArray(ax,ay),["coordinateSource"]=cropped?"APPROVED_CROP":"RAW_CANVAS",["frames"]=frames});
    }
   }
   if(clips.Count!=32)throw new ArgumentException("All 32 clips required.");return new FourAgentV03Catalog(new JObject{["version"]=Version,["clips"]=clips});
  }
  public JObject Sample(string actorId,int direction,bool attacking,long attackAgeMs,long continuousMs,Vector2 foot){
   if(attackAgeMs<0||continuousMs<0||direction<1||direction>4)throw new ArgumentOutOfRangeException();
   JToken Clip(string motion)=>data["clips"].Single(c=>(string)c["actorId"]==actorId&&(int)c["direction"]==direction&&(string)c["motion"]==motion);
   var clip=Clip(attacking?"attack":"idle");if(attacking&&attackAgeMs>=(long)clip["durationMs"]){attacking=false;clip=Clip("idle");}
   long time=attacking?attackAgeMs:continuousMs%(long)clip["durationMs"];int index=0;var frames=(JArray)clip["frames"];while(time>=(long)frames[index]["durationMs"]){time-=(long)frames[index]["durationMs"];index++;}
   var result=(JObject)frames[index].DeepClone();result["frameIndex"]=index;result["attacking"]=attacking;result["scale"]=clip["scale"].DeepClone();result["anchor"]=clip["anchor"].DeepClone();result["x"]=(float)result["x"]+foot.x;result["y"]=(float)result["y"]+foot.y;return result;
  }
 }
}
