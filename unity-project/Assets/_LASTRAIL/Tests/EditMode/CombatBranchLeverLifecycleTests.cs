using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class CombatBranchLeverLifecycleTests
{
    [Test] public void SubmitAfterDisableCannotCommitAndReenableCommitsOnce()
    {
        var host=new GameObject("BranchLever",typeof(RectTransform));
        var events=new GameObject("Events",typeof(EventSystem));
        try {
            var lever=host.AddComponent<CombatBranchLever>();int calls=0;lever.Commit=()=>calls++;
            var submit=new BaseEventData(events.GetComponent<EventSystem>());
            lever.OnSubmit(submit);Assert.AreEqual(1,calls);
            lever.BeginGesture(7);host.SetActive(false);lever.OnSubmit(submit);lever.EndGesture(7,true);Assert.AreEqual(1,calls);
            host.SetActive(true);lever.OnSubmit(submit);Assert.AreEqual(2,calls);
            lever.enabled=false;lever.OnSubmit(submit);Assert.AreEqual(2,calls);
        } finally {Object.DestroyImmediate(host);Object.DestroyImmediate(events);}
    }
}
