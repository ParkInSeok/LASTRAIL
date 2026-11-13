using System;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Keeps the separate public v2 contract over the shared owned publication lifetime.
 public sealed class BInterceptDefinitionClient:IDisposable {
  readonly ImmutableCombatDefinitionClient<BInterceptDefinitionSnapshot> owner;
  public event Action Changed {add=>owner.Changed+=value;remove=>owner.Changed-=value;}
  public string Status=>owner.Status;public string ErrorCode=>owner.ErrorCode;public BInterceptDefinitionSnapshot Snapshot=>owner.Snapshot;
  public BInterceptDefinitionClient(IGameApiAuthenticatedHttpTransport transport,Func<GameApiAuthSessionSnapshot> session,Func<long> generation){owner=new ImmutableCombatDefinitionClient<BInterceptDefinitionSnapshot>(transport,session,generation,"/v3/combat/b-scout-gunner-definition",BInterceptDefinitionSnapshot.Version,BInterceptDefinitionSnapshot.ApprovedPacketHash,"combat_b_intercept",BInterceptDefinitionSnapshot.Read,BInterceptDefinitionSnapshot.ReadJson,nameof(BInterceptDefinitionClient));}
  public void Invalidate()=>owner.Invalidate();public void RestoreVerified(JObject cache,string independentlyExpectedPacketHash)=>owner.RestoreVerified(cache,independentlyExpectedPacketHash);
  public Task RefreshAsync(string independentlyExpectedPacketHash,string ifMatch=null,CancellationToken stop=default)=>owner.RefreshAsync(independentlyExpectedPacketHash,ifMatch,stop);
  public void Dispose()=>owner.Dispose();
 }
}