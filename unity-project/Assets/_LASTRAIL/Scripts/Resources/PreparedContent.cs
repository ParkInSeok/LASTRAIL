using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace LastRail.Resources {
 // The bootstrap owns one session; scopes own asset handles, never gameplay/account state.
 public static class PreparedContent {
  public sealed class AssetLease:IDisposable {
   public UnityEngine.Object Asset {get;private set;} Action release;
   public AssetLease(UnityEngine.Object asset,Action release){Asset=asset;this.release=release;}
   public void Dispose(){var action=release;release=null;Asset=null;action?.Invoke();}
  }
  public sealed class Scope:IDisposable {
   List<Action> releases;
   internal Scope(List<Action> releases){this.releases=releases;}
   public void Dispose(){var owned=releases;releases=null;if(owned!=null)foreach(var release in owned)release();}
  }
  public sealed class Session:IDisposable {
   sealed class Entry {public Task<AssetLease> Pending;public int Users;}
   readonly Dictionary<string,Entry> entries=new Dictionary<string,Entry>(StringComparer.Ordinal);
   readonly Func<string,Task<AssetLease>> load;readonly Action validate;readonly Func<CancellationToken,Task> initializePublic;bool disposed;
   public Session(Func<string,Task<AssetLease>> load,Action validate=null,Func<CancellationToken,Task> initializePublic=null){this.initializePublic=initializePublic;this.load=load??throw new ArgumentNullException(nameof(load));this.validate=validate;}
   internal Task InitializePublic(CancellationToken cancel){Check();return initializePublic!=null?initializePublic(cancel):throw new InvalidOperationException("Public artwork initialization is not configured.");}
   internal void Check(){if(disposed)throw new ObjectDisposedException(nameof(Session));validate?.Invoke();}
   public async Task<Scope> PrepareAsync(IEnumerable<string> keys,CancellationToken cancel=default){
    Check();cancel.ThrowIfCancellationRequested();var acquired=new List<Action>();
    try{await Task.WhenAll(keys.Distinct(StringComparer.Ordinal).Select(async key=>{
     Check();cancel.ThrowIfCancellationRequested();
     if(!entries.TryGetValue(key,out var entry)){entry=new Entry();entries.Add(key,entry);try{entry.Pending=load(key);}catch{entries.Remove(key);throw;}}
     entry.Users++;Action release=()=>Release(key,entry);acquired.Add(release);
     var lease=await entry.Pending;if(lease?.Asset==null)throw new InvalidOperationException("Remote asset is missing: "+key);
     cancel.ThrowIfCancellationRequested();Check();
    }));return new Scope(acquired);}
    catch{foreach(var release in acquired)release();throw;}
   }
   public UnityEngine.Object Get(string key){Check();if(!entries.TryGetValue(key,out var entry)||entry.Users<=0||entry.Pending.Status!=TaskStatus.RanToCompletion)throw new InvalidOperationException("Prepare the resource before creating its view: "+key);return entry.Pending.Result.Asset;}
   void Release(string key,Entry entry){if(--entry.Users!=0)return;entries.Remove(key);if(entry.Pending.Status==TaskStatus.RanToCompletion)entry.Pending.Result?.Dispose();}
   public void Dispose(){disposed=true;if(ReferenceEquals(current,this))current=null;}
  }
  static Session current;
#if UNITY_EDITOR
  // Explicit Editor adapter for existing isolated authoring/tests; absent from Player builds.
  public static Func<string,Type,UnityEngine.Object> EditorAsset;
#endif
  internal static Task InitializePublicEntryAsync(CancellationToken cancel)=>current!=null?current.InitializePublic(cancel):throw new InvalidOperationException("Public artwork requires the bootstrap resource owner.");
  public static bool RuntimeInstalled=>current!=null;
  public static IDisposable Install(AddressableContentBackend backend){
   if(current!=null)throw new InvalidOperationException("A prepared-content session is already owned by the bootstrap.");
   string revision=null;int retainedAssets=0;
   var session=new Session(async key=>{
    var entry=RemoteContentCatalog.Find(key)??throw new InvalidOperationException("Unknown resource key: "+key);
    var location=backend.ResolveApprovedAsset(entry.Address,entry.ResourceType);var stamp=backend.ContentRevision;
    var handle=Addressables.LoadAssetAsync<UnityEngine.Object>(location);bool retained=false;
    try{await Complete(handle);backend.ValidatePreparedContent(stamp);if(handle.Result==null)throw new InvalidOperationException("Remote resource unavailable: "+key);revision=stamp;retainedAssets++;retained=true;return new AssetLease(handle.Result,()=>{AddressableContentBackend.ReleaseAfterCompletion(handle);if(--retainedAssets==0)revision=null;});}
    finally{if(!retained)AddressableContentBackend.ReleaseAfterCompletion(handle);}
   },()=>{if(revision!=null)backend.ValidatePreparedContent(revision);},backend.InitializePublicEntryAsync);
   current=session;return session;
  }
  static Task Complete(AsyncOperationHandle<UnityEngine.Object> handle){var done=new TaskCompletionSource<bool>();void Finish(AsyncOperationHandle<UnityEngine.Object> result){if(result.Status==AsyncOperationStatus.Succeeded)done.TrySetResult(true);else done.TrySetException(result.OperationException??new InvalidOperationException("Resource load failed."));}if(handle.IsDone)Finish(handle);else handle.Completed+=Finish;return done.Task;}
  public static UnityEngine.Object Load(string key)=>Load<UnityEngine.Object>(key);
  public static T Load<T>(string key) where T:UnityEngine.Object {
   var entry=RemoteContentCatalog.Find(key);if(entry==null)return null;
   if(entry.Local)return UnityEngine.Resources.Load<T>(key);
   if(current!=null)return current.Get(key) as T;
#if UNITY_EDITOR
   if(EditorAsset!=null)return EditorAsset(entry.Path,typeof(T)) as T;
#endif
   throw new InvalidOperationException("The approved content owner is not ready: "+key);
  }
  public static Task<Scope> PrepareAsync(IEnumerable<string> selectors,CancellationToken token=default){
   token.ThrowIfCancellationRequested();
   var keys=RemoteContentCatalog.Select(selectors).Where(entry=>!entry.Local).Select(entry=>entry.Key);
   if(current!=null)return current.PrepareAsync(keys,token);
#if UNITY_EDITOR
   if(EditorAsset!=null)return Task.FromResult(new Scope(new List<Action>()));
#endif
   throw new InvalidOperationException("The approved content owner is not ready.");
  }
  public static void Attach(GameObject owner,Scope scope){if(scope==null)return;if(owner==null){scope.Dispose();throw new ArgumentNullException(nameof(owner));}owner.AddComponent<PreparedContentLifetime>().Own(scope);}
 }
 public sealed class PreparedContentLifetime:MonoBehaviour {
  PreparedContent.Scope scope;
  public void Own(PreparedContent.Scope value){scope?.Dispose();scope=value;}
  void OnDestroy(){scope?.Dispose();scope=null;}
 }
}
