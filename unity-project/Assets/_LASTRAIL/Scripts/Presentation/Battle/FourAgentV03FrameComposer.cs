using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Presentation.Battle {
 // Explicit display inputs only. Host owns accepted action, target, clock and contact.
 public sealed class FourAgentV03FrameComposer {
  public const string CropRoot="D:/Project/LASTRAIL/QA/PlanImpact-20261015/eve-drone-motion-review-v01/five-agent-combat-fx-v02/";
  readonly FourAgentV03Assets assets;readonly FourAgentV03Catalog catalog;
  public FourAgentV03FrameComposer(FourAgentV03Assets assets){this.assets=assets??throw new ArgumentNullException(nameof(assets));catalog=assets.CreateCatalog();}
  static int Slot(string id)=>id=="op_kyle"?0:id=="op_reina"?1:id=="op_eve"?2:id=="op_sera"?3:throw new ArgumentException("Unknown actor.");
  public JObject Compose(string id,int direction,bool attacking,long attackAgeMs,long continuousMs,Vector2 foot,Vector2? target,bool effects=true,bool smoke=true,long? contactAgeMs=null,float referenceToDisplayScale=1){
   if(float.IsNaN(referenceToDisplayScale)||float.IsInfinity(referenceToDisplayScale)||referenceToDisplayScale<=0)throw new ArgumentOutOfRangeException(nameof(referenceToDisplayScale));int slot=Slot(id);var frame=catalog.Sample(id,direction,attacking,attackAgeMs,continuousMs,foot);bool active=(bool)frame["attacking"];var layers=new JArray();
   layers.Add(Image("actor",slot,(string)frame["path"],(float)frame["x"],(float)frame["y"],(float)frame["w"],(float)frame["h"],10));
   FourAgentV03Effects.Append(layers,id,direction,attackAgeMs,continuousMs,active,effects,smoke,foot,target,catalog,assets.Size);
   if(effects&&target.HasValue&&(id=="op_kyle"||id=="op_reina"))Ballistics(layers,id,direction,active?attackAgeMs:-1,foot,target.Value,contactAgeMs);
   foreach(JObject row in layers){row["direction"]=direction-1;if(id=="op_sera"&&(string)row["role"]=="beam"){float oldWidth=(float)row["h"],newWidth=oldWidth/referenceToDisplayScale;row["h"]=newWidth;row["w"]=(float)row["w"]-oldWidth+newWidth;}}
   return new JObject{["version"]="unity.five-agent-reference-frame.v1.20261017",["visualContract"]=FourAgentV03Catalog.Version,["classification"]="EXPLICIT_V03_DISPLAY_INPUT_NOT_COMBAT_AUTHORITY",["direction"]=direction-1,["gameplayAuthority"]=false,["actors"]=new JArray(new JObject{["runtimeId"]=id,["slot"]=slot,["foot"]=new JArray(foot.x,foot.y),["attacking"]=active,["frameIndex"]=frame["frameIndex"].DeepClone()}),["layers"]=layers};
  }
  public static JObject Combine(IEnumerable<JObject> frames){var values=frames.ToArray();if(values.Length==0)throw new ArgumentException("Explicit actor frames required.");var result=(JObject)values[0].DeepClone();result["actors"]=new JArray(values.SelectMany(f=>f["actors"]).Select(a=>a.DeepClone()));result["layers"]=new JArray(values.SelectMany(f=>f["layers"]).Select(a=>a.DeepClone()));if(result["actors"].Select(a=>(string)a["runtimeId"]).Distinct().Count()!=result["actors"].Count())throw new ArgumentException("Duplicate actor binding.");return result;}
  static JObject Image(string role,int slot,string path,float x,float y,float w,float h,int order=0)=>new JObject{["kind"]="image",["role"]=role,["slot"]=slot,["order"]=order,["path"]=path,["x"]=x+w/2,["y"]=y+h/2,["w"]=w,["h"]=h,["angle"]=0,["color"]=new JArray(1,1,1,1)};
  void Ballistics(JArray rows,string id,int direction,long age,Vector2 foot,Vector2 target,long? contactAge){
   int d=direction-1,slot=Slot(id);float offset=id=="op_kyle"?new[]{-1.4123f,8.9397f,9.3789f,11.8013f}[d]:new[]{-15f,-12f,-3f,-2f}[d],baseline=id=="op_kyle"?577.7f:732.6f;
   var m=Matrix4x4.Translate(foot)*Matrix4x4.Scale(new Vector3(.9f,.9f,1))*Matrix4x4.Translate(new Vector3(-550+offset,-baseline));Vector2 P(Vector2 p)=>m.MultiplyPoint3x4(p);
   var data=assets.Crops["directions"][d];
   if(id=="op_kyle"){
    var p=assets.Projectile;var e=p["emitters"][d];var origin=P(new Vector2((float)e["x"],(float)e["y"]));float shotAge=age-((float)p["fire_ms"]-1440);var delta=target-origin;
    if(age>=0&&shotAge>=0&&shotAge<(float)p["life_ms"]&&delta.sqrMagnitude>0){var point=Vector2.Lerp(origin+delta.normalized*(float)p["launch_offset"]*.9f,target,shotAge/(float)p["life_ms"]);var row=Image("projectile",slot,CropRoot+"kyle-projectile-source.png",point.x-9.9f,point.y-5.4f,19.8f,10.8f);row["angle"]=Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg-180;var crop=p["crop"];var size=assets.Size(CropRoot+"kyle-projectile-source.png");row["uv"]=new JArray((float)crop[0]/size.x,1-((float)crop[1]+(float)crop[3])/size.y,(float)crop[2]/size.x,(float)crop[3]/size.y);rows.Add(row);
     if(shotAge<90)rows.Add(new JObject{["kind"]="kyleFlash",["role"]="muzzle",["slot"]=slot,["order"]=0,["x"]=origin.x,["y"]=origin.y,["w"]=23.4f,["h"]=23.4f,["angle"]=Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,["color"]=new JArray(1,1,1,1-shotAge/90)});
    }
    if(contactAge>=0&&contactAge<(float)p["impact_ms"])FiveAgentVisualFx.KyleImpact(rows,slot,Matrix4x4.Translate(target)*Matrix4x4.Scale(new Vector3(.9f,.9f,1)),Vector2.zero,contactAge.Value);
   }else{
    Vector2 Center(JToken item){var s=assets.Size(CropRoot+(string)item["src"]);return new Vector2((float)item["x"]+s.x/2,(float)item["y"]+s.y/2);}
    void Place(JToken item,string role){if(item==null||item.Type==JTokenType.Null)return;var path=CropRoot+(string)item["src"];var s=assets.Size(path);var pos=P(new Vector2((float)item["x"],(float)item["y"]));rows.Add(Image(role,slot,path,pos.x,pos.y,s.x*.9f,s.y*.9f));}
    if(age>=0&&age<960){var f=data["reina"][(int)(age/40)];Place(f[1],"muzzle");if(age>=400&&age<620){var item=data["reina"][Math.Min(15,10+(int)(age-400)/40)][2];var origin=P(Center(data["reina"][10][2]));var end=P(Center(data["reina"][15][2]));var point=Vector2.Lerp(origin,target,(age-400)/220f);string path=CropRoot+(string)item["src"];var s=assets.Size(path)*.9f;var row=Image("projectile",slot,path,point.x-s.x/2,point.y-s.y/2,s.x,s.y);row["angle"]=(Mathf.Atan2(target.y-origin.y,target.x-origin.x)-Mathf.Atan2(end.y-origin.y,end.x-origin.x))*Mathf.Rad2Deg;rows.Add(row);}}
    if(contactAge>=0&&contactAge<340){var a=(JArray)data["impact"];int i=(int)((620+contactAge.Value)/20);if(i<a.Count&&a[i].Type!=JTokenType.Null){var path=CropRoot+(string)a[i]["src"];var s=assets.Size(path)*.9f;rows.Add(Image("impact",slot,path,target.x-s.x/2,target.y-s.y/2,s.x,s.y));}}
   }
  }
 }
}
