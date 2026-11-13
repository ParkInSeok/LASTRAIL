using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using LastRail.Resources;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading;
namespace LastRail.Presentation.MissionShell {
 // Only this cache loads/unloads the inventory atlas. Metadata does not hold textures.
 public sealed class EquipmentIconResources {
  public sealed class Lease:IDisposable {public Texture2D Texture{get;private set;} Action release;internal Lease(Texture2D texture,Action release){Texture=texture;this.release=release;}public void Dispose(){var owned=release;release=null;Texture=null;owned?.Invoke();}}
  static readonly Dictionary<string,Rect> regions=new Dictionary<string,Rect>(StringComparer.Ordinal){
   {"equipment.category.hp",new Rect(0,1-.125f-1f/3,1f/3,1f/3)},
   {"equipment.category.atk",new Rect(1f/3,1-.125f-1f/3,1f/3,1f/3)},
   {"equipment.category.def",new Rect(0,1-.832f*2f/3-1f/3,1f/3,1f/3)}
  };
  public static bool TryRegion(string key,out Rect rect){rect=default;return key!=null&&regions.TryGetValue(key,out rect);}
  public const string AtlasAddress="Assets/_LASTRAIL/Content/ApprovedUi20261110/d01-remaining-art-v01/items-atlas.png";
  public EquipmentIconResources(AddressableContentBackend backend){
   if(backend==null)throw new ArgumentNullException(nameof(backend));
   AsyncOperationHandle<Texture2D> handle=default;
   load=async()=>{
    var location=backend.ResolveApprovedTexture(AtlasAddress);var revision=backend.ContentRevision;
    handle=Addressables.LoadAssetAsync<Texture2D>(location);
    try{await AddressableContentBackend.Complete(handle,CancellationToken.None);
     if(revision!=backend.ContentRevision||!ReferenceEquals(location,backend.ResolveApprovedTexture(AtlasAddress)))throw new OperationCanceledException();
     if(handle.Result==null)throw new InvalidOperationException("Equipment atlas missing");
     return handle.Result;
    }catch{AddressableContentBackend.ReleaseAfterCompletion(handle);handle=default;throw;}
   };
   unload=_=>{AddressableContentBackend.ReleaseAfterCompletion(handle);handle=default;};
  }
  readonly Func<Task<Texture2D>> load;readonly Action<Texture2D> unload;Task<Texture2D> pending;int users;
  public EquipmentIconResources(Func<Task<Texture2D>> load,Action<Texture2D> unload){this.load=load??throw new ArgumentNullException(nameof(load));this.unload=unload??throw new ArgumentNullException(nameof(unload));}
  public async Task<Lease> AcquireAsync(){users++;try{if(pending==null)pending=load();var texture=await pending;if(texture==null)throw new InvalidOperationException("Equipment atlas missing");return new Lease(texture,Release);}catch{Release();throw;}}
  void Release(){if(--users!=0)return;var owned=pending;pending=null;if(owned?.Status==TaskStatus.RanToCompletion&&owned.Result!=null)unload(owned.Result);}
 }
}
