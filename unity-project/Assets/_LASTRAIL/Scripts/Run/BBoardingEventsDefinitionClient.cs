using System;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Auth;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Keeps the separate public v2 contract over the shared owned publication lifetime.
 public sealed class BBoardingEventsDefinitionClient:IDisposable {
  readonly ImmutableCombatDefinitionClient<BBoardingEventsDefinitionSnapshot> owner;
  public event Action Changed {add=>owner.Changed+=value;remove=>owner.Changed-=value;}
  public string Status=>owner.Status;public string ErrorCode=>owner.ErrorCode;public BBoardingEventsDefinitionSnapshot Snapshot=>owner.Snapshot;
  public BBoardingEventsDefinitionClient(IGameApiAuthenticatedHttpTransport transport,Func<GameApiAuthSessionSnapshot> session,Func<long> generation){owner=new ImmutableCombatDefinitionClient<BBoardingEventsDefinitionSnapshot>(transport,session,generation,"/v2/combat/b-scout-gunner-definition",BBoardingEventsDefinitionSnapshot.Version,BBoardingEventsDefinitionSnapshot.ApprovedPacketHash,"combat_b_boarding",BBoardingEventsDefinitionSnapshot.Read,BBoardingEventsDefinitionSnapshot.ReadJson,nameof(BBoardingEventsDefinitionClient));}
  public void Invalidate()=>owner.Invalidate();public void RestoreVerified(JObject cache,string independentlyExpectedPacketHash)=>owner.RestoreVerified(cache,independentlyExpectedPacketHash);
  public Task RefreshAsync(string independentlyExpectedPacketHash,string ifMatch=null,CancellationToken stop=default)=>owner.RefreshAsync(independentlyExpectedPacketHash,ifMatch,stop);
  public void Dispose()=>owner.Dispose();
 }
}