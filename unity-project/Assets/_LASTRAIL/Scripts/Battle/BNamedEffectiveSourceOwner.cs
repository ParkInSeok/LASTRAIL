using System;
using System.Threading.Tasks;
namespace LastRail.Battle {
 // Owns one immutable current source and one consumer binding, never native HP/state.
 // Generation invalidates borrowed asynchronous results after rebind/unbind/disposal.
 public sealed class BNamedEffectiveSourceOwner:IDisposable {
  readonly string accountId;long generation;bool disposed;
  Action<BNamedEffectiveSource> changed;
  public BNamedEffectiveSource Current {get;private set;}
  public BNamedEffectiveSourceOwner(string account){if(string.IsNullOrWhiteSpace(account))throw new ArgumentException("b_effective_owner_account");accountId=account;}
  public void Bind(Action<BNamedEffectiveSource> callback){NeedAlive();changed=callback??throw new ArgumentNullException(nameof(callback));generation=checked(generation+1);}
  public void Unbind(){NeedAlive();changed=null;generation=checked(generation+1);}
  public async Task<bool> RefreshAsync(Func<Task<BNamedEffectiveSource>> capture){
   NeedAlive();if(changed==null||capture==null)throw new ArgumentException("b_effective_source_binding_missing");
   long ticket=generation=checked(generation+1);var source=await capture();
   if(disposed||changed==null||ticket!=generation)return false;
   if(source==null||source.AccountId!=accountId)throw new ArgumentException("b_effective_source_account_conflict");
   Current=source;changed(source);return true;
  }
  void NeedAlive(){if(disposed)throw new ObjectDisposedException(nameof(BNamedEffectiveSourceOwner));}
  public void Dispose(){if(disposed)return;disposed=true;changed=null;generation=checked(generation+1);}
 }
}
