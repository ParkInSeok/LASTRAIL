using System;
using System.Reflection;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class MissionRootViewLifecycleTests
    {
        [TestCase(typeof(MissionShellScreenRootBehaviour),"primaryButton","PrimaryActionRequested")]
        [TestCase(typeof(CodexArchiveScreenRootBehaviour),"missionSelectButton","MissionSelectRequested")]
        [TestCase(typeof(RewardStoryScreenRootBehaviour),"primaryButton","PrimaryActionRequested")]
        public void HiddenRenderDoesNotRebindAndReenableDispatchesOnce(Type type,string buttonField,string eventName)
        {
            var root=new GameObject("RootLifecycle",typeof(RectTransform));
            try{
                var view=(MonoBehaviour)root.AddComponent(type);var control=new GameObject("Action",typeof(RectTransform),typeof(Button));control.transform.SetParent(root.transform,false);var button=control.GetComponent<Button>();
                type.GetField(buttonField,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(view,button);int calls=0,other=0;
                type.GetEvent(eventName).AddEventHandler(view,(Action)(()=>calls++));button.onClick.AddListener(()=>other++);
                var render=type.GetMethod("Render");object state=State(type);render.Invoke(view,new[]{state});button.onClick.Invoke();Assert.That(calls,Is.EqualTo(1));
                root.SetActive(false);render.Invoke(view,new[]{state});render.Invoke(view,new[]{state});button.onClick.Invoke();Assert.That(calls,Is.EqualTo(1),"Hidden Render must not restore the view listener.");Assert.That(other,Is.EqualTo(2),"Only the view-owned listener is detached.");
                root.SetActive(true);render.Invoke(view,new[]{state});render.Invoke(view,new[]{state});button.onClick.Invoke();Assert.That(calls,Is.EqualTo(2),"Rebinding must keep one listener.");Assert.That(other,Is.EqualTo(3));
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        static object State(Type type)
        {
            if(type==typeof(CodexArchiveScreenRootBehaviour))return new CodexArchiveScreenViewState("Archive",null,"fixture",Array.Empty<CodexArchiveScreenEntryViewState>(),0,true,true,true);
            if(type==typeof(RewardStoryScreenRootBehaviour))return new RewardStoryScreenViewState(MissionShellSurface.RewardReview,"Reward",null,null,null,"Confirm",null,null,null,null,null,0,null,true,false,null,null,null,null,false,false,false,false);
            return new MissionShellScreenViewState(MissionShellSurface.MissionSelect,"Mission",null,null,null,"Confirm",null,true,null,null,null,null,null,false,false,false);
        }
    }
}
