using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Presentation.Battle {
 // Visual-only v03 companions. Continuous motion and attack envelopes have separate clocks.
 public static class FourAgentV03Effects {
  const string Root="D:/Project/LASTRAIL/QA/PlanImpact-20261015/eve-drone-motion-review-v01/";
  static readonly string[] EveFolders={"eve-camera-d1-command-motion-v03/","eve-camera-d2-command-motion-v05/","eve-camera-d3-command-motion-v02/","eve-camera-d4-command-motion-v01/"};
  static readonly string[] SeraFolders={"sera-camera-d1-attack-motion-v01/","sera-camera-d2-attack-motion-v03/","sera-camera-d3-attack-motion-v01/","sera-camera-d4-attack-motion-v02/"};
  public static string[] TexturePaths {get {var paths=new List<string>();for(int d=0;d<4;d++){foreach(var part in new[]{"body","head","left","right"})paths.Add(EvePath(d,part));paths.Add(Root+SeraFolders[d]+"crystal.png");}return paths.ToArray();}}
  static string EvePath(int d,string part)=>Root+EveFolders[d]+(d==3?"d4-":"d2-")+part+".png";
  static Matrix4x4 T(Vector2 p)=>Matrix4x4.Translate(p);
  static Matrix4x4 S(float s)=>Matrix4x4.Scale(new Vector3(s,s,1));
  static Matrix4x4 R(float a)=>Matrix4x4.Rotate(Quaternion.Euler(0,0,a*Mathf.Rad2Deg));
  static Vector2 V(float x,float y)=>new Vector2(x,y);
  static Vector2 Rotate(Vector2 p,float a)=>V(Mathf.Cos(a)*p.x-Mathf.Sin(a)*p.y,Mathf.Sin(a)*p.x+Mathf.Cos(a)*p.y);
  static JArray Color(int rgb,float a=1)=>new JArray(((rgb>>16)&255)/255f,((rgb>>8)&255)/255f,(rgb&255)/255f,a);
  static JObject Row(JArray rows,string kind,string role,int slot,int order,Matrix4x4 m,Vector2 p,Vector2 dimensions,int color=0xffffff,float alpha=1,float angle=0){Vector2 q=m.MultiplyPoint3x4(p);Vector2 axis=m.MultiplyVector(Vector3.right);var row=new JObject{["kind"]=kind,["role"]=role,["slot"]=slot,["order"]=order,["x"]=q.x,["y"]=q.y,["w"]=dimensions.x*axis.magnitude,["h"]=dimensions.y*axis.magnitude,["angle"]=angle*Mathf.Rad2Deg+Mathf.Atan2(axis.y,axis.x)*Mathf.Rad2Deg,["color"]=Color(color,alpha),["path"]=JValue.CreateNull()};rows.Add(row);return row;}
  static void Image(JArray rows,string role,int slot,int order,Matrix4x4 m,string path,Func<string,Vector2> size){var dim=size(path);if(dim.x<=0||dim.y<=0)throw new ArgumentException("Approved texture dimensions required.");Row(rows,"image",role,slot,order,m,dim/2,dim)["path"]=path;}
  static void Line(JArray rows,string role,int slot,Matrix4x4 m,Vector2 a,Vector2 b,float width,int color,float alpha=1,bool rounded=false){Row(rows,rounded?"roundedLine":"line",role,slot,0,m,(a+b)/2,V(Vector2.Distance(a,b)+(rounded?width:0),width),color,alpha,Mathf.Atan2(b.y-a.y,b.x-a.x));}
  public static void Append(JArray layers,string actorId,int direction1to4,long attackAgeMs,long continuousMs,bool attacking,bool effects,bool smoke,Vector2 foot,Vector2? target,FourAgentV03Catalog catalog,Func<string,Vector2> size){
   if(layers==null||catalog==null||size==null)throw new ArgumentNullException();if(direction1to4<1||direction1to4>4||attackAgeMs<0||continuousMs<0)throw new ArgumentOutOfRangeException();if(actorId!="op_eve"&&actorId!="op_sera")return;
   int d=direction1to4-1;var idle=catalog.Sample(actorId,direction1to4,false,0,continuousMs,foot);float scale=(float)idle["scale"];var anchor=V((float)idle["anchor"][0],(float)idle["anchor"][1]);var m=T(foot)*S(scale)*T(-anchor);Vector2? rawTarget=target.HasValue?(Vector2?)((target.Value-foot)/scale+anchor):null;
   if(actorId=="op_eve"){Eve(layers,d,attackAgeMs,continuousMs,attacking&&attackAgeMs<2400,effects,m,rawTarget,size);if(smoke)Smoke(layers,d,continuousMs,m);}else Sera(layers,d,attackAgeMs,continuousMs,attacking&&attackAgeMs<2160,effects,m,rawTarget,size);
  }
  static void Eve(JArray rows,int d,long t,long continuous,bool attack,bool effects,Matrix4x4 raw,Vector2? target,Func<string,Vector2> size){
   float outerPhase=(continuous%2400)/2400f*Mathf.PI*2;var origin=new[]{V(80,520),V(-100,450),V(20,250),V(900,290)}[d];if(d!=1)origin+=V(Mathf.Sin(outerPhase),Mathf.Cos(outerPhase))*8;
   var m=raw*T(origin)*S(.22f);bool active=attack&&t>=900&&t<1460;float clock=!attack?240:t<1000?240:t<1300?560+(t-1000)/300f*239:t<1460?t-1300:240,age=clock%800;
   var pivotBase=d==3?V(626,650):V(660,610);var muzzleBase=d==3?V(625,220):V(138,580);var rear=d==3?V(628,640):V(850,460);var left=d==3?V(330,720):V(485,415);var right=d==3?V(918,720):V(690,885);
   float phase=(continuous%3200)/3200f*Mathf.PI*2,tilt=Mathf.Cos(phase)*.018f,pod=Mathf.Sin(phase)*.028f;var offset=V(55,18)*Mathf.Sin(phase);var pivot=pivotBase+offset;var axis=(muzzleBase-rear).normalized;var q=muzzleBase-pivotBase;Vector2 localTarget=target.HasValue?(target.Value-origin)/.22f:Vector2.zero;float angle=0;
   if(target.HasValue){var v=localTarget-pivot;if(v.sqrMagnitude>.000001f)angle=Mathf.Atan2(v.y,v.x)-Mathf.Atan2(axis.y,axis.x)-Mathf.Asin(Mathf.Clamp((axis.x*q.y-axis.y*q.x)/v.magnitude,-1,1));}
   float recoil=attack&&age<160?10*Mathf.Sin(age/160*Mathf.PI):0;var muzzle=pivot+Rotate(q,angle)-Rotate(axis,angle)*recoil;
   var body=m*T(offset)*T(pivotBase)*R(tilt)*T(-pivotBase);var leftTransform=body*T(left)*R(pod)*T(-left);if(d==3)leftTransform=leftTransform*T(V(5,115))*S(.8f);
   Image(rows,"drone",2,8,leftTransform,EvePath(d,"left"),size);Image(rows,"drone",2,8,body,EvePath(d,"body"),size);Image(rows,"drone",2,8,body*T(right)*R(-pod)*T(-right),EvePath(d,"right"),size);
   Image(rows,"drone",2,8,m*T(pivot)*R(angle)*T(-axis*recoil)*T(-pivotBase)*T(d==3?V(125.6f,43):V(38,12))*S(.8f),EvePath(d,"head"),size);
   if(!effects||!active)return;
   if(age>=560)FiveAgentVisualFx.Charge(rows,2,m,muzzle,(age-560)/240);
   if(age<75)FiveAgentVisualFx.Muzzle(rows,2,m,muzzle);
   if(age<120&&target.HasValue){float alpha=1-age/150;Line(rows,"beam",2,m,muzzle,localTarget,152.77778f,0x242332,alpha,true);Line(rows,"beam",2,m,muzzle,localTarget,116.66667f,0xffb52e,alpha,true);Line(rows,"beam",2,m,muzzle,localTarget,36.11111f,0xfff8d6,alpha,true);}
   FiveAgentVisualFx.Glove(rows,2,raw,new[]{V(332,277),V(215,300),V(290,506),V(803,265)}[d],t<1000?320+(t-900)*2.4f:clock);
  }
  static float Smooth(float x){x=Mathf.Clamp01(x);return x*x*(3-2*x);}
  static readonly Vector2[][] Ends={new[]{new Vector2(100,420),new Vector2(490,-70),new Vector2(150,720)},new[]{new Vector2(150,530),new Vector2(490,-70),new Vector2(115,830)},new[]{new Vector2(70,650),new Vector2(490,-70),new Vector2(150,960)},new[]{new Vector2(160,400),new Vector2(520,-70),new Vector2(940,400)}};
  static void Sera(JArray rows,int d,long t,long continuous,bool attack,bool effects,Matrix4x4 m,Vector2? target,Func<string,Vector2> size){
   float u=!attack?0:t<1440?Smooth((t-180)/360f):1-Smooth((t-1440)/720f);string path=Root+SeraFolders[d]+"crystal.png";var dim=size(path);if(dim.x<=0||dim.y<=0)throw new ArgumentException("Crystal canvas dimensions required.");float scale=m.MultiplyVector(Vector3.right).magnitude;
   for(int k=0;k<3;k++){float a=(continuous%6480)/6480f*Mathf.PI*2+k*2*Mathf.PI/3-.3f,depth=Mathf.Sin(a);var p=Vector2.Lerp(V(550+350*Mathf.Cos(a),700+135*depth),Ends[d][k],u);float aim=target.HasValue?Mathf.Atan2(target.Value.y-p.y,target.Value.x-p.x):0;float rotation=target.HasValue?(aim+Mathf.PI/2)*u:0;
    var row=Row(rows,"image","crystal",3,depth<0&&u<.7f?8:12,m,p,V(175*542f/1300,175),angle:rotation);row["path"]=path;row["uv"]=new JArray(240/dim.x,1-1389/dim.y,542/dim.x,1300/dim.y);
    if(!effects||!attack||t<360||t>=1460)continue;float s=t-180,r,alpha,beam=0;if(s<240){r=70;alpha=s/240;}else if(s<900){float v=(s-240)/660;r=70-56*v*v;alpha=1;}else if(s<1200){r=14;alpha=1;float b=s-900;beam=b<20?b/20:b<200?1:1-(b-200)/100;}else{r=14+8*(s-1200)/80;alpha=1-(s-1200)/80;}
    var emit=p+V(Mathf.Cos(aim),Mathf.Sin(aim))*102;Circle(rows,m,emit,r,aim,.45f,alpha,continuous/350f,beam>0);
    if(beam>0&&target.HasValue){Line(rows,"beam",3,m,emit,target.Value,5/scale,0x65dfcd,beam*.2f,true);Line(rows,"beam",3,m,emit,target.Value,2.4f/scale,0xbaffec,beam,true);Line(rows,"beam",3,m,emit,target.Value,1/scale,0xffffff,beam,true);}
   }
   if(effects&&attack&&d!=3&&t>=180&&t<540)Circle(rows,m,new[]{V(275,440),V(380,598),V(420,605)}[d],34,0,.8f,Mathf.Sin((t-180)/360f*Mathf.PI)*.6f,continuous/350f,false);
  }
  static void Circle(JArray rows,Matrix4x4 m,Vector2 p,float r,float angle,float tilt,float alpha,float phase,bool burst){
   if(alpha<=0)return;float width=burst?2.3f:1.5f;int color=burst?0xeffff8:0x94ead5;var basis=m*T(p)*R(angle);
   foreach(float radius in new[]{r,r*.76f}){var ring=Row(rows,"ring","magicCircle",3,0,basis,Vector2.zero,V((radius*2+width)*tilt,radius*2+width),color,alpha);ring["innerRatioX"]=ring["innerRatioY"]=Mathf.Max(0,(radius-width/2)/(radius+width/2));}
   Vector2 Local(Vector2 v){v=Rotate(v,phase);return V(v.x*tilt,v.y);}
   for(int k=0;k<6;k++){float a=k*Mathf.PI/3;var a1=Local(Rotate(V(r*.78f,-3),a));var b=Local(Rotate(V(r*.94f,0),a));var c=Local(Rotate(V(r*.78f,3),a));Line(rows,"magicCircle",3,basis,a1,b,width,color,alpha);Line(rows,"magicCircle",3,basis,b,c,width,color,alpha);}
   for(int k=0;k<3;k++){float a=k*2*Mathf.PI/3-Mathf.PI/2,b=(k+1)*2*Mathf.PI/3-Mathf.PI/2;Line(rows,"magicCircle",3,basis,Local(V(Mathf.Cos(a),Mathf.Sin(a))*r*.59f),Local(V(Mathf.Cos(b),Mathf.Sin(b))*r*.59f),width,color,alpha);}
  }
  static void Smoke(JArray rows,int d,long time,Matrix4x4 m){var anchor=new[]{V(511,301),V(514,284),V(532,304),V(630,265)}[d];for(int k=0;k<18;k++){float u=((time%3200)/3200f+k/18f)%1;var p=anchor+V(-30*u+12*Mathf.Sin(u*8+k*.8f)*u,-210*u);float alpha=.28f*Mathf.Sin(Mathf.PI*u);var row=Row(rows,"glow","smoke",2,15,m,p,Vector2.one*(2+13*u)*2,0xd2dde2,alpha);row["gradient"]=new JArray(new JArray(0,210/255f,221/255f,226/255f,alpha),new JArray(1,210/255f,221/255f,226/255f,0));}}
 }
}
