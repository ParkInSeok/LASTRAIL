using System;
using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class CombatCommandFeedbackTests {
 [Test] public void PressCancelAndDisabledDoNotSubmitOrChangeBattle(){
  var type=typeof(ApprovedCombatView).Assembly.GetType("LastRail.Battle.CombatCommandPress");Assert.NotNull(type);
  var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();
  var go=new GameObject("Feedback",typeof(RectTransform),typeof(Canvas));var ev=new GameObject("Events",typeof(EventSystem));
  try{
   go.AddComponent<ApprovedCombatView>().Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));
   var root=go.transform.Find("ApprovedBattle/Command0");Assert.NotNull(root);
   var feedback=root.GetComponent(type);Assert.NotNull(feedback);
   var button=root.GetComponent<Button>();button.interactable=true;
   var before=driver.StateJson;var pointer=new PointerEventData(ev.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,pointerId=1};
   ((IPointerDownHandler)feedback).OnPointerDown(pointer);
   Assert.IsTrue((bool)type.GetProperty("Pressed").GetValue(feedback));Assert.AreEqual(before,driver.StateJson);
   ((IPointerExitHandler)feedback).OnPointerExit(pointer);Assert.IsFalse((bool)type.GetProperty("Pressed").GetValue(feedback));
   ((IPointerUpHandler)feedback).OnPointerUp(pointer);Assert.AreEqual(before,driver.StateJson);
   button.interactable=false;((IPointerDownHandler)feedback).OnPointerDown(pointer);Assert.IsFalse((bool)type.GetProperty("Pressed").GetValue(feedback));Assert.AreEqual(before,driver.StateJson);
  }finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(ev);}
 }
}
