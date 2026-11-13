using System;
using System.Linq;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class DepartureInputTests
{
    [Test] public void ControllerIntentRejectsReplacedScreenAndChangedSelection()
    {
        var root=new GameObject("ControllerIntent",typeof(RectTransform));
        try{
            var deck=root.AddComponent<LobbyCommandDeck>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            void Set(string field,object value)=>typeof(LobbyCommandDeck).GetField(field,flags).SetValue(deck,value);
            var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            var view=DeparturePreparationView.Create(root.transform,font,null,null,null,null,null,null,null,null,null,null,null);
            Set("preparationView",view);Set("departureDialog",true);Set("departureOperation","opn_01_last_departure");
            Func<bool> Capture()=>(Func<bool>)typeof(LobbyCommandDeck).GetMethod("CaptureDepartureIntent",flags).Invoke(deck,null);
            var risk=Capture();Assert.IsTrue(risk());Set("departureRisk",1);Assert.IsFalse(risk());
            var operation=Capture();Set("departureOperation","changed");Assert.IsFalse(operation());
            var previousScreen=Capture();Set("preparationView",null);Assert.IsFalse(previousScreen());Set("preparationView",view);
            var closed=Capture();Set("departureDialog",false);Assert.IsFalse(closed());
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    [Test] public void DisabledPressCannotBecomeDepartureWhenAssetsFinish()
    {
        var root=new GameObject("DepartureInput",typeof(RectTransform));var events=new GameObject("Events",typeof(EventSystem));int calls=0;
        try{
            var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            var view=DeparturePreparationView.Create(root.transform,font,null,null,null,null,null,null,null,null,()=>calls++,null,null);
            var button=root.GetComponentsInChildren<Button>().Single(x=>x.name=="ConfirmDeparture");
            var position=((RectTransform)button.transform).anchoredPosition;var input=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,pointerId=3};
            void Render(bool ready)=>view.Render("작전","I","",null,"",true,true,ready,true,true,"출격");
            Render(false);ExecuteEvents.Execute(button.gameObject,input,ExecuteEvents.pointerDownHandler);
            Render(true);Assert.AreEqual(0,calls);ExecuteEvents.Execute(button.gameObject,input,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,input,ExecuteEvents.pointerClickHandler);
            Assert.AreEqual(0,calls,"A press started while blocked must not be queued until assets become ready.");
            Assert.AreEqual(position,((RectTransform)button.transform).anchoredPosition);
            ExecuteEvents.Execute(button.gameObject,input,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(button.gameObject,input,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,input,ExecuteEvents.pointerClickHandler);Assert.AreEqual(1,calls);
            ExecuteEvents.Execute(button.gameObject,input,ExecuteEvents.pointerClickHandler);Assert.AreEqual(1,calls,"One press is consumed once.");
        }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(events);}
    }
    [Test] public void SelectionChangesAndViewDisableDiscardHeldInput()
    {
        var root=new GameObject("DepartureInput",typeof(RectTransform));var events=new GameObject("Events",typeof(EventSystem));int calls=0,generation=0;
        try{
            var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
            var view=DeparturePreparationView.Create(root.transform,font,null,null,null,null,null,null,null,null,()=>calls++,null,null);
            var bind=typeof(DeparturePreparationView).GetMethod("BindDepartureIntent");Assert.IsNotNull(bind);
            Func<Func<bool>> capture=()=>{int captured=generation;return ()=>captured==generation;};bind.Invoke(view,new object[]{capture});
            var button=root.GetComponentsInChildren<Button>().Single(x=>x.name=="ConfirmDeparture");var input=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,pointerId=1};
            view.Render("작전","I","",null,"",true,true,true,true,true,"출격");
            ExecuteEvents.Execute(button.gameObject,input,ExecuteEvents.pointerDownHandler);generation++;ExecuteEvents.Execute(button.gameObject,input,ExecuteEvents.pointerClickHandler);Assert.AreEqual(0,calls);
            ExecuteEvents.Execute(button.gameObject,input,ExecuteEvents.pointerDownHandler);view.gameObject.SetActive(false);view.gameObject.SetActive(true);ExecuteEvents.Execute(button.gameObject,input,ExecuteEvents.pointerClickHandler);Assert.AreEqual(0,calls);
            ExecuteEvents.Execute(button.gameObject,input,ExecuteEvents.pointerDownHandler);
            view.Render("작전","I","",null,"",true,true,false,true,true,"출격");view.Render("작전","I","",null,"",true,true,true,true,true,"출격");
            ExecuteEvents.Execute(button.gameObject,input,ExecuteEvents.pointerClickHandler);Assert.AreEqual(0,calls,"A transient blocker cancels the original press even if it clears before release.");
            ExecuteEvents.Execute(button.gameObject,input,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(button.gameObject,input,ExecuteEvents.pointerClickHandler);Assert.AreEqual(1,calls);
            ExecuteEvents.Execute(button.gameObject,new BaseEventData(events.GetComponent<EventSystem>()),ExecuteEvents.submitHandler);Assert.AreEqual(2,calls,"A fresh keyboard/controller submit still works.");
        }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(events);}
    }
}
