using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Owns only the authenticated loopback exchange; lifetime execution stays with the existing native owner.
 public sealed class BTargetLifecycleHttpTransport : IDisposable {
  readonly OwnedUnityJsonExchange exchange;public string AccountId {get;}public int InFlightCount=>exchange.InFlightCount;
  public BTargetLifecycleHttpTransport(JObject privateIntake){
   BuffWire.Fields(privateIntake,"origin","accountId","accessExpiresAt","headers");var h=BuffWire.Object(privateIntake["headers"]);BuffWire.Need((string)h["X-Combat-B-Lifecycle-Version"]==BTargetLifecycleJournal.Version,"b_lifecycle_transport_version");
   var copy=(JObject)privateIntake.DeepClone();copy["headers"]=new JObject{["Authorization"]=h["Authorization"].DeepClone(),["X-Combat-B-Refit-Version"]=h["X-Combat-B-Refit-Version"].DeepClone()};var auth=new BRefitPrivateIntake(copy);AccountId=auth.AccountId;exchange=new OwnedUnityJsonExchange(auth.Origin,"X-Combat-B-Lifecycle-Version",auth.Authorize,auth);
  }
  public async Task<JObject> ReadAsync(string independentlyTrustedHash,JObject trustedOriginal,CancellationToken stop=default){
   var response=await exchange.SendAsync("GET","/_local/combat-b/target-lifecycle",new Dictionary<string,string>{{"X-Combat-B-Lifecycle-Version",BTargetLifecycleJournal.Version},{"X-Combat-B-Refit-Version",BRefitRequest.Version},{"X-Combat-B-Projectile-Version",BActiveProjectileSourcePacket.Version}},null,stop);stop.ThrowIfCancellationRequested();
   BuffWire.Need(response.Status==200&&response.Version==BTargetLifecycleJournal.Version&&response.CacheControl=="no-store","b_lifecycle_exchange_failed");var verified=BTargetLifecycleJournal.Read(response.Body,trustedOriginal,independentlyTrustedHash);BuffWire.Need((string)verified.Capture()["accountId"]==AccountId,"b_lifecycle_exchange_account");return verified.Capture();
  }
  public void Dispose()=>exchange.Dispose();
 }
}
