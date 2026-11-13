using LastRail.Presentation.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class ReinaImpactPlaybackTests {
 static JObject Impact(int pulse)=>new JObject{["commandId"]="cast",["pulseIndex"]=pulse,["combatSeconds"]=1.4+.8*pulse};
 [Test] public void JitteredReceiptAndLateDisplayDoNotMoveTheNextDeadline(){
  var p=new ReinaImpactPlayback();
  Assert.That(p.Enqueue(Impact(0),10).Due,Is.EqualTo(10.3).Within(1e-8));
  Assert.IsNull(p.Take(10.29));Assert.NotNull(p.Take(10.44));
  Assert.That(p.Enqueue(Impact(1),10.91).Due,Is.EqualTo(11.1).Within(1e-8));
  Assert.NotNull(p.Take(11.2));
  Assert.That(p.Enqueue(Impact(2),11.65).Due,Is.EqualTo(11.9).Within(1e-8));
  Assert.That(p.Enqueue(Impact(3),12.54).Due,Is.EqualTo(12.7).Within(1e-8));
 }
 [Test] public void PauseClearAndCancellationDoNotReplay(){
  var p=new ReinaImpactPlayback();p.Enqueue(Impact(0),1);p.Enqueue(Impact(1),1);
  for(int i=0;i<20;i++)Assert.IsNull(p.Take(1));
  p.Cancel("cast");Assert.IsNull(p.Take(100));Assert.AreEqual(0,p.PendingCount);
  p.Clear();Assert.That(p.Enqueue(Impact(0),100).Due,Is.EqualTo(100.3).Within(1e-8));
 }
 [Test] public void LateReceiptDoesNotSilentlyShiftRemainingPhase(){
  var p=new ReinaImpactPlayback();p.Enqueue(Impact(0),1);p.Take(1.3);
  Assert.That(p.Enqueue(Impact(1),3).Due,Is.EqualTo(2.1).Within(1e-8));Assert.NotNull(p.Take(3));
  Assert.That(p.Enqueue(Impact(2),3).Due,Is.EqualTo(2.9).Within(1e-8));
 }
 [Test] public void InvalidAndOutOfOrderNativeTimingIsRejected(){
  var p=new ReinaImpactPlayback();Assert.Throws<System.ArgumentException>(()=>p.Enqueue(new JObject{["commandId"]="cast"},0));
  p.Enqueue(Impact(1),0);Assert.Throws<System.ArgumentException>(()=>p.Enqueue(Impact(0),0));
 }
}
