using System.Reflection;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using UnityEngine;
namespace LastRail.Tests.EditMode
{
    public class RandomHologramTests
    {
        [Test]
        public void SuccessiveBurstsUseDifferentSitesAndFourToNineSecondIntervals()
        {
            var root=new GameObject("RandomHologramTest",typeof(RectTransform));
            ((RectTransform)root.transform).sizeDelta=new Vector2(720,1280);
            try {
                var motion=LobbyCabinMotion.Create(root.transform);
                var site=typeof(LobbyCabinMotion).GetProperty("InterferenceSite");
                Assert.That(site,Is.Not.Null,"A chosen interference site is required.");
                var visited=new System.Collections.Generic.HashSet<int>();
                int previous=-1,bursts=0;float lastStart=-1;bool active=false;
                for(int i=0;i<12000;i++) {
                    motion.Evaluate(.01f);
                    if(motion.InterferenceLevel>0 && !active) {
                        int current=(int)site.GetValue(motion);Assert.That(current,Is.InRange(0,10));
                        Assert.That(current,Is.Not.EqualTo(previous));
                        if(lastStart>=0)Assert.That(motion.Clock-lastStart,Is.InRange(3.98f,9.02f));
                        visited.Add(current);previous=current;lastStart=motion.Clock;bursts++;
                    }
                    active=motion.InterferenceLevel>0;
                }
                Assert.That(bursts,Is.GreaterThanOrEqualTo(12));
                Assert.That(visited.Count,Is.EqualTo(11),"All six nodes and five roads must get a turn, not repeatedly favor one node.");
                motion.ReducedMotion=true;float paused=motion.Clock;motion.Evaluate(20);
                Assert.That(motion.InterferenceLevel,Is.Zero);Assert.That(motion.Clock,Is.EqualTo(paused));
            } finally {Object.DestroyImmediate(root);}
        }
    }
}
