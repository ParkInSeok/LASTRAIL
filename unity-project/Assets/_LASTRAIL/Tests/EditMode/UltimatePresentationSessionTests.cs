using System;
using System.Collections.Generic;
using LastRail.Battle;
using NUnit.Framework;
namespace LastRail.Tests.EditMode
{
 // Presentation fixtures only, not native five-operator ultimate integration.
 public sealed class UltimatePresentationSessionTests
 {
  sealed class Lease:IDisposable { readonly Action release; public Lease(Action r){release=r;} public void Dispose(){release();} }
  sealed class Fixture
  {
   public bool Current=true,Standard=true,Summary=true,Eligible=true,CompleteSynchronously,VisibleSynchronously,ThrowOnPlay,ThrowOnRelease;
   public Action OnValidityRead, OnMark; public bool NullHold; public int Holds,Releases,Stops,Restores,Marks,ReadinessChecks; public bool RestoredCurrent; public string Played;
   public Action Visible,Ended,Skipped; public Action<Exception> Failed; public readonly List<Exception> Errors=new List<Exception>(); public object Session;
   public readonly List<string> Trace=new List<string>();
   public Fixture(CombatPresentationMode mode=CombatPresentationMode.Full)
   {
    var type=typeof(CombatPresentationPolicy).Assembly.GetType("LastRail.Presentation.Battle.UltimatePresentationSession");
    Assert.That(type,Is.Not.Null,"Accepted-action presentation session is not implemented.");
    Session=Activator.CreateInstance(type,new object[]{"presentation-fixture-action",7L,mode,
     (Func<bool>)(()=>{OnValidityRead?.Invoke();return Current;}),(Func<bool>)(()=>{ReadinessChecks++;return Standard;}),(Func<bool>)(()=>Summary),(Func<bool>)(()=>Eligible),
     (Action)(()=>{Marks++;OnMark?.Invoke();}),(Func<IDisposable>)(()=>{Holds++;if(NullHold)return null;return new Lease(()=>{Trace.Add("hold");Releases++;if(ThrowOnRelease)throw new InvalidOperationException("release fixture");});}),
     (Func<string,Action,Action,Action,Action<Exception>,IDisposable>)((kind,visible,ended,skipped,failed)=>{
      Played=kind;Visible=visible;Ended=ended;Skipped=skipped;Failed=failed;
      Trace.Add("begin");if(VisibleSynchronously)visible();if(CompleteSynchronously)ended();if(ThrowOnPlay)throw new InvalidOperationException("play fixture");Trace.Add("return");return new Lease(()=>{Trace.Add("stop");Stops++;});
     }),(Action<bool>)(valid=>{Trace.Add("restore");Restores++;RestoredCurrent=valid;}),(Action<Exception>)(error=>Errors.Add(error))});
   }
   public object Call(string name)=>Session.GetType().GetMethod(name).Invoke(Session,null);
   public string State=>(string)Session.GetType().GetProperty("State").GetValue(Session);
  }
  [Test] public void StandardOnlyAcquiresOwnedHoldAndWritesOnFirstVisibleFrame(){var f=new Fixture();f.Call("Start");Assert.That(f.Played,Is.EqualTo("Standard"));Assert.That(f.Holds,Is.EqualTo(1));Assert.That(f.Marks,Is.Zero);f.Visible();f.Visible();Assert.That(f.Marks,Is.EqualTo(1));f.Ended();Assert.That(f.Releases,Is.EqualTo(1));Assert.That(f.Stops,Is.EqualTo(1));}
  [TestCase(false,true,"Summary")][TestCase(false,false,"Minimal")]
  public void MissingAssetsFreezeFallbackWithoutHold(bool standard,bool summary,string expected){var f=new Fixture{Standard=standard,Summary=summary};f.Call("Start");Assert.That(f.Played,Is.EqualTo(expected));Assert.That(f.Holds,Is.Zero);f.Standard=true;f.Call("Start");Assert.That(f.Played,Is.EqualTo(expected));Assert.That(f.ReadinessChecks,Is.EqualTo(1));f.Visible();Assert.That(f.Marks,Is.Zero);}
  [Test] public void DailyRechecksCurrentEligibilityAtPresentationTurn(){var f=new Fixture(CombatPresentationMode.FirstUseFull);f.Eligible=false;f.Call("Start");Assert.That(f.Played,Is.EqualTo("Summary"));Assert.That(f.Holds,Is.Zero);f.Visible();Assert.That(f.Marks,Is.Zero);}
  [Test] public void OffUsesSummaryEvenWhenStandardReady(){var f=new Fixture(CombatPresentationMode.Simplified);f.Call("Start");Assert.That(f.Played,Is.EqualTo("Summary"));Assert.That(f.Holds,Is.Zero);}
  [Test] public void SkipFinishRaceUsesOneCleanupAndIgnoresLateFrames(){var f=new Fixture();f.Call("Start");f.Skipped();f.Ended();f.Visible();f.Call("Dispose");Assert.That(f.Releases,Is.EqualTo(1));Assert.That(f.Stops,Is.EqualTo(1));Assert.That(f.Restores,Is.EqualTo(1));Assert.That(f.Marks,Is.Zero);}
  [Test] public void CancelBeforeStartDoesNotPlayOrAcquire(){var f=new Fixture{Current=false};Assert.That(f.Call("Start"),Is.EqualTo(false));Assert.That(f.Played,Is.Null);Assert.That(f.Holds,Is.Zero);}
  [Test] public void StaleGenerationCannotWriteOrRestoreOldBattlefield(){var f=new Fixture();f.Call("Start");f.Current=false;f.Visible();f.Ended();Assert.That(f.Marks,Is.Zero);Assert.That(f.RestoredCurrent,Is.False);Assert.That(f.Releases,Is.EqualTo(1));}
  [Test] public void FailureBeforeFirstFrameDoesNotConsumeDailyView(){var f=new Fixture();f.Call("Start");f.Failed(new InvalidOperationException("decode fixture"));f.Visible();Assert.That(f.Marks,Is.Zero);Assert.That(f.Releases,Is.EqualTo(1));Assert.That(f.Errors.Count,Is.EqualTo(1));}
  [Test] public void FailureAfterFirstFrameKeepsDailyView(){var f=new Fixture();f.Call("Start");f.Visible();f.Failed(new InvalidOperationException("decode fixture"));Assert.That(f.Marks,Is.EqualTo(1));Assert.That(f.Releases,Is.EqualTo(1));}
  [Test] public void ReentrantCompletionDisposesReturnedPlaybackOnce(){var f=new Fixture{CompleteSynchronously=true,VisibleSynchronously=true};f.Call("Start");f.Call("Dispose");Assert.That(f.Stops,Is.EqualTo(1));Assert.That(f.Releases,Is.EqualTo(1));Assert.That(f.Marks,Is.EqualTo(1));Assert.That(f.Restores,Is.EqualTo(1));}
  [Test] public void PlaybackThrowAndReleaseThrowStillRestoreAndReportBoth(){var f=new Fixture{ThrowOnPlay=true,ThrowOnRelease=true};f.Call("Start");Assert.That(f.Restores,Is.EqualTo(1));Assert.That(f.Releases,Is.EqualTo(1));Assert.That(f.Errors.Count,Is.EqualTo(2));}
  [Test] public void TimeoutUsesExplicitSingleCleanupWithoutInventedDuration(){var f=new Fixture();f.Call("Start");f.Call("Timeout");f.Call("Timeout");f.Ended();Assert.That(f.Releases,Is.EqualTo(1));Assert.That(f.Stops,Is.EqualTo(1));Assert.That(f.Restores,Is.EqualTo(1));}
  [Test] public void RepeatedStartAfterFinishCannotReplayAcceptedAction(){var f=new Fixture();f.Call("Start");f.Ended();Assert.That(f.Call("Start"),Is.EqualTo(false));Assert.That(f.Holds,Is.EqualTo(1));Assert.That(f.State,Is.EqualTo("Disposed"));}
  [Test] public void CurrentPredicateDisposalCannotWriteLateDailyRecord(){var f=new Fixture();f.Call("Start");f.OnValidityRead=()=>{f.OnValidityRead=null;f.Call("Dispose");};f.Visible();Assert.That(f.Marks,Is.Zero);Assert.That(f.Releases,Is.EqualTo(1));}
  [Test] public void ReentrantFirstVisibleRecordCannotDuplicateWrite(){var f=new Fixture();f.Call("Start");f.OnMark=()=>f.Visible();f.Visible();Assert.That(f.Marks,Is.EqualTo(1));f.Ended();Assert.That(f.Releases,Is.EqualTo(1));}
  [Test] public void MissingSafeTickLeaseFallsBackWithoutStandardPlayback(){var f=new Fixture{NullHold=true};f.Call("Start");Assert.That(f.Played,Is.EqualTo("Summary"));Assert.That(f.Releases,Is.Zero);f.Visible();Assert.That(f.Marks,Is.Zero);}
  [Test] public void DisposeBeforeStartPreventsPlayback(){var f=new Fixture();f.Call("Dispose");Assert.That(f.Call("Start"),Is.EqualTo(false));Assert.That(f.Played,Is.Null);}
  [Test] public void CompletionNotifiesOnceAfterSynchronousReturnedLeaseAndRestoration()
  {
   var f=new Fixture{CompleteSynchronously=true};int completed=0;
   var signal=f.Session.GetType().GetEvent("Completed");
   Assert.That(signal,Is.Not.Null,"Sequence needs an owned completion subscription.");
   signal.AddEventHandler(f.Session,(Action)(()=>{completed++;Assert.That(f.Stops,Is.EqualTo(1));Assert.That(f.Restores,Is.EqualTo(1));}));
   f.Call("Start");f.Call("Dispose");Assert.That(completed,Is.EqualTo(1));Assert.That(f.Errors,Is.Empty);
  }
  [Test] public void CancelBeforeStartAlsoNotifiesSequenceExactlyOnce()
  {
   var f=new Fixture();int completed=0;
   var signal=f.Session.GetType().GetEvent("Completed");
   Assert.That(signal,Is.Not.Null);
   signal.AddEventHandler(f.Session,(Action)(()=>completed++));
   f.Call("Cancel");f.Call("Start");Assert.That(completed,Is.EqualTo(1));
  }
  [Test] public void SynchronousEndStopsReturnedPlayerBeforeRestoreAndHoldRelease()
  {
   var f=new Fixture{CompleteSynchronously=true};f.Call("Start");
   Assert.That(f.Trace,Is.EqualTo(new[]{"begin","return","stop","restore","hold"}));
  }
 }
}
