using System;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
// Cosmetic read-only condition layer. Coordinates are normalized, top-left
// relative to the exterior skin; no hit scheduling, RNG consumption or HP writes.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatTrainDamageView:MaskableGraphic {
 public int Stage{get;private set;}
 float condition=1,clock;
 Vector2[] sites={new Vector2(.43f,.52f),new Vector2(.63f,.56f),new Vector2(.86f,.59f)};
 public void SetDamageSites(Vector2[] normalizedSites){
  if(normalizedSites==null||normalizedSites.Length>8)throw new ArgumentException();
  foreach(var p in normalizedSites)if(float.IsNaN(p.x)||float.IsNaN(p.y)||p.x<0||p.x>1||p.y<0||p.y>1)throw new ArgumentException();
  sites=(Vector2[])normalizedSites.Clone();SetVerticesDirty();
 }
 public void SetCondition(int hp,int maxHp,float simulationSeconds){
  condition=maxHp>0?Mathf.Clamp01(hp/(float)maxHp):1;
  Stage=condition<=0?4:condition<=.2f?3:condition<=.4f?2:condition<=.7f?1:0;
  clock=float.IsNaN(simulationSeconds)||float.IsInfinity(simulationSeconds)?0:Mathf.Max(0,simulationSeconds);SetVerticesDirty();
 }
 Vector2 Point(Vector2 p){var r=rectTransform.rect;return new Vector2(r.xMin+p.x*r.width,r.yMax-p.y*r.height);}
 static float Noise(float x)=>Mathf.Repeat(Mathf.Sin(x*12.9898f)*43758.5453f,1);
 void Cloud(VertexHelper vh,Vector2 center,Vector2 radius,Color tint,float seed){
  const int segments=18;int first=vh.currentVertCount;vh.AddVert(center,tint,Vector2.zero);
  for(int ring=1;ring<=3;ring++)for(int i=0;i<segments;i++){
   float a=i*Mathf.PI*2/segments;float uneven=.88f+.22f*Noise(i+seed);float scale=ring/3f;
   var c=tint;c.a*=ring==1?.77f:ring==2?.27f:0;
   vh.AddVert(center+new Vector2(Mathf.Cos(a)*radius.x,Mathf.Sin(a)*radius.y)*scale*uneven,c,Vector2.zero);
  }
  for(int i=0;i<segments;i++){int next=(i+1)%segments;vh.AddTriangle(first,first+1+i,first+1+next);for(int ring=0;ring<2;ring++){int a=first+1+ring*segments+i,b=first+1+ring*segments+next,c=a+segments,d=b+segments;vh.AddTriangle(a,c,b);vh.AddTriangle(b,c,d);}}
 }
 static void Stroke(VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint){
  var d=b-a;if(d.sqrMagnitude<.001f)return;var normal=new Vector2(-d.y,d.x).normalized*width*.5f;int i=vh.currentVertCount;
  vh.AddVert(a-normal,tint,Vector2.zero);vh.AddVert(a+normal,tint,Vector2.zero);vh.AddVert(b+normal,tint,Vector2.zero);vh.AddVert(b-normal,tint,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
 }
 protected override void OnPopulateMesh(VertexHelper vh){
  vh.Clear();if(Stage==0)return;float scale=rectTransform.rect.width/625f;float severity=1-condition;
  for(int n=0;n<sites.Length;n++){
   var origin=Point(sites[n]);float strength=Mathf.Clamp01(.42f+(severity-.3f)*1.4f-n*.025f);
   if(strength<=0)continue;
   Cloud(vh,origin,new Vector2(27+strength*13,13+strength*9)*scale,new Color(.025f,.019f,.015f,.70f*strength),n+5);
   for(int j=0;j<7;j++){
    var a=origin+new Vector2((Noise(n*20+j)-.5f)*33,(Noise(n*20+j+31)-.5f)*19)*scale;
    var b=a+new Vector2(4+Noise(j+8)*9,-3-Noise(j+9)*7)*scale;
    Stroke(vh,a,b,2.2f*scale,new Color(.025f,.023f,.02f,strength*.85f));
    Stroke(vh,a+Vector2.down*.8f*scale,b+Vector2.down*.8f*scale,.9f*scale,new Color(.66f,.60f,.48f,strength*.8f));
   }
   if(Stage<2)continue;
   float smoke=Mathf.Clamp01(.45f+(severity-.6f)*1.8f);
   for(int p=0;p<13;p++){
    float age=Mathf.Repeat(clock*.31f+p/13f+n*.23f,1);float fade=Mathf.Sin(age*Mathf.PI);
    var center=origin+new Vector2(-age*(65+n*8)+Mathf.Sin(age*9+n)*7,-4+age*(135+smoke*24))*scale;
    float radius=(5+age*(17+smoke*11))*scale;
    var smokeTint=Color.Lerp(new Color(.64f,.65f,.64f),new Color(.17f,.18f,.19f),Mathf.InverseLerp(.65f,.95f,severity));smokeTint.a=fade*smoke*.7f;
    Cloud(vh,center,new Vector2(radius*1.4f,radius),smokeTint,p+n*17);
   }
   float burst=Mathf.Repeat(clock*.73f+n*.37f,1);
   if(burst<.22f)for(int j=0;j<4;j++){
    float age=burst/.22f;var a=origin+new Vector2(-age*(12+j*7),age*(10+j*3)-age*age*23)*scale;
    Stroke(vh,a,a+new Vector2(2+j,-1)*scale,.8f*scale,new Color(1,.58f,.18f,(1-age)*.8f));
   }
   if(Stage<3||n==0)continue;
   float flicker=.65f+.18f*Mathf.Sin(clock*17+n)+.13f*Mathf.Sin(clock*29+n*3);
   Cloud(vh,origin,new Vector2(15,7)*scale,new Color(1,.30f,.025f,.22f*flicker),n);
   for(int flame=0;flame<3;flame++){
    var start=origin+new Vector2((flame-1)*4,0)*scale;float length=(13+flame*4)*flicker*scale;
    for(int k=0;k<5;k++){
     float a=k/5f,b=(k+1)/5f;
     var from=start+new Vector2(-a*7+Mathf.Sin(clock*11+a*5+flame)*a*3,a*length);
     var to=start+new Vector2(-b*7+Mathf.Sin(clock*11+b*5+flame)*b*3,b*length);
     Stroke(vh,from,to,(1-a)*3.8f*scale,new Color(1,.35f+.4f*(1-a),.06f,.8f*(1-a)));
    }
   }
  }
 }
}
}
