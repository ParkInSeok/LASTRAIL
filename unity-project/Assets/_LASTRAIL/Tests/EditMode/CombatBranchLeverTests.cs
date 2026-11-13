using System;
using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
public class CombatBranchLeverTests {
 [Test] public void GestureConfirmsOnceOnlyAndProgrammaticChangesNeverConfirm(){var type=typeof(ApprovedCombatView).Assembly.GetType("LastRail.Battle.CombatBranchLever");Assert.NotNull(type);var go=new GameObject("lever",typeof(RectTransform));try{var c=go.AddComponent(type);var slider=(Slider)c;int commits=0;type.GetField("Commit").SetValue(c,(Action)(()=>commits++));slider.value=1;Assert.AreEqual(0,commits);type.GetMethod("BeginGesture").Invoke(c,new object[]{7});slider.value=0;type.GetMethod("EndGesture").Invoke(c,new object[]{7,true});type.GetMethod("EndGesture").Invoke(c,new object[]{7,true});Assert.AreEqual(1,commits);type.GetMethod("BeginGesture").Invoke(c,new object[]{8});slider.value=1;type.GetMethod("EndGesture").Invoke(c,new object[]{8,false});Assert.AreEqual(1,commits);slider.SetValueWithoutNotify(0);Assert.AreEqual(1,commits);}finally{UnityEngine.Object.DestroyImmediate(go);}}
}
