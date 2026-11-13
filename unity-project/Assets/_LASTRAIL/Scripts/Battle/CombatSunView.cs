using System;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
// One far-world celestial source, never repeated with terrain UV or texture wrap.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatSunView:MaskableGraphic {
 Rect viewport;double seconds;
 public static Vector2 WorldCenter(double time)=>new Vector2(.79f-.015f*(1-(float)Math.Exp(-Math.Max(0,time)/120)),.9f);
 public void SetProjection(double time,Rect farViewport){seconds=time;viewport=farViewport;SetVerticesDirty();}
 protected override void OnPopulateMesh(VertexHelper mesh){
  mesh.Clear();if(viewport.width<=0||viewport.height<=0)return;
  var bounds=GetPixelAdjustedRect();var world=WorldCenter(seconds);
  var center=new Vector2(bounds.x+(world.x-viewport.x)/viewport.width*bounds.width,bounds.y+(world.y-viewport.y)/viewport.height*bounds.height);
  var radius=new Vector2(.011f/viewport.width*bounds.width,.019f/viewport.height*bounds.height);
  if(center.x+radius.x*4.8f<bounds.xMin||center.x-radius.x*4.8f>bounds.xMax||center.y+radius.y*4.8f<bounds.yMin||center.y-radius.y*4.8f>bounds.yMax)return;
  float[] rings={0,.65f,1,1.3f,2,3.2f,4.8f};
  Color[] tint={new Color(1,.99f,.92f,.98f),new Color(1,.97f,.84f,.97f),new Color(1,.9f,.66f,.88f),new Color(1,.78f,.42f,.26f),new Color(1,.7f,.3f,.085f),new Color(1,.65f,.3f,.024f),new Color(1,.65f,.3f,0)};
  for(int j=0;j<rings.Length;j++)for(int i=0;i<64;i++){float angle=i*Mathf.PI*2/64;mesh.AddVert(center+new Vector2(Mathf.Cos(angle)*radius.x,Mathf.Sin(angle)*radius.y)*rings[j],tint[j],Vector2.zero);}
  for(int j=0;j<rings.Length-1;j++)for(int i=0;i<64;i++){int a=j*64+i,b=j*64+(i+1)%64,c=(j+1)*64+i,d=(j+1)*64+(i+1)%64;mesh.AddTriangle(a,c,d);mesh.AddTriangle(a,d,b);}
 }
}
}
