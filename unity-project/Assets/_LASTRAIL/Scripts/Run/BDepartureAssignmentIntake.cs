using System;
using System.Threading;
using System.Threading.Tasks;
namespace LastRail.Run {
 // Owns only original immutable source publication and its asynchronous lifetime.
 internal sealed class BDepartureAssignmentIntake:IDisposable {
  readonly string account,run;readonly Func<BRouteBinding> route;CancellationTokenSource pending;long generation;bool disposed,invalidating;BDepartureAssignment current;BDepartureSourceBundle bundle;
  internal BDepartureAssignmentIntake(string accountId,string runId,Func<BRouteBinding> originalRoute){account=accountId;run=runId;route=originalRoute;}
  internal BDepartureAssignment Current{get{Alive();return current;}}
  internal BDepartureSourceBundle Bundle{get{Alive();return bundle;}}
  internal void RequireRoute(BRouteBinding next){Alive();current?.RequireSameDeparture(next);bundle?.RequireSameDeparture(next);}
  internal async Task<bool> ReadAsync(Func<CancellationToken,Task<BDepartureAssignment>> read,CancellationToken cancellation){Alive();if(read==null)throw new ArgumentNullException(nameof(read));return await ReadSourceAsync(async ct=>(await read(ct),null),cancellation);}
  internal async Task<bool> ReadBundleAsync(Func<CancellationToken,Task<BDepartureSourceBundle>> read,CancellationToken cancellation){Alive();if(read==null)throw new ArgumentNullException(nameof(read));return await ReadSourceAsync(async ct=>{var value=await read(ct);return(value?.Assignment,value);},cancellation);}
  async Task<bool> ReadSourceAsync(Func<CancellationToken,Task<(BDepartureAssignment assignment,BDepartureSourceBundle bundle)>> read,CancellationToken cancellation){
   Invalidate();var ticket=generation;var owned=CancellationTokenSource.CreateLinkedTokenSource(cancellation);pending=owned;
   try{var incoming=await read(owned.Token);if(disposed||ticket!=generation)return false;owned.Token.ThrowIfCancellationRequested();var assignment=incoming.assignment;
    if(assignment==null||assignment.AccountId!=account||assignment.RunId!=run)throw new ArgumentException("b_assignment_intake_identity_conflict");
    var binding=route();if(binding!=null){assignment.RequireSameDeparture(binding);incoming.bundle?.RequireSameDeparture(binding);}
    if(current!=null&&current.PacketHash!=assignment.PacketHash)throw new ArgumentException("b_assignment_frozen_source_changed");
    if(bundle!=null&&incoming.bundle!=null){bundle.RequireOriginalSource(incoming.bundle);var old=bundle.Capture();var next=incoming.bundle.Capture();long prior=(long)old["mapRevision"],revision=(long)next["mapRevision"];
     if(revision<prior||revision==prior&&bundle.PacketHash!=incoming.bundle.PacketHash)throw new ArgumentException("b_bundle_map_source_conflict");}
    owned.Token.ThrowIfCancellationRequested();current=assignment;if(incoming.bundle!=null)bundle=incoming.bundle;return true;
   }catch(OperationCanceledException)when(disposed||ticket!=generation){return false;}finally{if(ReferenceEquals(pending,owned))pending=null;owned.Dispose();}
  }
  internal void Invalidate(){Alive();generation=checked(generation+1);var old=pending;pending=null;invalidating=true;try{old?.Cancel();}finally{invalidating=false;}}
  void Alive(){if(disposed)throw new ObjectDisposedException(nameof(BDepartureAssignmentIntake));if(invalidating)throw new InvalidOperationException("b_assignment_intake_invalidating");}
  public void Dispose(){if(disposed)return;try{Invalidate();}finally{disposed=true;current=null;bundle=null;}}
 }
}
