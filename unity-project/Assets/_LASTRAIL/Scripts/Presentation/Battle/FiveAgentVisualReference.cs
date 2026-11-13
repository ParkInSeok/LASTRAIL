using System;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Presentation.Battle {
 /// <summary>Approved pixels and PNG exposures only. Reference sampling has no attack/HP/clock authority.</summary>
 public sealed class FiveAgentVisualReference {
  public const string Version="direction.five-agent-combat-fx.v02.20261017";
  readonly JObject contract,manifest,projectile;readonly Func<string,Vector2> size;
  FiveAgentVisualReference(JObject c,JObject m,JObject p,Func<string,Vector2> size){contract=(JObject)c.DeepClone();manifest=(JObject)m.DeepClone();projectile=(JObject)p.DeepClone();this.size=size;}
  public static FiveAgentVisualReference Read(JObject c,JObject m,JObject p,string independentContractHash,Func<string,Vector2> size){
   if(c==null||m==null||p==null||size==null||BDirectAttackInput.Digest(c)!=independentContractHash||(string)c["version"]!=Version
    ||(bool)c["rules"]["runtime_auto_direction_cycle"]||(bool)c["rules"]["actor_interpolation"]||((JArray)c["slots"]).Count!=5||((JArray)m["directions"]).Count!=4
    ||(float)c["coordinate_system"]["scale_relative_to_manifest"]!=.9f)throw new ArgumentException("Approved visual reference/pin required.");
   return new FiveAgentVisualReference(c,m,p,size);
  }
  static Matrix4x4 T(float x,float y)=>Matrix4x4.Translate(new Vector3(x,y));
  static Matrix4x4 S(float value)=>Matrix4x4.Scale(new Vector3(value,value,1));
  static Matrix4x4 R(float radians)=>Matrix4x4.Rotate(Quaternion.Euler(0,0,radians*Mathf.Rad2Deg));
  static Vector2 P(Matrix4x4 m,Vector2 p)=>m.MultiplyPoint3x4(p);
  static JArray Color(float r,float g,float b,float a=1)=>new JArray(r,g,b,a);
  static JObject Layer(string kind,string role,int slot,int order,Vector2 center,Vector2 dimensions,float angle=0)=>new JObject{["kind"]=kind,["role"]=role,["slot"]=slot,["order"]=order,["x"]=center.x,["y"]=center.y,["w"]=dimensions.x,["h"]=dimensions.y,["angle"]=angle,["path"]=JValue.CreateNull(),["color"]=Color(1,1,1)};
  void Image(JArray output,string role,int slot,int order,string path,Matrix4x4 matrix,float x,float y,float w=-1,float h=-1,JArray uv=null){var natural=size(path);if(w<0)w=natural.x;if(h<0)h=natural.y;var u=P(matrix,new Vector2(x+w,y))-P(matrix,new Vector2(x,y));var v=P(matrix,new Vector2(x,y+h))-P(matrix,new Vector2(x,y));var row=Layer("image",role,slot,order,P(matrix,new Vector2(x+w/2,y+h/2)),new Vector2(u.magnitude,v.magnitude),Mathf.Atan2(u.y,u.x)*Mathf.Rad2Deg);row["path"]=path;row["uv"]=uv??new JArray(0,0,1,1);output.Add(row);}
  void Glow(JArray output,string role,int slot,int order,Matrix4x4 matrix,Vector2 point,float radius,JArray color){float scale=(P(matrix,Vector2.right)-P(matrix,Vector2.zero)).magnitude;var row=Layer("glow",role,slot,order,P(matrix,point),Vector2.one*(radius*scale*2));row["color"]=color;output.Add(row);}
  void Beam(JArray output,int slot,Matrix4x4 m,Vector2 from,Vector2 to,float width,JArray color){var a=P(m,from);var b=P(m,to);float scale=(P(m,Vector2.right)-P(m,Vector2.zero)).magnitude;var row=Layer("roundedLine","beam",slot,0,(a+b)/2,new Vector2(Vector2.Distance(a,b)+width*scale,width*scale),Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);row["color"]=color;output.Add(row);}
  Vector2 Center(JToken item){var dimensions=size((string)item["src"]);return new Vector2((float)item["x"]+dimensions.x/2,(float)item["y"]+dimensions.y/2);}
  static Vector2 ReferenceTarget(float y,int d){if(d==3)return new Vector2(520,400);float v=Mathf.Clamp(y-85+new[]{-110,0,90,0}[d],640,1100);return new Vector2(340-(v-700)*.25f,v);}
  static Vector2 Forward(float footY,float baseline,Vector2 start,Vector2 axis){float y=(400-footY)/.9f+baseline;if(Mathf.Abs(axis.y)<1e-6f)throw new ArgumentException("Approved forward axis missing.");return new Vector2(start.x+(y-start.y)*axis.x/axis.y,y);}
  public JObject SampleReference(int direction,long ageMs,bool effects){
   if(direction<0||direction>3||ageMs<0)throw new ArgumentOutOfRangeException();var layers=new JArray();var actors=new JArray();var data=manifest["directions"][direction];var align=contract["coordinate_system"]["alignment_by_direction"][direction];
   for(int slot=0;slot<5;slot++){
    var row=contract["slots"][slot];string actor=(string)row["preview_actor"];float footX=(float)row["foot_anchor"][0],footY=(float)row["foot_anchor"][1],baseline=(float)contract["coordinate_system"]["native_baselines"][actor],offset=actor=="eve"?0:(float)align[actor];var transform=T(footX,footY)*S(.9f)*T(-550,-baseline)*T(offset,0);
    actors.Add(new JObject{["slot"]=slot,["actor"]=actor,["placeholder"]=row["placeholder"].DeepClone(),["runtimeId"]=JValue.CreateNull(),["foot"]=row["foot_anchor"].DeepClone()});
    var target=(ReferenceTarget(footY,direction)-new Vector2(footX,footY))/.9f+new Vector2(550-offset,baseline);
    if(actor=="kyle"){
     long age=ageMs%2480;int index=0;foreach(var duration in (JArray)manifest["kyle_durations"]){if(age<(int)duration)break;age-=(int)duration;index++;}var frame=data["kyle"][Math.Min(index,23)];Image(layers,"actor",slot,10+slot*2,(string)frame["src"],transform,(float)frame["x"],(float)frame["y"]);
     if(effects){float shotAge=ageMs%2480-(float)projectile["fire_ms"];var e=projectile["emitters"][direction];var origin=new Vector2((float)e["x"],(float)e["y"]);if(direction==3){float a=(float)e["angle"]*Mathf.Deg2Rad;target=Forward(footY,baseline,origin,new Vector2(Mathf.Cos(a),Mathf.Sin(a)));}if(shotAge>=0&&shotAge<460){var axis=(target-origin).normalized;float angle=Mathf.Atan2(axis.y,axis.x);float distance=Mathf.Lerp((float)projectile["launch_offset"],Vector2.Distance(origin,target),Mathf.Clamp01(shotAge/300));var point=origin+axis*distance;
      if(shotAge<300){var uv=(JArray)projectile["crop"];var tex=size("kyle-projectile-source.png");Image(layers,"projectile",slot,0,"kyle-projectile-source.png",transform*T(point.x,point.y)*R(angle-Mathf.PI),-11,-6,22,12,new JArray((float)uv[0]/tex.x,1-((float)uv[1]+(float)uv[3])/tex.y,(float)uv[2]/tex.x,(float)uv[3]/tex.y));if(shotAge<90){var flash=Layer("kyleFlash","muzzle",slot,0,P(transform,origin),new Vector2(26,26)*.9f,angle*Mathf.Rad2Deg);flash["color"]=Color(1,1,1,1-shotAge/90);layers.Add(flash);}}
      else FiveAgentVisualFx.KyleImpact(layers,slot,transform,point,shotAge-300);
     }}
    }else if(actor=="reina"){
     long age=ageMs%960;var frame=data["reina"][(int)(age/40)];Image(layers,"actor",slot,10+slot*2,(string)frame[0]["src"],transform,(float)frame[0]["x"],(float)frame[0]["y"]);
     if(effects){if(frame[1].Type!=JTokenType.Null)Image(layers,"muzzle",slot,11+slot*2,(string)frame[1]["src"],transform,(float)frame[1]["x"],(float)frame[1]["y"]);var start=Center(data["reina"][10][2]);var finish=Center(data["reina"][15][2]);if(direction==3)target=Forward(footY,baseline,start,finish-start);if(age>=400&&age<620){var item=data["reina"][Math.Min(15,10+(int)(age-400)/40)][2];var dimensions=size((string)item["src"]);float rotation=Mathf.Atan2(target.y-start.y,target.x-start.x)-Mathf.Atan2(finish.y-start.y,finish.x-start.x);var point=Vector2.Lerp(start,target,(age-400)/220f);Image(layers,"projectile",slot,0,(string)item["src"],transform*T(point.x,point.y)*R(rotation),-dimensions.x/2,-dimensions.y/2);}var impact=data["impact"][(int)age/20];if(impact.Type!=JTokenType.Null){var dimensions=size((string)impact["src"]);Image(layers,"impact",slot,0,(string)impact["src"],transform,target.x-dimensions.x/2,target.y-dimensions.y/2);}}
    }else Eve(layers,slot,direction,ageMs,effects,transform,(float)align["eveX"],target);
   }
   return new JObject{["version"]="unity.five-agent-reference-frame.v1.20261017",["classification"]="APPROVED_VISUAL_REFERENCE_FIXTURE_NOT_GAMEPLAY",["direction"]=direction,["targetKind"]=direction==3?"FORWARD_OBSTACLE_REFERENCE":"ENEMY_TRAIN_REFERENCE",["gameplayAuthority"]=false,["actors"]=actors,["layers"]=new JArray(layers.OrderBy(l=>(int)l["order"]))};
  }
  void Eve(JArray layers,int slot,int d,long time,bool effects,Matrix4x4 transform,float eveX,Vector2 target){
   long age=time%3200;var exposure=((JArray)contract["timing_reference"]["eve"]["actor_exposures"][d]["exposures_ms"]).Single(e=>age>=(long)e[0]&&age<(long)e[1]);Image(layers,"actor",slot,10+slot*2,"eve"+(d+1)+"/"+(string)exposure[2]+".png",transform,eveX,550,112.6961f,112.6961f);
   float phase=(float)(time%3200)/3200*Mathf.PI*2;var center=new Vector2(443+Mathf.Sin(phase)*14,620+Mathf.Cos(phase)*22);var origin=center-Vector2.one*(627*.0684f);var dm=transform*T(origin.x,origin.y)*S(.0684f);int facing=d==0||d==3?4:2;
   Vector2 pivot=facing==4?new Vector2(626,650):new Vector2(660,610),muzzle=facing==4?new Vector2(625,220):new Vector2(138,580),rear=facing==4?new Vector2(628,640):new Vector2(850,460),left=facing==4?new Vector2(330,720):new Vector2(485,415),right=facing==4?new Vector2(918,720):new Vector2(690,885);
   var offset=effects?new Vector2(Mathf.Sin(phase)*55,Mathf.Sin(phase)*18):Vector2.zero;var moved=pivot+offset;var aim=(target-origin)/.0684f;var u=(muzzle-rear).normalized;var q=muzzle-pivot;var v=aim-moved;float angle=effects?Mathf.Atan2(v.y,v.x)-Mathf.Atan2(u.y,u.x)-Mathf.Asin(Mathf.Clamp((u.x*q.y-u.y*q.x)/v.magnitude,-1,1)):0;
   bool charge=age>=1500&&age<1820,firing=age>=1820&&age<1940;float droneClock=charge?560+(age-1500)*240/320f:firing?age-1820:240,clock=droneClock%800;float recoil=effects&&clock<160?10*Mathf.Sin(clock/160*Mathf.PI):0;var rotation=R(angle);var axis=(Vector2)rotation.MultiplyVector(u);var barrel=moved+(Vector2)rotation.MultiplyVector(q)-axis*recoil;
   var body=dm*T(offset.x,offset.y)*T(pivot.x,pivot.y)*R(effects?Mathf.Cos(phase)*.018f:0)*T(-pivot.x,-pivot.y);float pod=effects?Mathf.Sin(phase)*.028f:0;
   var lp=body*T(left.x,left.y)*R(pod)*T(-left.x,-left.y);if(facing==4)lp=lp*T(5,115)*S(.8f);Image(layers,"drone",slot,0,"d"+facing+"-left.png",lp,0,0);
   Image(layers,"drone",slot,0,"d"+facing+"-body.png",body,0,0);Image(layers,"drone",slot,0,"d"+facing+"-right.png",body*T(right.x,right.y)*R(-pod)*T(-right.x,-right.y),0,0);
   var head=dm*T(moved.x,moved.y)*rotation*T(-u.x*recoil,-u.y*recoil)*T(-pivot.x,-pivot.y)*T(facing==2?38:125.6f,facing==2?12:43)*S(.8f);Image(layers,"drone",slot,0,"d"+facing+"-head.png",head,0,0);
   if(effects&&charge)FiveAgentVisualFx.Charge(layers,slot,dm,barrel,(age-1500)/320f);
   if(effects&&firing&&age-1820<75)FiveAgentVisualFx.Muzzle(layers,slot,dm,barrel);
   if(effects&&firing){float alpha=1-(age-1820)/150f;Beam(layers,slot,dm,barrel,aim,152.7778f,Color(.14f,.14f,.2f,alpha));Beam(layers,slot,dm,barrel,aim,116.6667f,Color(1,.71f,.18f,alpha));Beam(layers,slot,dm,barrel,aim,36.1111f,Color(1,.97f,.84f,alpha));}
   if(effects&&age>=1200&&age<2020){var hand=new[]{new Vector2(380,180),new Vector2(290,225),new Vector2(400,450),new Vector2(760,220)}[d]*(112.6961f/1254)+new Vector2(eveX,550);float gloveClock=age<1500?320+(age-1200)*240/300f:charge?560+(age-1500)*240/320f:firing?age-1820:240;FiveAgentVisualFx.Glove(layers,slot,transform,hand,gloveClock);}
   // Existing procedural smoke is an independent visual layer, never actor interpolation.
   for(int k=0;k<18;k++){float value=(float)((time/3200.0+k/18.0)%1);float x=new[]{542,524,550,640}[d]-30*value+12*Mathf.Sin(value*8+k*.8f)*value,y=(d==0?229:242)-210*value;var point=new Vector2(eveX,550)+new Vector2(x,y)*(112.6961f/1254);Glow(layers,"smoke",slot,11+slot*2,transform,point,(2+13*value)*112.6961f/1254,Color(210/255f,221/255f,226/255f,.28f*Mathf.Sin(Mathf.PI*value)));}
  }
 }
}