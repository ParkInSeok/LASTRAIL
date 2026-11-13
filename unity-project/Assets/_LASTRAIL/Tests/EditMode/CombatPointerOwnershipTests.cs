using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using LastRail.Battle;
public class CombatPointerOwnershipTests {
 [TestCase(typeof(CombatPointerSlider))][TestCase(typeof(CombatBranchLever))]
 public void EditorLayoutRebuildDoesNotSubmitUserIntent(System.Type type){var go=new GameObject("display-only-slider",typeof(RectTransform));try{var slider=(Slider)go.AddComponent(type);int changes=0;slider.onValueChanged.AddListener(_=>changes++);slider.Rebuild(CanvasUpdate.Prelayout);Assert.AreEqual(0,changes,"A layout rebuild is not a user command.");}finally{Object.DestroyImmediate(go);}}
 [Test] public void OsCanceledTouchDoesNotCommitButtonOrBranch(){
  var screen=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>();
  var go=new GameObject("button",typeof(RectTransform));var leverGo=new GameObject("lever",typeof(RectTransform));var events=new GameObject("events",typeof(EventSystem));
  try{
   var b=go.AddComponent<CombatInputButton>();var lever=leverGo.AddComponent<CombatBranchLever>();int count=0;b.onClick.AddListener(()=>count++);lever.Commit=()=>count++;
   var e=new UnityEngine.InputSystem.UI.ExtendedPointerEventData(events.GetComponent<EventSystem>()){pointerId=1,touchId=19,device=screen,position=Vector2.zero,button=PointerEventData.InputButton.Left};
   UnityEngine.InputSystem.InputSystem.QueueStateEvent(screen,new UnityEngine.InputSystem.LowLevel.TouchState{touchId=19,phase=UnityEngine.InputSystem.TouchPhase.Began});UnityEngine.InputSystem.InputSystem.Update();
   b.OnPointerDown(e);lever.BeginGesture(1);
   UnityEngine.InputSystem.InputSystem.QueueStateEvent(screen,new UnityEngine.InputSystem.LowLevel.TouchState{touchId=19,phase=UnityEngine.InputSystem.TouchPhase.Canceled});UnityEngine.InputSystem.InputSystem.Update();
   b.OnPointerUp(e);b.OnPointerClick(e);lever.OnPointerUp(e);Assert.AreEqual(0,count);
  }finally{Object.DestroyImmediate(go);Object.DestroyImmediate(leverGo);Object.DestroyImmediate(events);UnityEngine.InputSystem.InputSystem.RemoveDevice(screen);}
 }
 [Test] public void DragIsOwnedAndIndependentButtonStillWorks(){
  var go=new GameObject("slider",typeof(RectTransform));var other=new GameObject("button",typeof(RectTransform));var events=new GameObject("events",typeof(EventSystem));
  try{
   ((RectTransform)go.transform).sizeDelta=new Vector2(100,100);
   var handle=new GameObject("handle",typeof(RectTransform));handle.transform.SetParent(go.transform,false);
   var slider=go.AddComponent<CombatPointerSlider>();slider.handleRect=(RectTransform)handle.transform;
   var b=other.AddComponent<CombatInputButton>();int count=0;b.onClick.AddListener(()=>count++);
   var one=new PointerEventData(events.GetComponent<EventSystem>()){pointerId=1,position=new Vector2(-40,0),button=PointerEventData.InputButton.Left};
   var two=new PointerEventData(events.GetComponent<EventSystem>()){pointerId=2,position=new Vector2(40,0),button=PointerEventData.InputButton.Left};
   slider.OnPointerDown(one);float before=slider.value;slider.OnPointerDown(two);slider.OnDrag(two);Assert.AreEqual(before,slider.value);
   b.OnPointerDown(two);b.OnPointerUp(two);b.OnPointerClick(two);Assert.AreEqual(1,count);
   one.position=new Vector2(30,0);slider.OnDrag(one);Assert.Greater(slider.value,before);
   float accepted=slider.value;slider.OnCancel(one);one.position=new Vector2(-30,0);slider.OnDrag(one);Assert.AreEqual(accepted,slider.value);
  }finally{Object.DestroyImmediate(go);Object.DestroyImmediate(other);Object.DestroyImmediate(events);}
 }
 [Test] public void ModalAndFocusCancellationRequireNewDown(){
  var go=new GameObject("button",typeof(RectTransform),typeof(CanvasGroup));var events=new GameObject("events",typeof(EventSystem));
  try{var b=go.AddComponent<CombatInputButton>();int count=0;b.onClick.AddListener(()=>count++);var e=new PointerEventData(events.GetComponent<EventSystem>()){pointerId=1,button=PointerEventData.InputButton.Left};
   b.OnPointerDown(e);go.GetComponent<CanvasGroup>().interactable=false;go.GetComponent<CanvasGroup>().interactable=true;b.OnPointerUp(e);b.OnPointerClick(e);Assert.AreEqual(0,count);
   b.OnPointerDown(e);go.SendMessage("OnApplicationFocus",false);go.SendMessage("OnApplicationFocus",true);b.OnPointerUp(e);b.OnPointerClick(e);Assert.AreEqual(0,count);
   b.OnPointerDown(e);b.OnPointerUp(e);b.OnPointerClick(e);Assert.AreEqual(1,count);
  }finally{Object.DestroyImmediate(go);Object.DestroyImmediate(events);}
 }
 [Test] public void SecondPointerCannotStealBranchGesture(){
  var go=new GameObject("lever",typeof(RectTransform));
  try{var lever=go.AddComponent<CombatBranchLever>();int count=0;lever.Commit=()=>count++;
   lever.BeginGesture(1);lever.BeginGesture(2);lever.EndGesture(2,true);Assert.AreEqual(0,count);
   lever.EndGesture(1,true);Assert.AreEqual(1,count);
  }finally{Object.DestroyImmediate(go);}
 }
 [Test] public void ButtonOwnsFirstPointerAndExitCannotRearm(){
  var type=typeof(CombatBranchLever).Assembly.GetType("LastRail.Battle.CombatInputButton");Assert.NotNull(type);
  var go=new GameObject("button",typeof(RectTransform));var events=new GameObject("events",typeof(EventSystem));
  try{var b=(Button)go.AddComponent(type);int count=0;b.onClick.AddListener(()=>count++);
   var one=new PointerEventData(events.GetComponent<EventSystem>()){pointerId=1,button=PointerEventData.InputButton.Left};
   var two=new PointerEventData(events.GetComponent<EventSystem>()){pointerId=2,button=PointerEventData.InputButton.Left};
   b.OnPointerDown(one);b.OnPointerDown(two);b.OnPointerUp(two);b.OnPointerClick(two);Assert.AreEqual(0,count);
   b.OnPointerUp(one);b.OnPointerClick(one);Assert.AreEqual(1,count);
   b.OnPointerDown(one);b.OnPointerExit(one);b.OnPointerEnter(one);b.OnPointerUp(one);b.OnPointerClick(one);Assert.AreEqual(1,count);
   b.OnPointerDown(one);go.SetActive(false);go.SetActive(true);b.OnPointerUp(one);b.OnPointerClick(one);Assert.AreEqual(1,count);
  }finally{Object.DestroyImmediate(go);Object.DestroyImmediate(events);}
 }
}
