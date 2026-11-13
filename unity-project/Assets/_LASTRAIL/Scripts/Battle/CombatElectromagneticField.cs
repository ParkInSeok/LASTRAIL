using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatElectromagneticField:MaskableGraphic {
 CombatElectromagneticState reader;JObject hud;RectTransform train;Func<string,Vector3?> actorWorld;
 readonly Dictionary<int,float> contacts=new Dictionary<int,float>();
 public JObject Frame{get;private set;}
 public bool Simplified;
 public void Bind(RectTransform trainLayer,JObject initial){train=trainLayer;reader=new CombatElectromagneticState(initial);raycastTarget=false;}
 public void Refresh(JObject state,Func<string,Vector3?> world){hud=state;actorWorld=world;Frame=reader.Read(state);var live=new HashSet<int>(((JArray)Frame["absorptions"]).Select(p=>(int)p["index"]));foreach(int id in contacts.Keys.Where(id=>!live.Contains(id)).ToArray())contacts.Remove(id);SetVerticesDirty();}
 Vector2 Point(Vector2 p)=>rectTransform.InverseTransformPoint(train.TransformPoint(p));
 static Vector2 Bezier(Vector2 a,Vector2 b,Vector2 c,Vector2 d,float t){float u=1-t;return u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;}
#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
 RectTransform comparisonHull;
 Rect comparisonCap;
 bool comparisonCellsVisible=true;
 internal void SetComparisonCellsVisible(bool visible){comparisonCellsVisible=visible;SetVerticesDirty();}
 readonly List<(Vector2 a,Vector2 b,Color tint)> cellEdges=new List<(Vector2,Vector2,Color)>();
 internal void BindComparisonHull(RectTransform hull,Rect cap){
  comparisonHull=hull;comparisonCap=cap;BuildCellEdges();SetVerticesDirty();
 }
 float CapHeight(float x){
  float offset=Mathf.Clamp01(1-Mathf.Min(x,comparisonCap.width-x)/comparisonCap.height);
  return comparisonCap.height*Mathf.Sqrt(1-offset*offset);
 }
 void BuildCellEdges(){
  cellEdges.Clear();
  // Source cell pitch is about 15 pixels across the 720-pixel native D dome.
  float w=comparisonCap.width*15/720, h=w*.72f, inset=w*.13f;
  var ceiling=new float[33];
  for(int i=0;i<ceiling.Length;i++)ceiling[i]=CapHeight(comparisonCap.width*i/32);
  bool Inside(Vector2 p){
   if(p.x<inset||p.x>comparisonCap.width-inset||p.y<inset)return false;
   // The actual cap is drawn in 32 straight strips. Leave space for the antialiased line fringe.
   float x=(p.x+(p.x<comparisonCap.width/2?-inset:inset))*32/comparisonCap.width;
   int i=Mathf.Clamp(Mathf.FloorToInt(x),0,31);
   return p.y<Mathf.Lerp(ceiling[i],ceiling[i+1],x-i)-inset;
  }
  Vector2 Joint(Vector2 p){
   // Give each shared junction one stable offset, as in the uneven cells of the painted reference.
   int col=Mathf.RoundToInt(p.x*2/w),row=Mathf.RoundToInt(p.y*4/h);
   float dx=Mathf.Sin(col*17.3f+row*4.7f)*w*.065f,dy=Mathf.Sin(col*3.1f+row*21.9f)*h*.085f;
   return p+new Vector2(dx,dy);
  }
  void Edge(Vector2 a,Vector2 b){
   a=Joint(a);b=Joint(b);
   bool first=Inside(a),last=Inside(b);
   if(!first&&!last)return;
   if(first!=last){
    Vector2 inside=first?a:b,outside=first?b:a;
    for(int i=0;i<8;i++){var mid=(inside+outside)*.5f;if(Inside(mid))inside=mid;else outside=mid;}
    if(first)b=inside;else a=inside;
   }
   float depth=Mathf.Clamp01((a.y+b.y)*.5f/comparisonCap.height);
   cellEdges.Add((comparisonCap.min+a,comparisonCap.min+b,new Color(.18f,.77f,.88f,Mathf.Lerp(.04f,.16f,depth))));
  }
  // Each cell owns three edges, so adjacent cells cannot double the brightness of a shared wall.
  for(int row=-1;row*h*.75f<comparisonCap.height+h;row++){
   for(float x=(row&1)*w*.5f-w;x<comparisonCap.width+w;x+=w){
    float y=row*h*.75f;
    var top=new Vector2(x,y+h*.5f);var right=new Vector2(x+w*.5f,y+h*.25f);
    Edge(new Vector2(x-w*.5f,y+h*.25f),top);Edge(top,right);Edge(right,new Vector2(right.x,y-h*.25f));
   }
  }
 }
 void DrawComparisonCells(VertexHelper mesh){
  if(!comparisonCellsVisible)return;
  var toField=rectTransform.worldToLocalMatrix*comparisonHull.localToWorldMatrix;
  foreach(var edge in cellEdges)Line(mesh,toField.MultiplyPoint3x4(edge.a),toField.MultiplyPoint3x4(edge.b),.36f,edge.tint);
 }
 Vector2 HullPoint(float t,bool inner){

  float x=comparisonCap.xMin+t*comparisonCap.width;
  float height=inner?0:CapHeight(x-comparisonCap.xMin);
  return rectTransform.InverseTransformPoint(comparisonHull.TransformPoint(new Vector2(x,comparisonCap.yMin+height)));
 }
#endif
 Vector2 Outer(float t){
#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
  if(comparisonHull!=null)return HullPoint(t,false);
#endif
  return Point(Bezier(new Vector2(120,-5),new Vector2(460,270),new Vector2(1140,190),new Vector2(1080,-160),t));
 }
 Vector2 Inner(float t){
#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
  if(comparisonHull!=null)return HullPoint(t,true);
#endif
  return Point(Bezier(new Vector2(340,-55),new Vector2(630,-65),new Vector2(1030,-110),new Vector2(1050,-125),t));
 }
 static void Line(VertexHelper m,Vector2 a,Vector2 b,float width,Color c){if((b-a).sqrMagnitude<.00001f||c.a<=0)return;var n=new Vector2(-(b-a).y,(b-a).x).normalized;int i=m.currentVertCount;var clear=c;clear.a=0;foreach(var p in new[]{a,b}){m.AddVert(p-n*(width*.5f+.65f),clear,Vector2.zero);m.AddVert(p-n*width*.5f,c,Vector2.zero);m.AddVert(p+n*width*.5f,c,Vector2.zero);m.AddVert(p+n*(width*.5f+.65f),clear,Vector2.zero);}for(int j=0;j<3;j++){m.AddTriangle(i+j,i+j+1,i+j+4);m.AddTriangle(i+j+1,i+j+5,i+j+4);}}
 Vector2 Flow(float t,float band,float time,int seed){float envelope=Mathf.Sin(t*Mathf.PI);float drift=Mathf.Sin(t*21-time*1.7f+seed)*.07f;var p=Vector2.Lerp(Outer(t),Inner(t),band+drift);return p+new Vector2(Mathf.Sin(t*117+seed*3),Mathf.Sin(t*157+seed*9+time*2))*3.2f*envelope;}
 static void Glow(VertexHelper m,Vector2 a,Vector2 b,float strength){Line(m,a,b,5,new Color(.05f,.65f,1,.07f*strength));Line(m,a,b,1.15f,new Color(.23f,.88f,1,.65f*strength));}
 static void Radial(VertexHelper m,Vector2 p,float radius,float alpha){int n=m.currentVertCount;m.AddVert(p,new Color(.85f,1,1,alpha),Vector2.zero);for(int i=0;i<24;i++){float a=i*Mathf.PI/12;m.AddVert(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,new Color(.06f,.7f,1,0),Vector2.zero);}for(int i=0;i<24;i++)m.AddTriangle(n,n+1+i,n+1+(i+1)%24);}
 float Contact(string source){var world=actorWorld(source);Vector2 origin=world.HasValue?(Vector2)rectTransform.InverseTransformPoint(world.Value):Point(new Vector2(1200,-130));float nearest=.85f,best=float.MaxValue;for(int i=16;i<=32;i++){float t=i/32f,dist=(Outer(t)-origin).sqrMagnitude;if(dist<best){best=dist;nearest=t;}}return nearest;}
 protected override void OnPopulateMesh(VertexHelper m){
  m.Clear();if(Frame==null||!(bool)Frame["active"])return;float time=(float)Frame["time"],issue=Mathf.Clamp01(1-(float)Frame["issueAge"]/.36f);bool shield=(int)Frame["shield"]>0;
  if(shield){
   for(int i=0;i<32;i++){float a=i/32f,b=(i+1)/32f;int start=m.currentVertCount;float edgeFade=Mathf.Clamp01((1-b)/.2f);
#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
   if(comparisonHull!=null)edgeFade=1;
#endif
   var top=new Color(.08f,.66f,.92f,(.075f+issue*.08f)*edgeFade);var low=new Color(.08f,.60f,.88f,(.022f+issue*.03f)*edgeFade);m.AddVert(Outer(a),top,Vector2.zero);m.AddVert(Outer(b),top,Vector2.zero);m.AddVert(Inner(b),low,Vector2.zero);m.AddVert(Inner(a),low,Vector2.zero);m.AddTriangle(start,start+1,start+2);m.AddTriangle(start,start+2,start+3);Glow(m,Outer(a),Outer(b),(.3f+issue*.5f)*edgeFade);}
#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
   if(comparisonHull!=null)DrawComparisonCells(m);else
#endif
   for(int flow=1;flow<(Simplified?3:7);flow++)for(int i=0;i<48;i++){float t=i/48f,next=(i+1)/48f;float band=flow/(Simplified?3f:7f);float phase=Mathf.Repeat(t*2.7f+flow*.37f-time*.08f,1);float fragment=Mathf.SmoothStep(0,1,phase/.1f)*Mathf.SmoothStep(0,1,(.64f-phase)/.15f);float endFade=Mathf.Clamp01((.96f-flow*.045f-t)/.09f)*Mathf.Clamp01((t-flow*.017f)/.08f);float strength=fragment*endFade;if(strength<=.001f)continue;float pulse=Mathf.Pow(.5f+.5f*Mathf.Sin(t*23-time*2+flow),3);var a=Flow(t,band,time,flow);var b=Flow(next,band,time,flow);Glow(m,a,b,(.25f+.75f*pulse+issue*.2f)*strength);if(!Simplified&&i%13==flow){var fork=Flow(next+.025f,Mathf.Clamp01(band+.10f),time,flow+5);Line(m,b,fork,.7f,new Color(.2f,.85f,1,.3f*strength));}}
   // Visual approach of an already reserved basic attack, not a new projectile
   // entity or an interception rule. Absorption below is receipt-driven.
   foreach(var actor in (JArray)hud["actors"]){if(!(bool)actor["enemy"]||(string)actor["attackTarget"]!=(string)((JArray)hud["actors"]).Single(a=>(bool)a["train"])["id"])continue;float p=(float)((double)hud["motionSeconds"]-reader.TimeAt((long)actor["attackDue"])+.14d)/.14f;if(p<0||p>=1)continue;var source=actorWorld((string)actor["id"]);if(!source.HasValue)continue;Vector2 from=rectTransform.InverseTransformPoint(source.Value),to=Outer(Contact((string)actor["id"]));var point=Vector2.Lerp(from,to,p);var direction=(to-from).normalized;Line(m,point-direction*12,point,1.4f,new Color(1,.68f,.28f,.9f));}
  }
  foreach(var pulse in (JArray)Frame["absorptions"]){int id=(int)pulse["index"];if(!contacts.TryGetValue(id,out float t)){t=Contact((string)pulse["source"]);contacts[id]=t;}float age=(float)pulse["age"],fade=Mathf.Clamp01(1-age/(float)CombatElectromagneticState.AbsorptionTailSeconds);var p=Outer(t);Radial(m,p,22+age*24,fade*.8f);Radial(m,p,6,fade);
   for(int ray=0;ray<8;ray++){float a=ray*Mathf.PI/4+id*.37f;var dir=new Vector2(Mathf.Cos(a),Mathf.Sin(a));Glow(m,p-dir*4,p+dir*(10+age*28),fade);}
   for(int i=-4;i<4;i++){float a=Mathf.Clamp01(t+i*.018f),b=Mathf.Clamp01(t+(i+1)*.018f);var wobble=new Vector2(0,Mathf.Sin(i*5+time*31)*3*fade);Glow(m,Outer(a)+wobble,Outer(b)-wobble,fade);}
   if((int)pulse["hpLoss"]>0&&age<.14f){var to=Point(new Vector2(350,-150));var head=Vector2.Lerp(p,to,age/.14f);Line(m,head-(to-p).normalized*16,head,1,new Color(1,.55f,.2f,fade));}
  }
 }
}
}
