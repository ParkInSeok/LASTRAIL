using System;
using Newtonsoft.Json.Linq;
using static LastRail.Run.RunWaveManifestReader;
namespace LastRail.Run {
 public sealed class BScoutGunnerNodeOwner {
  BScoutGunnerNodeSnapshot snapshot;JObject originalContext;
  public BScoutGunnerNodeSnapshot Snapshot=>snapshot;
  public void Invalidate(){snapshot=null;originalContext=null;}
  public void RestoreVerified(JObject packet,JObject independentlyTrustedOriginal){var verified=BScoutGunnerNodeSnapshot.Read(packet,independentlyTrustedOriginal);var context=new JObject{["origin"]=independentlyTrustedOriginal["origin"].DeepClone(),["nativeContextId"]=independentlyTrustedOriginal["nativeContextId"].DeepClone(),["seedHash"]=independentlyTrustedOriginal["seedHash"].DeepClone()};if(originalContext!=null)Need(JToken.DeepEquals(originalContext,context));originalContext=context;snapshot=verified;}
 }
}
