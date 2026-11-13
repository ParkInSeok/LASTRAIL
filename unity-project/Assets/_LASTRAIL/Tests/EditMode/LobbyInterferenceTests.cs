using System.Reflection;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Tests.EditMode
{
    public class LobbyInterferenceTests
    {
        [Test]
        public void InterferenceIsBriefSparseAndStopsWithReducedMotion()
        {
            var root=new GameObject("InterferenceTest",typeof(RectTransform));
            ((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);
            try
            {
                var motion=LobbyCabinMotion.Create(root.transform);
                var property=typeof(LobbyCabinMotion).GetProperty("InterferenceLevel",BindingFlags.Public|BindingFlags.Instance);
                Assert.That(property,Is.Not.Null,"A bounded interference envelope is required.");
                float run=0,maxRun=0;int bursts=0;bool active=false;
                for(int i=0;i<6000;i++)
                {
                    motion.Evaluate(.01f);float level=(float)property.GetValue(motion);
                    Assert.That(level,Is.InRange(0f,1f));
                    if(level>0){if(!active)bursts++;active=true;run+=.01f;maxRun=Mathf.Max(maxRun,run);}
                    else {active=false;run=0;}
                }
                Assert.That(bursts,Is.InRange(6,15));Assert.That(maxRun,Is.InRange(.1f,.2f));
                motion.ReducedMotion=true;motion.Evaluate(0);
                Assert.That((float)property.GetValue(motion),Is.Zero);
                Assert.That(motion.Art.GetComponent<RawImage>().material.GetFloat("_Interference"),Is.Zero);
            }finally {Object.DestroyImmediate(root);}
        }
    }
}
