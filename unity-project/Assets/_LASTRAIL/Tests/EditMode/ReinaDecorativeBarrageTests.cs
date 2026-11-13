using System;
using System.IO;
using System.Linq;
using LastRail.Presentation.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class ReinaDecorativeBarrageTests {
 static JArray Source()=> (JArray)JObject.Parse(File.ReadAllText("D:/Project/LASTRAIL/QA/SideViewReuse-20261023/reina-battle-hud-v01/d-console/barrage-random/events.json"))["events"];
 [Test] public void SixtyTwoIndependentLaunchesStayWithinFiveAirborneAcrossSeedsAndExtremes(){
  var source=Source();string before=source.ToString();
  for(int seed=-2;seed<100;seed++){
   var rng=new Random(seed);Func<double> next=seed==-2?()=>0:seed==-1?()=>.999999:rng.NextDouble;
   var rounds=new ReinaDecorativeBarrageSchedule(source,next).Rounds;
   Assert.AreEqual(62,rounds.Count);Assert.AreEqual(62,rounds.Select(r=>r.Id).Distinct().Count());Assert.AreEqual(62,rounds.Select(r=>r.Start).Distinct().Count());
   foreach(var r in rounds){Assert.Greater(r.Fall,0);Assert.LessOrEqual(r.Impact,7.400001);Assert.Less(r.EntryY,r.ControlY);Assert.Less(r.ControlY,r.Y);Assert.LessOrEqual(rounds.Count(x=>x.Start<=r.Start&&x.Impact>r.Start),5);}
   if(seed>=0)Assert.Greater(rounds.Select(r=>r.EntryX).Distinct().Count(),40);
  }
  Assert.AreEqual(before,source.ToString(),"Source artwork metadata must remain immutable.");
 }
 [Test] public void InvalidCountAndRandomValuesAreRejected(){
  Assert.Throws<ArgumentException>(()=>new ReinaDecorativeBarrageSchedule(new JArray(),()=>.5));
  Assert.Throws<ArgumentException>(()=>new ReinaDecorativeBarrageSchedule(Source(),()=>double.NaN));
 }
 [Test] public void PooledVisualsShowEveryRoundWithoutReplayingClearedCommands(){
  var root=new UnityEngine.GameObject("decorative-test",typeof(UnityEngine.RectTransform));
  var rect=(UnityEngine.RectTransform)root.transform;rect.sizeDelta=new UnityEngine.Vector2(1280,720);
  using(var view=new ReinaDecorativeBarrageView(rect,_=>new UnityEngine.Vector2(700,350))){
   var feedback=new JArray(new JObject{["commandId"]="first",["nextPulse"]=0});
   view.Refresh(feedback,0,false);
   for(int i=1;i<=150;i++)view.Refresh(feedback,i*.1,false);
   Assert.AreEqual(62,view.GeneratedCount);Assert.AreEqual(62,view.ShownCount);Assert.AreEqual(62,view.LandedCount);Assert.LessOrEqual(view.MaximumAirborne,5);Assert.False(view.Active);
   feedback[0]["commandId"]="cancelled";view.Refresh(feedback,16,false);view.Refresh(feedback,16.1,false);
   int shown=view.ShownCount;view.Refresh(feedback,16.1,false);Assert.AreEqual(shown,view.ShownCount,"Stopped presentation clock must stay stopped.");
   view.Cancel("cancelled");view.Refresh(feedback,17,false);Assert.False(view.Active);Assert.AreEqual(shown,view.ShownCount);
   feedback[0]["commandId"]="disabled";view.Refresh(feedback,18,false);view.Clear();view.Refresh(feedback,19,false);Assert.False(view.Active);
  }
  UnityEngine.Object.DestroyImmediate(root);
 }
}
