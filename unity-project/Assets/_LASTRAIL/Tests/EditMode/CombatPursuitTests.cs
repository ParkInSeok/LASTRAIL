using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using LastRail.Battle;
public class CombatPursuitTests {
 [Test] public void RebuiltWheelDustHasIdenticalSavedTimeGeometry(){
  var a=new GameObject("wheel-contact",typeof(RectTransform));var b=new GameObject("rebuilt",typeof(RectTransform));
  try{var x=a.AddComponent<CombatWheelContact>();var y=b.AddComponent<CombatWheelContact>();x.Dust=y.Dust=true;var points=new[]{new Vector2(50,-230),new Vector2(80,-232),new Vector2(120,-234)};
   x.SetContacts(19.5f,130,points);y.SetContacts(19.5f,130,points);var method=typeof(CombatWheelContact).GetMethod("OnPopulateMesh",BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);
   using(var va=new VertexHelper())using(var vb=new VertexHelper()){method.Invoke(x,new object[]{va});method.Invoke(y,new object[]{vb});Assert.Greater(va.currentVertCount,0);Assert.AreEqual(va.currentVertCount,vb.currentVertCount);for(int i=0;i<va.currentVertCount;i++){var pa=new UIVertex();var pb=new UIVertex();va.PopulateUIVertex(ref pa,i);vb.PopulateUIVertex(ref pb,i);Assert.AreEqual(pa.position,pb.position);Assert.AreEqual(pa.color,pb.color);}}
  }finally{Object.DestroyImmediate(a);Object.DestroyImmediate(b);}
 }
 [Test] public void MovingRoadAndActorsShareTheSameContactSurface(){
  foreach(float t in new[]{0f,2.3f,14.999f,15f,15.001f,89f})for(int slot=0;slot<4;slot++)foreach(float progress in new[]{0f,.2f,.5f,1f}){
   var p=CombatPursuitLayout.Contact(slot,progress,t);
   Assert.AreEqual(CombatPursuitLayout.RoadY(p.x,t,slot%2),p.y-CombatExteriorLayout.ViewTop,.00003f);
  }
 }
 [Test] public void WorldWrapHasNoContactJumpAndRanksStaySeparated(){
  for(int slot=0;slot<4;slot++){
   var before=CombatPursuitLayout.Contact(slot,.8f,14.99999f);var after=CombatPursuitLayout.Contact(slot,.8f,15.00001f);
   Assert.Less(Vector2.Distance(before,after),.001f);
   Assert.AreEqual(CombatPursuitLayout.Contact(slot,.8f,0),CombatPursuitLayout.Contact(slot,.8f,15));
  }
  for(int lane=0;lane<2;lane++)Assert.Greater(CombatPursuitLayout.Contact(lane+2,1,3).x-CombatPursuitLayout.Contact(lane,1,3).x,100);
 }
}
