using System.Collections.Generic;
using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public sealed class CombatHeaderPointerTests {
 [TestCase(1280)][TestCase(1600)]
 public void RaycastPointerClicksUseExistingCommandsOnceAfterRefreshBinding(int height){
  var prep=new Ce03RailPreparation(CombatRailPreparationTests.Golden(),"a",1);
  int saves=0;var driver=new CombatSceneDriver(()=>prep.CreateModel(1),()=>null,(s,r)=>{saves++;return r+1;},()=>true);driver.Open();
  var cameraObject=new GameObject("header-pointer-camera",typeof(Camera));var target=new RenderTexture(720,height,24);
  var go=new GameObject("header-pointer",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));var events=new GameObject("header-events",typeof(EventSystem));
  try{
   var camera=cameraObject.GetComponent<Camera>();camera.targetTexture=target;var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
   var view=go.AddComponent<ApprovedCombatView>();var art=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");view.Bind(driver,art);canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;view.BindOrRefresh(driver,art);view.BindOrRefresh(driver,art);Canvas.ForceUpdateCanvases();
   var root=go.transform.Find("ApprovedBattle");
   System.Action<string> click=name=>{view.Refresh();Canvas.ForceUpdateCanvases();camera.Render();var button=root.Find(name).GetComponent<Button>();var r=(RectTransform)button.transform;var position=RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(r.rect.center));var pointer=new PointerEventData(events.GetComponent<EventSystem>()){position=position,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();go.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);Assert.IsNotEmpty(hits,name);Assert.AreEqual(button, hits[0].gameObject.GetComponentInParent<Button>(),name);int before=saves;ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);Assert.AreEqual(before+1,saves,name+" commits once");view.Refresh();};
   click("UltimateAuto");Assert.IsTrue((bool)driver.HudState["ultimateAuto"]);click("UltimateAuto");Assert.IsFalse((bool)driver.HudState["ultimateAuto"]);
   foreach(int value in new[]{3,4,2}){click("Speed");Assert.AreEqual(value,(int)driver.HudState["speedHalfUnits"]);}
   click("Pause");string paused=driver.StateJson;driver.Advance(10000000,8);Assert.AreEqual(paused,driver.StateJson);
   var pauseOwner=go.GetComponent<CombatPauseController>();pauseOwner.BindNavigation(null,()=>Assert.Fail("Confirmation has not been accepted"));
   System.Array.Find(go.GetComponentsInChildren<Button>(),b=>b.name=="PauseLobby").onClick.Invoke();
   Canvas.ForceUpdateCanvases();camera.Render();var blockedRect=(RectTransform)root.Find("Speed");var blockedPointer=new PointerEventData(events.GetComponent<EventSystem>()){position=RectTransformUtility.WorldToScreenPoint(camera,blockedRect.TransformPoint(blockedRect.rect.center))};var blockedHits=new List<RaycastResult>();go.GetComponent<GraphicRaycaster>().Raycast(blockedPointer,blockedHits);
   Assert.IsNotEmpty(blockedHits);Assert.IsNotNull(blockedHits[0].gameObject.GetComponentInParent<CombatPauseView>(),"Confirmation must intercept input above underlying combat controls");
   var resume=go.GetComponentInChildren<CombatPauseView>().GetComponentsInChildren<Button>();var continueButton=System.Array.Find(resume,b=>b.name=="PauseContinue");Canvas.ForceUpdateCanvases();camera.Render();var resumeRect=(RectTransform)continueButton.transform;var resumePointer=new PointerEventData(events.GetComponent<EventSystem>()){position=RectTransformUtility.WorldToScreenPoint(camera,resumeRect.TransformPoint(resumeRect.rect.center)),button=PointerEventData.InputButton.Left};var resumeHits=new List<RaycastResult>();go.GetComponent<GraphicRaycaster>().Raycast(resumePointer,resumeHits);Assert.IsNotEmpty(resumeHits);Assert.AreEqual(continueButton,resumeHits[0].gameObject.GetComponentInParent<Button>());int resumeSaves=saves;ExecuteEvents.Execute(continueButton.gameObject,resumePointer,ExecuteEvents.pointerClickHandler);Assert.AreEqual(resumeSaves+1,saves);Assert.IsFalse((bool)driver.HudState["paused"]);
   Assert.AreEqual(3,root.GetComponentsInChildren<CombatTitleBarPress>().Length);Assert.IsFalse(driver.RecoveryRequired);
  }finally{Object.DestroyImmediate(go);Object.DestroyImmediate(events);Object.DestroyImmediate(cameraObject);target.Release();Object.DestroyImmediate(target);}
 }
}
