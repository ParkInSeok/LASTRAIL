using System.Threading;
using System.Threading.Tasks;
namespace LastRail.Auth {
 internal static class GameApiBorrowedTaskAwait {
  // Cancels this consumer's await without disposing another owner's borrowed transport.
  internal static async Task<T> Wait<T>(Task<T> pending,CancellationToken stop){if(!stop.CanBeCanceled)return await pending;var cancelled=new TaskCompletionSource<bool>();using(stop.Register(()=>cancelled.TrySetResult(true))){if(await Task.WhenAny(pending,cancelled.Task)!=pending){_ = pending.ContinueWith(t=>{var observed=t.Exception;},CancellationToken.None,TaskContinuationOptions.OnlyOnFaulted|TaskContinuationOptions.ExecuteSynchronously,TaskScheduler.Default);stop.ThrowIfCancellationRequested();}return await pending;}}
 }
}
