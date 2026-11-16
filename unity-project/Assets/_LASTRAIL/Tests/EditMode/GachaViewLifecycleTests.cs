using System.Reflection;
using LastRail.Gacha;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class GachaViewLifecycleTests
{
    [Test] public void HiddenBindingCannotDispatchAndReenableAddsOnlyOneListener()
    {
        var root=new GameObject("GachaLifecycle",typeof(RectTransform));
        try {
            var view=root.AddComponent<GachaScreenRootBehaviour>();var button=new GameObject("Pull",typeof(RectTransform),typeof(Button)).GetComponent<Button>();button.transform.SetParent(root.transform,false);
            typeof(GachaScreenRootBehaviour).GetField("onePullButton",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(view,button);
            int calls=0;view.OnePullRequested+=()=>calls++;view.BindInteractionCallbacks();view.BindInteractionCallbacks();button.onClick.Invoke();Assert.AreEqual(1,calls);
            root.SetActive(false);view.BindInteractionCallbacks();button.onClick.Invoke();Assert.AreEqual(1,calls);
            root.SetActive(true);view.BindInteractionCallbacks();button.onClick.Invoke();Assert.AreEqual(2,calls);
        } finally {Object.DestroyImmediate(root);}
    }
}
