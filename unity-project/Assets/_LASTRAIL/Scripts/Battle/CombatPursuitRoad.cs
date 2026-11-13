using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
// Two thin, projected pursuit surfaces. UV collars are verified in capture, not assumed seamless.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatPursuitRoad:MaskableGraphic {
 public Texture2D Texture;public Rect TextureRegion=new Rect(0,0,1,1),SoilRegion=new Rect(0,.30f,1,.44f);float seconds;
 public override Texture mainTexture=>Texture!=null?Texture:Texture2D.whiteTexture;
 public void SetTime(float value){seconds=value;SetVerticesDirty();}
 void Vertex(VertexHelper mesh,float x,float offset,int lane,float u,float v,float alpha,bool floor=false){
  var rect=rectTransform.rect;var region=floor?SoilRegion:TextureRegion;
  mesh.AddVert(new Vector2(rect.xMin+x,rect.yMax-CombatPursuitLayout.RoadY(x,seconds,lane)-offset),floor?new Color(.70f,.69f,.66f,alpha):new Color(.87f,.84f,.79f,alpha),new Vector2(region.x+u*region.width,region.y+v*region.height));
 }
 protected override void OnPopulateMesh(VertexHelper mesh){
  mesh.Clear();float phase=CombatPursuitLayout.Phase(seconds,CombatPursuitLayout.RoadSpeed);
  for(int layer=-1;layer<2;layer++)for(int module=-1;module<=1;module++){
   bool floor=layer<0;int lane=floor?1:layer;
   const int columns=48;const float collar=24;float left=module*960-phase-collar;
   for(int x=0;x<columns;x++){
    float a=x/(float)columns,b=(x+1)/(float)columns;float xa=left+a*1008,xb=left+b*1008;
    if(xb<0||xa>720)continue;
    float aa=Mathf.Min(1,Mathf.Min(a,1-a)*1008/collar),ab=Mathf.Min(1,Mathf.Min(b,1-b)*1008/collar);
    for(int row=0;row<3;row++){
     float[] y=floor?new[]{-65f,-35,60,90}:new[]{-14f,-9,9,14},v={1,.82f,.18f,0},opacity=floor?new[]{0f,1,1,1}:new[]{0f,1,1,0};int i=mesh.currentVertCount;
     Vertex(mesh,xa,y[row],lane,a,v[row],aa*opacity[row],floor);Vertex(mesh,xb,y[row],lane,b,v[row],ab*opacity[row],floor);Vertex(mesh,xb,y[row+1],lane,b,v[row+1],ab*opacity[row+1],floor);Vertex(mesh,xa,y[row+1],lane,a,v[row+1],aa*opacity[row+1],floor);mesh.AddTriangle(i,i+1,i+2);mesh.AddTriangle(i,i+2,i+3);
    }
   }
  }
 }
}
}
