using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatWheelContact:MaskableGraphic {
 Vector2[] wheels=System.Array.Empty<Vector2>();float clock,speed;
 public bool Dust;
 public void SetContacts(float seconds,float groundSpeed,Vector2[] contacts){clock=seconds;speed=groundSpeed;wheels=contacts;SetVerticesDirty();}
 static void Puff(VertexHelper mesh,Vector2 point,Vector2 size,Color tint){
  int first=mesh.currentVertCount;mesh.AddVert(point,tint,Vector2.zero);
  for(int i=0;i<=16;i++){float a=i*Mathf.PI*2/16;mesh.AddVert(point+new Vector2(Mathf.Cos(a)*size.x,Mathf.Sin(a)*size.y),new Color(tint.r,tint.g,tint.b,0),Vector2.zero);}
  for(int i=0;i<16;i++)mesh.AddTriangle(first,first+i+1,first+i+2);
 }
 protected override void OnPopulateMesh(VertexHelper mesh){
  mesh.Clear();if(Dust&&speed<=.001f)return;for(int i=0;i<wheels.Length;i++){
   if(!Dust){Puff(mesh,wheels[i]+new Vector2(-2,-.6f),new Vector2(8,2),new Color(.035f,.027f,.022f,.5f));continue;}
   // Absolute saved simulation time: rebuilding the view preserves every plume phase.
   if(i%2!=0)continue;
   for(int p=0;p<4;p++){
    float age=Mathf.Repeat(clock+i*.083f-p*.15f,.72f);float t=age/.72f;
    var center=wheels[i]+new Vector2(-age*speed*.68f,2+age*8);
    Puff(mesh,center,new Vector2(3+t*17,1.3f+t*4),new Color(.69f,.58f,.44f,Mathf.Sin(t*Mathf.PI)*.14f));
   }
  }
 }
}
}
