using System;
using System.Reflection;
using LastRail.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Tests.EditMode {
 public sealed class UiPrimitiveLifecycleTests {
  [TestCase(typeof(LastRailUiButton))]
  [TestCase(typeof(LastRailUiInputField))]
  [TestCase(typeof(LastRailUiToggle))]
  [TestCase(typeof(LastRailUiSegmentedControl))]
  public void DisabledBindCannotDispatchAndReenableBindsOnce(Type type) {
   var root=new GameObject("Primitive",typeof(RectTransform));
   try {
    root.SetActive(false);var view=(MonoBehaviour)root.AddComponent(type);
    var child=new GameObject("Control",typeof(RectTransform));child.transform.SetParent(root.transform,false);
    int calls=0,foreign=0;Action fire;string field;object control;
    if(type==typeof(LastRailUiInputField)) {
     var input=child.AddComponent<TMP_InputField>();control=input;field="inputField";
     ((LastRailUiInputField)view).Submitted+=_=>calls++;input.onSubmit.AddListener(_=>foreign++);fire=()=>input.onSubmit.Invoke("typed");
    } else if(type==typeof(LastRailUiToggle)) {
     var toggle=child.AddComponent<Toggle>();control=toggle;field="toggle";
     ((LastRailUiToggle)view).ValueChanged+=_=>calls++;toggle.onValueChanged.AddListener(_=>foreign++);fire=()=>toggle.onValueChanged.Invoke(true);
    } else {
     var button=child.AddComponent<Button>();button.onClick.AddListener(()=>foreign++);fire=()=>button.onClick.Invoke();
     if(type==typeof(LastRailUiButton)){control=button;field="button";((LastRailUiButton)view).Clicked+=()=>calls++;}
     else {control=new[]{button};field="segmentButtons";((LastRailUiSegmentedControl)view).SegmentSelected+=_=>calls++;}
    }
    type.GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(view,control);
    var bind=type.GetMethod("BindInteractionCallbacks");root.SetActive(true);bind.Invoke(view,null);fire();Assert.That(calls,Is.EqualTo(1));
    view.enabled=false;bind.Invoke(view,null);fire();Assert.That(calls,Is.EqualTo(1));Assert.That(foreign,Is.EqualTo(2));
    view.enabled=true;bind.Invoke(view,null);bind.Invoke(view,null);fire();Assert.That(calls,Is.EqualTo(2));Assert.That(foreign,Is.EqualTo(3));
   } finally { UnityEngine.Object.DestroyImmediate(root); }
  }
 }
}
