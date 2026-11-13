using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Owns LOCAL authenticated exchange lifetime; execution remains in the original B loop.
 public sealed class BActiveProjectileSourceHttpTransport : IDisposable {
  readonly OwnedUnityJsonExchange exchange;
  public string AccountId {get;}
  public int InFlightCount=>exchange.InFlightCount;
  public BActiveProjectileSourceHttpTransport(JObject privateIntake){
   BuffWire.Fields(privateIntake,"origin","accountId","accessExpiresAt","headers");var h=BuffWire.Object(privateIntake["headers"]);
   BuffWire.Need((string)h["X-Combat-B-Projectile-Version"]==BActiveProjectileSourcePacket.Version,"b_active_transport_version");
   var copy=(JObject)privateIntake.DeepClone();copy["headers"]=new JObject{["Authorization"]=h["Authorization"].DeepClone(),["X-Combat-B-Refit-Version"]=h["X-Combat-B-Refit-Version"].DeepClone()};
   var auth=new BRefitPrivateIntake(copy);AccountId=auth.AccountId;exchange=new OwnedUnityJsonExchange(auth.Origin,"X-Combat-B-Projectile-Version",auth.Authorize,auth);
  }
  public async Task<JObject> ReadAsync(string trustedHash,BCarriageBuffDefinitions definitions,CancellationToken stop=default){
   var response=await exchange.SendAsync("GET","/_local/combat-b/active-projectile-source",new Dictionary<string,string>{{"X-Combat-B-Refit-Version",BRefitRequest.Version},{"X-Combat-B-Projectile-Version",BActiveProjectileSourcePacket.Version}},null,stop);
   stop.ThrowIfCancellationRequested();BuffWire.Need(response.Status==200&&response.Version==BActiveProjectileSourcePacket.Version&&response.CacheControl=="no-store","b_active_source_exchange_failed");
   var source=new BActiveProjectileSourcePacket(response.Body,trustedHash,definitions);BuffWire.Need((string)source.Capture()["accountId"]==AccountId,"b_active_source_account");return source.Capture();
  }
  public void Dispose()=>exchange.Dispose();
 }
}
