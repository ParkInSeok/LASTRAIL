using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
namespace LastRail.Battle {
 public sealed class BOperationRiskHttpTransport : IDisposable {
  sealed class PrivateAuth:IDisposable {
   string authorization;internal readonly string Origin,AccountId;
   internal PrivateAuth(JObject input){BuffWire.Fields(input,"origin","accountId","headers");var h=BuffWire.Object(input["headers"]);BuffWire.Fields(h,"Authorization","X-Combat-B-Risk-Version");Origin=BuffWire.Text(input["origin"]);AccountId=BuffWire.Text(input["accountId"]);authorization=BuffWire.Text(h["Authorization"],8192);BuffWire.Need(Uri.TryCreate(Origin,UriKind.Absolute,out var uri)&&uri.Scheme=="http"&&uri.IsLoopback&&uri.AbsolutePath=="/"&&uri.Query==""&&uri.Fragment==""&&uri.UserInfo==""&&Origin==uri.GetLeftPart(UriPartial.Authority)&&authorization.StartsWith("Bearer ",StringComparison.Ordinal)&&authorization.Length>7&&authorization.IndexOfAny(new[]{'\r','\n'})<0&&(string)h["X-Combat-B-Risk-Version"]==BOperationRiskSource.Version,"risk_private_intake_invalid");}
   internal void Authorize(UnityWebRequest request){if(authorization==null)throw new ObjectDisposedException(nameof(PrivateAuth));request.SetRequestHeader("Authorization",authorization);}
   public void Dispose(){authorization=null;}
  }
  readonly OwnedUnityJsonExchange exchange;public string AccountId{get;}public int InFlightCount=>exchange.InFlightCount;
  public BOperationRiskHttpTransport(JObject privateIntake){var auth=new PrivateAuth(privateIntake);AccountId=auth.AccountId;exchange=new OwnedUnityJsonExchange(auth.Origin,"X-Combat-B-Risk-Version",auth.Authorize,auth);}
  public async Task<JObject> ReadAsync(JObject independentlyTrustedBinding,string independentlyTrustedHash,CancellationToken stop=default){if(independentlyTrustedBinding==null)throw new ArgumentNullException(nameof(independentlyTrustedBinding));var binding=(JObject)independentlyTrustedBinding.DeepClone();var response=await exchange.SendAsync("GET","/_local/combat-b/operation-risk",new Dictionary<string,string>{{"X-Combat-B-Risk-Version",BOperationRiskSource.Version}},null,stop);stop.ThrowIfCancellationRequested();BuffWire.Need(response.Status==200&&response.CacheControl=="no-store"&&response.Version==BOperationRiskSource.Version,"risk_exchange_failed");return BOperationRiskSource.Read(response.Body,binding,independentlyTrustedHash).Capture();}
  public void Dispose()=>exchange.Dispose();
 }
}
