using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 // Borrowed main-thread geometry source. Domain owns no scene object or lifetime.
 public interface IMiraFirstGeometrySource {
  JObject CaptureAuthoring();
  JObject CaptureNativeState(long tick,long revision);
  // Validate wholly before publishing; failure must leave every live pose unchanged.
  void RestoreNativeState(JObject state,string independentlyPinnedStateHash);
 }
}