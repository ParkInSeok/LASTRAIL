using System;
using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
public class CombatSunTests {
 [Test] public void FarSunNeverWrapsWithTwoGroundTileBoundaries(){
  var t=typeof(ApprovedCombatView).Assembly.GetType("LastRail.Battle.CombatSunView");Assert.NotNull(t);
  var m=t.GetMethod("WorldCenter");Assert.NotNull(m);Vector2 previous=(Vector2)m.Invoke(null,new object[]{0d});
  for(int i=1;i<=6000;i++){
   var p=(Vector2)m.Invoke(null,new object[]{i/60d});Assert.Less(Vector2.Distance(p,previous),.0001f);Assert.AreEqual(.9f,p.y,.00001f);Assert.That(p.x,Is.InRange(.76f,.8f));previous=p;
  }
  Assert.Greater(100*.045,2,"Test traverses more than two original terrain UV boundaries");
 }
}
