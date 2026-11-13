using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Presentation.Battle {
 // Stateless translation of approved procedural effects. Browser blur/AA is not a pixel identity contract.
 internal static class FiveAgentVisualFx {
  static JArray C(float r,float g,float b,float a=1)=>new JArray(r,g,b,a);
  static JArray Hex(int rgb,float a=1)=>C(((rgb>>16)&255)/255f,((rgb>>8)&255)/255f,(rgb&255)/255f,a);
  static JArray Stop(float radius,JArray color){var stop=new JArray(radius);foreach(var c in color)stop.Add(c.DeepClone());return stop;}
  static float Scale(Matrix4x4 m)=>m.MultiplyVector(Vector3.right).magnitude;
  static JObject Shape(JArray rows,string kind,string role,int slot,int order,Matrix4x4 m,Vector2 p,Vector2 dimensions,JArray color,float angle=0){
   var q=m.MultiplyPoint3x4(p);var row=new JObject{["kind"]=kind,["role"]=role,["slot"]=slot,["order"]=order,["x"]=q.x,["y"]=q.y,["w"]=dimensions.x*Scale(m),["h"]=dimensions.y*Scale(m),["angle"]=angle,["color"]=color,["path"]=JValue.CreateNull()};rows.Add(row);return row;
  }
  static JObject Disc(JArray rows,string role,int slot,int order,Matrix4x4 m,Vector2 p,float radius,JArray color)=>Shape(rows,"disc",role,slot,order,m,p,Vector2.one*radius*2,color);
  static JObject Glow(JArray rows,string role,int slot,int order,Matrix4x4 m,Vector2 p,float radius,JArray center,params JArray[] stops){var row=Shape(rows,"glow",role,slot,order,m,p,Vector2.one*radius*2,center);row["gradient"]=new JArray(stops);return row;}
  static void Ring(JArray rows,string role,int slot,int order,Matrix4x4 m,Vector2 p,float rx,float ry,float width,JArray color,float angle=0){var row=Shape(rows,"ring",role,slot,order,m,p,new Vector2(rx*2+width,ry*2+width),color,angle);row["innerRatioX"]=Mathf.Max(0,(rx-width/2)/(rx+width/2));row["innerRatioY"]=Mathf.Max(0,(ry-width/2)/(ry+width/2));}
  static void Line(JArray rows,string role,int slot,Matrix4x4 m,Vector2 a,Vector2 b,float width,JArray color){Shape(rows,"line",role,slot,0,m,(a+b)/2,new Vector2(Vector2.Distance(a,b),width),color,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);}
  public static void KyleImpact(JArray rows,int slot,Matrix4x4 m,Vector2 p,float age){
   float u=age/160,fade=1-u,r=17*(.4f+.6f*u);
   Glow(rows,"impact",slot,0,m,p,r*1.3f,Hex(0xefffff,fade*.85f),Stop(0,Hex(0xefffff,fade*.85f)),Stop(.28f,Hex(0x8bffff,fade*.85f)),Stop(.58f,Hex(0x20cde6,fade*.85f*170/255f)),Stop(1,Hex(0x20cde6,0)));
   if(u<.5f){var star=Shape(rows,"star","impactStar",slot,0,m,p,Vector2.one*r*1.4f,Hex(0xdfffff,fade));star["innerRatioX"]=3/(r*.7f);}
   foreach(float a in new[]{.2f,1.5f,2.7f,3.8f,5.1f}){var axis=new Vector2(Mathf.Cos(a),Mathf.Sin(a));Line(rows,"impactSpark",slot,m,p+axis*r,p+axis*(r+5*fade),2,Hex(0x5be9ff,fade));}
  }
  public static void Charge(JArray rows,int slot,Matrix4x4 m,Vector2 p,float value){
   const float s=3.611111111111111f;
   var center=Hex(0xffffdc,.4f+value*.6f);
   Glow(rows,"charge",slot,0,m,p,(42+value*48)*s,center,Stop(0,center),Stop(.3f,Hex(0xffbb28,.2f+value*.5f)),Stop(1,Hex(0xffa014,0)));
   float r=(85*(1-value)+18)*s;
   Ring(rows,"chargeRing",slot,0,m,p,r,r,(10+value*5)*s,Hex(0x18202d));
   Ring(rows,"chargeRing",slot,0,m,p,r,r,(5+value*5)*s,Hex(0xffeb9b,.65f+.35f*value));
   for(int k=0;k<6;k++){float a=k*Mathf.PI/3+.2f,rad=(125*(1-value)+15)*s;Disc(rows,"chargeParticle",slot,0,m,p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*rad,(7+value*5)*s,Hex(0xfff3ba));}
   Disc(rows,"chargeCore",slot,0,m,p,(6+value*13)*s,Hex(0xfffbdc));
  }
  public static void Muzzle(JArray rows,int slot,Matrix4x4 m,Vector2 p){Disc(rows,"droneFlash",slot,0,m,p,18.055555555555557f,Hex(0xfff0ad));}
  public static void Glove(JArray rows,int slot,Matrix4x4 m,Vector2 p,float clock){
   const float s=.75f;float age=clock%800;int order=11+slot*2;
   if(age>=320&&age<560){float u=(age-320)/240;GloveGlow(rows,slot,order,m,p,(9+u*7)*s,.3f+u*.35f);for(int i=0;i<6;i++){float a=i*Mathf.PI/3+.3f,r=(23*(1-u)+3)*s;Disc(rows,"gloveParticle",slot,order,m,p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,(1.7f+u)*s,Hex(0xfff3ba));}}
   if(age>=560&&age<680){float u=(age-560)/120;Ring(rows,"gloveRing",slot,order,m,p,(5+u*16)*s,(3+u*10)*s,2*s,Hex(0xffcd68,1-u),-.4f*Mathf.Rad2Deg);}
   if(age>=560)GloveGlow(rows,slot,order,m,p,8*s,.25f);
   if(age<120)GloveGlow(rows,slot,order,m,p,14*s,1-age/120);
  }
  static void GloveGlow(JArray rows,int slot,int order,Matrix4x4 m,Vector2 p,float radius,float alpha){var c=Hex(0xfff8d6,alpha);Glow(rows,"glove",slot,order,m,p,radius,c,Stop(0,c),Stop(.3f,Hex(0xffb52e,alpha*.8f)),Stop(1,Hex(0xffb52e,0)));}
 }
}
