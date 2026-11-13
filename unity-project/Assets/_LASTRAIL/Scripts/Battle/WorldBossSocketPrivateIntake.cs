using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

namespace LastRail.Battle {
 // Private supplier IPC intake. No credential serialization/capture API.
 // The caller transfers ownership to WorldBossSocketTransport.
 public sealed class WorldBossSocketPrivateIntake : IDisposable {
  string authorization;
  bool disposed;
  public string Origin { get; }
  public string AccountId { get; }
  public string ContentVersion { get; }
  public DateTimeOffset ExpiresAt { get; }
  public override string ToString() => "Private world authority intake";
  public WorldBossSocketPrivateIntake(JObject input) {
   WorldBossRailSnapshot.Fields(input,"origin","accountId","accessTokenExpiresAt","headers");
   var origin=Text(input,"origin",80);
   if(!Uri.TryCreate(origin,UriKind.Absolute,out var uri)||uri.Scheme!="http"||uri.Host!="127.0.0.1"||uri.Port<1||uri.Port>65535||
      uri.UserInfo!=""||uri.AbsolutePath!="/"||uri.Query!=""||uri.Fragment!=""||origin!="http://127.0.0.1:"+uri.Port.ToString(CultureInfo.InvariantCulture))
    throw new ArgumentException("world_socket_origin_invalid");
   Origin=origin;AccountId=Text(input,"accountId",128);
   if(!DateTimeOffset.TryParseExact(Text(input,"accessTokenExpiresAt",64),"yyyy-MM-dd'T'HH:mm:ss.fff'Z'",CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var expiry)||expiry<=DateTimeOffset.UtcNow)
    throw new ArgumentException("world_socket_expiry_invalid");
   ExpiresAt=expiry;
   var h=input["headers"] as JObject;
   WorldBossRailSnapshot.Fields(h,"Authorization","Content-Type","X-World-Authority-Version","X-World-Rail-Version","X-World-Journal-Version","X-World-Health-Version","X-World-Content-Version");
   if(Text(h,"Content-Type",64)!="application/json"||Text(h,"X-World-Authority-Version",128)!=WorldBossAuthorityRequest.Version||
      Text(h,"X-World-Rail-Version",128)!=WorldBossRailSnapshot.Version||Text(h,"X-World-Journal-Version",128)!=WorldBossAuthorityRequest.JournalVersion||
      Text(h,"X-World-Health-Version",128)!=WorldBossAuthorityRequest.HealthVersion)
    throw new ArgumentException("world_socket_version_invalid");
   ContentVersion=Text(h,"X-World-Content-Version",200);
   var secret=Text(h,"Authorization",8192);
   if(!Regex.IsMatch(secret,@"^Bearer [A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$"))
    throw new ArgumentException("world_socket_credentials_invalid");
   authorization=secret;
  }
  static string Text(JObject o,string key,int max) {
   var t=o?[key];if(t?.Type!=JTokenType.String||string.IsNullOrEmpty((string)t)||((string)t).Length>max)
    throw new ArgumentException("world_socket_intake_invalid");
   return (string)t;
  }
  internal void Authorize(UnityWebRequest request) {
   if(disposed)throw new ObjectDisposedException(nameof(WorldBossSocketPrivateIntake));
   if(DateTimeOffset.UtcNow>=ExpiresAt)throw new InvalidOperationException("world_socket_credentials_expired");
   request.SetRequestHeader("Authorization",authorization);
  }
  public void Dispose(){if(disposed)return;disposed=true;authorization=null;}
 }
}
