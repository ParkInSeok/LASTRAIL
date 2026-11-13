using System;
using System.Reflection;
using System.Threading.Tasks;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyBackInputTests
{
    GameObject root;
    LobbyDestinationPages pages;
    int closed;
    [SetUp] public void Open()
    {
        root=new GameObject("BackTest",typeof(RectTransform));
        ((RectTransform)root.transform).sizeDelta=new Vector2(1600,900);
        pages=LobbyDestinationPages.Create(root.transform,TMP_Settings.defaultFontAsset,()=>closed++);
        pages.Open(LobbyPage.Operators,true);
    }
    [TearDown] public void Dispose(){UnityEngine.Object.DestroyImmediate(root);}
    object Call(string name,params object[] args)
    {
        var method=typeof(LobbyDestinationPages).GetMethod(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
        Assert.NotNull(method,"Existing page owner must route "+name);
        return method.Invoke(pages,args);
    }
    [Test] public void BackClosesNoticeOnlyAndSecondBackReturnsFromPage()
    {
        Call("Notice","test","information");
        Call("RequestBack");
        Assert.IsTrue(pages.IsOpen);Assert.AreEqual(0,closed);
        Call("RequestBack");
        Assert.IsFalse(pages.IsOpen);Assert.AreEqual(1,closed);
    }
    [Test] public void BackCancelsConfirmationWithoutSubmittingOrClosingParent()
    {
        int submitted=0;
        Call("CoreConfirm","confirm","cost",(Func<bool>)(()=>true),(Func<Task>)(()=>{submitted++;return Task.CompletedTask;}));
        Call("RequestBack");
        Assert.AreEqual(0,submitted);Assert.IsTrue(pages.IsOpen);Assert.AreEqual(0,closed);
        Assert.AreEqual(LobbyPage.Operators,pages.CurrentPage);
    }
    [Test] public void NoticeOverConfirmationReturnsToConfirmationBeforePage()
    {
        Call("CoreConfirm","confirm","cost",(Func<bool>)(()=>true),(Func<Task>)(()=>Task.CompletedTask));
        Call("Notice","details","information");
        Call("RequestBack");
        Assert.IsTrue(pages.IsOpen);
        Call("RequestBack");
        Assert.IsTrue(pages.IsOpen,"Second back must cancel confirmation, not close its page");
        Call("RequestBack");Assert.IsFalse(pages.IsOpen);
    }
    [Test] public void ModalBlocksUnderlyingNavigationAndReleasesOnlyItsScope()
    {
        var navigation=pages.GetComponentsInChildren<Button>(true);
        Call("Notice","test","information");
        foreach(var button in navigation)if(button.gameObject.activeInHierarchy)Assert.IsFalse(button.IsInteractable(),button.name);
        Call("RequestBack");
        Assert.IsTrue(pages.IsOpen);
        Assert.IsTrue(Array.Exists(navigation,button=>button!=null&&button.gameObject.activeInHierarchy&&button.IsInteractable()));
    }
    [Test] public void HeldEscapeConsumesOneLayerAndEditingConsumesItsOwnPress()
    {
        var key=new LastRail.Presentation.ScreenBackKey();int requests=0;
        Event Input(EventType type)=>new Event{type=type,keyCode=KeyCode.Escape};
        key.Handle(Input(EventType.KeyDown),()=>requests++,false);
        key.Handle(Input(EventType.KeyDown),()=>requests++,false);
        Assert.AreEqual(1,requests);
        key.Handle(Input(EventType.KeyUp),()=>requests++,false);
        key.Handle(Input(EventType.KeyDown),()=>requests++,true);
        key.Handle(Input(EventType.KeyDown),()=>requests++,false);
        Assert.AreEqual(1,requests,"Keyboard dismissal must not close a game layer on repeat");
        key.Handle(Input(EventType.KeyUp),()=>requests++,false);
        key.Handle(Input(EventType.KeyDown),()=>requests++,false);
        Assert.AreEqual(2,requests);
        key.ResetIfReleased(true);
        key.Handle(Input(EventType.KeyDown),()=>requests++,false);
        Assert.AreEqual(2,requests,"Reactivating while physically held must not repeat");
        key.ResetIfReleased(false);
        key.Handle(Input(EventType.KeyDown),()=>requests++,false);
        Assert.AreEqual(3,requests,"A release while inactive must not swallow the next press");
    }
}
