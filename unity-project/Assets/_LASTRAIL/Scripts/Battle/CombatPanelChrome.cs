using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
// Native beveled metal housing. It never handles input or changes combat state.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatPanelChrome:MaskableGraphic {
 public bool Leader,Compact;
 public string State{get;private set;}="CHARGING";
 public bool Enabled{get;private set;}
 float light;
 public void SetState(string state,bool enabled,float glow){glow=Mathf.Clamp01(glow);if(State==state&&Enabled==enabled&&Mathf.Approximately(light,glow))return;State=state;Enabled=enabled;light=glow;SetVerticesDirty();}
 void Face(VertexHelper m,Rect r,float bevel,Color bottom,Color top){
  if(r.width<=0||r.height<=0)return;float b=Mathf.Min(bevel,Mathf.Min(r.width,r.height)*.5f);
  var p=new[]{new Vector2(r.x+b,r.y),new Vector2(r.xMax-b,r.y),new Vector2(r.xMax,r.y+b),new Vector2(r.xMax,r.yMax-b),new Vector2(r.xMax-b,r.yMax),new Vector2(r.x+b,r.yMax),new Vector2(r.x,r.yMax-b),new Vector2(r.x,r.y+b)};
  int n=m.currentVertCount;m.AddVert(r.center,Color.Lerp(bottom,top,.5f),Vector2.zero);foreach(var v in p)m.AddVert(v,Color.Lerp(bottom,top,(v.y-r.y)/r.height),Vector2.zero);for(int i=0;i<8;i++)m.AddTriangle(n,n+1+i,n+1+(i+1)%8);
 }
 static Rect Inset(Rect r,float n)=>new Rect(r.x+n,r.y+n,r.width-2*n,r.height-2*n);
 protected override void OnPopulateMesh(VertexHelper m){
  m.Clear();var r=GetPixelAdjustedRect();float b=Compact?5:7;
  Face(m,r,b,new Color(.015f,.018f,.02f),new Color(.03f,.038f,.041f));
  Face(m,Inset(r,1),b,new Color(.1f,.12f,.13f),new Color(.54f,.51f,.43f));
  Face(m,Inset(r,2.5f),b-1,new Color(.24f,.25f,.23f),new Color(.095f,.12f,.13f));
  Face(m,Inset(r,4),b-2,new Color(.027f,.045f,.055f),new Color(.06f,.083f,.092f));
  var tint=State=="CASTING"?new Color(.53f,.83f,.91f):new Color(.22f,.8f,.94f);
  float glow=State=="CASTING"?.55f:light;var rail=Color.Lerp(new Color(.12f,.22f,.25f),tint,glow);
  Face(m,new Rect(r.x+7,r.y+5,r.width-14,1.4f),0,rail,rail);
  if(Leader){Face(m,new Rect(r.x+8,r.yMax-6,r.width-16,2),0,rail,rail);Face(m,new Rect(r.x+3,r.y+18,1.5f,r.height-36),0,rail,rail);Face(m,new Rect(r.xMax-4.5f,r.y+18,1.5f,r.height-36),0,rail,rail);}
  foreach(float x in new[]{r.x+4,r.xMax-6})foreach(float y in new[]{r.y+5,r.yMax-7}){Face(m,new Rect(x,y,2,2),.4f,new Color(.12f,.1f,.07f),new Color(.65f,.52f,.33f));}
 }
}
}
