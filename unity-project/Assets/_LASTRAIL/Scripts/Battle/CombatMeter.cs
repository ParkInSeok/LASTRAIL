using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
// Fixed housing; value clips the luminous surface without moving its end caps.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatMeter:CombatLayeredGraphic {
 static readonly string[] Names={"Frame","Track","Glow","Fill","Core","Shield","Dividers"};protected override string[] LayerNames=>Names;
 protected override void ConstrainReplacement(string layer,VertexHelper mesh){var r=GetPixelAdjustedRect();var track=Inset(r,2);if(RoundedEnds)track=Expand(track,0,-1);float cut=Divisions>1?Mathf.Min(4,track.height*.4f):RoundedEnds?track.height*.5f:0;
  if(layer=="Frame"){CombatUiMeshPartition.RemoveInterior(mesh,CombatUiMeshPartition.Aperture(track,cut));return;}
  if(layer=="Fill"||layer=="Core"||layer=="Glow"){if(Value<=0){mesh.Clear();return;}float end=track.x+track.width*Value;if(layer=="Core")track=new Rect(track.x,track.y+track.height*.3f,track.width,track.height*.4f);if(layer=="Glow")track=Expand(track,1.5f,HullNeon?4.2f:2.7f);track.width=Mathf.Max(0,end-track.x);}
  else if(layer=="Shield"){if(ShieldValue<=0){mesh.Clear();return;}track=new Rect(track.x+track.width*Value,track.y,track.width*ShieldValue,track.height);}
  CombatUiMeshPartition.KeepInterior(mesh,CombatUiMeshPartition.Aperture(track,0));
 }
 public float Value {get;private set;}=1;
 public float ShieldValue {get;private set;}
 public int Divisions=1;
 public bool RoundedEnds,Gold,HullNeon,CyanBevel;
 public void SetValue(float value){value=float.IsNaN(value)?0:Mathf.Clamp01(value);if(Mathf.Approximately(value,Value)&&ShieldValue==0)return;Value=value;ShieldValue=0;SetVerticesDirty();}
 public void SetHealth(int hp,int maxHp,int shield){
  hp=Mathf.Max(0,hp);shield=Mathf.Max(0,shield);
  long denominator=System.Math.Max((long)maxHp,(long)hp+shield);
  float health=denominator>0?(float)(hp/(double)denominator):0;
  float barrier=denominator>0?(float)(shield/(double)denominator):0;
  // Exact equality avoids discarding subpixel amounts with an approximate epsilon.
  if(Value==health&&ShieldValue==barrier)return;Value=health;ShieldValue=barrier;SetVerticesDirty();
 }
 protected override void DrawLayer(string layer,VertexHelper mesh){
  mesh.Clear();var r=GetPixelAdjustedRect();if(r.width<2||r.height<2)return;
  bool charge=Divisions>1;var track=Inset(r,2);if(RoundedEnds)track=Expand(track,0,-1);
  float cut=charge?Mathf.Min(4,track.height*.4f):RoundedEnds?track.height*.5f:0;
  if(layer=="Frame"){if(charge){
   Surface(mesh,Expand(r,3,3),6,false,new Color(.015f,.025f,.033f),new Color(.015f,.025f,.033f));
   Surface(mesh,Expand(r,2,2),5,false,new Color(.04f,.35f,.43f),new Color(.23f,.72f,.78f));
   Surface(mesh,Expand(r,.7f,.7f),4.5f,false,new Color(.035f,.11f,.14f),new Color(.025f,.08f,.105f));
   Surface(mesh,r,4,false,new Color(.012f,.023f,.03f),new Color(.012f,.023f,.03f));
   Surface(mesh,Inset(r,1),3.5f,false,new Color(.08f,.36f,.42f),new Color(.14f,.48f,.53f));
   Surface(mesh,new Rect(r.x+8,r.yMax+4,r.width-16,1),0,false,new Color(.08f,.5f,.61f,.8f),new Color(.08f,.5f,.61f,.8f));
   var rail=new Rect(r.x+5,r.y-6,r.width-10,2);
   Surface(mesh,Expand(rail,1,1),1,false,new Color(.01f,.025f,.03f),new Color(.01f,.025f,.03f));
   Surface(mesh,rail,1,false,new Color(.02f,.28f,.36f),new Color(.12f,.64f,.75f));
   Surface(mesh,new Rect(rail.x+2,rail.y+1,rail.width*.73f,1),0,false,new Color(.21f,.85f,.92f,.8f),new Color(.21f,.85f,.92f,.8f));
  }else if(CyanBevel){
   Surface(mesh,r,0,false,new Color(.01f,.20f,.24f),new Color(.12f,.86f,.93f));
   Surface(mesh,Inset(r,.5f),0,false,new Color(.015f,.055f,.065f),new Color(.01f,.09f,.11f));
   Surface(mesh,Inset(r,1),0,false,new Color(.12f,.65f,.71f),new Color(.25f,.95f,1));
   Surface(mesh,Inset(r,1.5f),0,false,new Color(.01f,.04f,.045f),new Color(.02f,.12f,.14f));
  }else{
   Surface(mesh,r,RoundedEnds?r.height*.5f:0,RoundedEnds,new Color(.008f,.018f,.023f),new Color(.025f,.045f,.05f));
   Surface(mesh,Inset(r,1),RoundedEnds?(r.height-2)*.5f:0,RoundedEnds,new Color(.065f,.085f,.09f),new Color(.08f,.115f,.12f));
  }
  }
  if(layer=="Track")Surface(mesh,track,cut,RoundedEnds,HullNeon||CyanBevel?new Color(.018f,.035f,.039f):new Color(.12f,.16f,.18f),HullNeon||CyanBevel?new Color(.045f,.065f,.069f):new Color(.16f,.195f,.205f));
  if(Value>0){
   float end=track.x+track.width*Value;
   int layers=charge||RoundedEnds?6:3;
   if(layer=="Glow")for(int i=layers;i>=1;i--){float a=(HullNeon?.03f:charge?.019f:.014f)*(layers+1-i);var glow=Expand(track,i*.25f,i*(HullNeon?.7f:charge||RoundedEnds?.45f:.3f));var c=new Color(color.r,color.g,color.b,a);Surface(mesh,glow,cut+i*.2f,RoundedEnds,c,c,end);}
   var low=Color.Lerp(color,Gold?new Color(.65f,.3f,.03f):new Color(.02f,.58f,.69f),.18f);
   var high=Color.Lerp(color,Color.white,HullNeon?.65f:RoundedEnds?.48f:.24f);
   if(layer=="Fill")Surface(mesh,track,cut,RoundedEnds,low,high,end);
   var core=new Rect(track.x,track.y+track.height*.3f,track.width,track.height*.4f);
   var coreLow=HullNeon?Color.Lerp(color,Color.white,.65f):Gold?new Color(1,.9f,.5f,.22f):new Color(.61f,1,1,.22f);
   var coreHigh=HullNeon?Color.Lerp(color,Color.white,.9f):Gold?new Color(1,.97f,.7f,.3f):new Color(.72f,1,1,.3f);
   if(layer=="Core")Surface(mesh,core,RoundedEnds?core.height*.5f:0,RoundedEnds,coreLow,coreHigh,end);
  }
  if(layer=="Shield"&&ShieldValue>0){
   var shield=new Rect(track.x+track.width*Value,track.y,track.width*ShieldValue,track.height);
   Surface(mesh,shield,0,false,new Color(.69f,.74f,.77f),new Color(1,1,1));
   if(Value>0)Surface(mesh,new Rect(shield.x,shield.y,Mathf.Min(.65f,shield.width*.15f),shield.height),0,false,new Color(.06f,.09f,.10f),new Color(.09f,.12f,.13f));
  }
  if(layer=="Dividers"&&charge)for(int i=1;i<Divisions;i++){
   float x=track.x+track.width*i/Divisions;
   Surface(mesh,new Rect(x,track.y,1,track.height),0,false,new Color(.025f,.3f,.37f,.7f),new Color(.03f,.35f,.43f,.5f));
   Surface(mesh,new Rect(x+1,track.y,1,track.height),0,false,new Color(.48f,.94f,.98f,.22f),new Color(.48f,.94f,.98f,.3f));
  }
  if(layer=="Frame")CombatUiMeshPartition.RemoveInterior(mesh,CombatUiMeshPartition.Aperture(track,cut));
 }
 static Rect Inset(Rect r,float p)=>Expand(r,-p,-p);
 static Rect Expand(Rect r,float x,float y)=>new Rect(r.x-x,r.y-y,Mathf.Max(0,r.width+2*x),Mathf.Max(0,r.height+2*y));
 static void Surface(VertexHelper m,Rect r,float radius,bool round,Color bottom,Color top,float clip=float.PositiveInfinity){
  if(r.width<=0||r.height<=0||clip<=r.xMin)return;
  radius=Mathf.Clamp(radius,0,Mathf.Min(r.width,r.height)*.5f);
  var points=new List<Vector2>();
  if(round&&radius>0){
   var centers=new[]{new Vector2(r.xMax-radius,r.yMax-radius),new Vector2(r.xMin+radius,r.yMax-radius),new Vector2(r.xMin+radius,r.yMin+radius),new Vector2(r.xMax-radius,r.yMin+radius)};
   for(int corner=0;corner<4;corner++)for(int step=0;step<=5;step++){float angle=(corner*90+step*18)*Mathf.Deg2Rad;points.Add(centers[corner]+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius);}
  }else points.AddRange(new[]{new Vector2(r.xMax,r.yMax-radius),new Vector2(r.xMax-radius,r.yMax),new Vector2(r.xMin+radius,r.yMax),new Vector2(r.xMin,r.yMax-radius),new Vector2(r.xMin,r.yMin+radius),new Vector2(r.xMin+radius,r.yMin),new Vector2(r.xMax-radius,r.yMin),new Vector2(r.xMax,r.yMin+radius)});
  if(clip<r.xMax){var clipped=new List<Vector2>();var a=points[points.Count-1];foreach(var b in points){bool ai=a.x<=clip,bi=b.x<=clip;if(ai!=bi)clipped.Add(Vector2.Lerp(a,b,(clip-a.x)/(b.x-a.x)));if(bi)clipped.Add(b);a=b;}points=clipped;}
  if(points.Count<3)return;int n=m.currentVertCount;var center=Vector2.zero;foreach(var p in points)center+=p;center/=points.Count;
  m.AddVert(center,Color.Lerp(bottom,top,(center.y-r.y)/r.height),Vector2.zero);
  foreach(var p in points)m.AddVert(p,Color.Lerp(bottom,top,(p.y-r.y)/r.height),Vector2.zero);
  for(int i=0;i<points.Count;i++)m.AddTriangle(n,n+1+i,n+1+(i+1)%points.Count);
 }
}
}
