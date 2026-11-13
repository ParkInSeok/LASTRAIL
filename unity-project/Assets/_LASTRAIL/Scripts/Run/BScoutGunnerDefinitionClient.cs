using System;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Keeps the original public v1 contract over the shared owned publication lifetime.
 public sealed class BScoutGunnerDefinitionClient:IDisposable {
  readonly ImmutableCombatDefinitionClient<BScoutGunnerDefinitionSnapshot> owner;
  public event Action Changed {add=>owner.Changed+=value;remove=>owner.Changed-=value;}
  public string Status=>owner.Status;public string ErrorCode=>owner.ErrorCode;public BScoutGunnerDefinitionSnapshot Snapshot=>owner.Snapshot;
  public BScoutGunnerDefinitionClient(IGameApiAuthenticatedHttpTransport transport,Func<GameApiAuthSessionSnapshot> session,Func<long> generation){owner=new ImmutableCombatDefinitionClient<BScoutGunnerDefinitionSnapshot>(transport,session,generation,"/v1/combat/b-scout-gunner-definition",BScoutGunnerDefinitionSnapshot.Version,BScoutGunnerDefinitionSnapshot.ApprovedPacketHash,"combat_b_scout_gunner",BScoutGunnerDefinitionSnapshot.Read,BScoutGunnerDefinitionSnapshot.ReadJson,nameof(BScoutGunnerDefinitionClient));}
  public void Invalidate()=>owner.Invalidate();public void RestoreVerified(JObject cache,string independentlyExpectedPacketHash)=>owner.RestoreVerified(cache,independentlyExpectedPacketHash);
  public Task RefreshAsync(string independentlyExpectedPacketHash,string ifMatch=null,CancellationToken stop=default)=>owner.RefreshAsync(independentlyExpectedPacketHash,ifMatch,stop);
  public void Dispose()=>owner.Dispose();
 }
}