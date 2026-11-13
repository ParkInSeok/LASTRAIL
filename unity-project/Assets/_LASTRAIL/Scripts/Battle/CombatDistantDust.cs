using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
// Direction's five-puff ground dust recipe; simulation clock only, no history replay.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatDistantDust:MaskableGraphic {
 float clock,born;bool initialized;Vector2 ground;
 public void SetState(float simulationSeconds,Vector2 localGround){if(!initialized){born=simulationSeconds;initialized=true;}clock=simulationSeconds;ground=localGround;SetVerticesDirty();}
 protected override void OnPopulateMesh(VertexHelper vh){
  vh.Clear();float elapsed=clock-born;if(elapsed<=0)return;
  for(int p=0;p<5;p++){
   float start=p*.14f;if(elapsed<start)continue;float age=Mathf.Repeat(elapsed-start,.7f);float t=age/.7f;float radius=4+t*7;
   var center=ground+new Vector2(-age*12,age*4);var tint=new Color(.667f,.573f,.447f,Mathf.Sin(t*Mathf.PI)*.12f);int first=vh.currentVertCount;vh.AddVert(center,tint,Vector2.zero);
   for(int i=0;i<=24;i++){float a=i*Mathf.PI*2/24;vh.AddVert(center+new Vector2(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius*.5f),new Color(tint.r,tint.g,tint.b,0),Vector2.zero);}for(int i=0;i<24;i++)vh.AddTriangle(first,first+i+1,first+i+2);
  }
 }
}
}
