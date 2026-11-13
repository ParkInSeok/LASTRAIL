using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
namespace LastRail.Battle {
 public sealed class BLocalFlowSourcePrivateIntake : IDisposable {
  readonly BRefitPrivateIntake auth;public string Origin=>auth.Origin;public string AccountId=>auth.AccountId;public override string ToString()=>"Private LOCAL flow source intake";
  public BLocalFlowSourcePrivateIntake(JObject packet){BuffWire.Fields(packet,"origin","accountId","accessExpiresAt","headers");var headers=BuffWire.Object(packet["headers"]);BuffWire.Fields(headers,"Authorization","X-Combat-B-Refit-Version","X-Combat-B-Selection-Version","X-Combat-B-Source-Version");BuffWire.Need((string)headers["X-Combat-B-Selection-Version"]==BSelectionRequest.Version&&(string)headers["X-Combat-B-Source-Version"]==BLocalFlowSourcePacket.Version);var copy=(JObject)packet.DeepClone();copy["headers"]=new JObject{["Authorization"]=headers["Authorization"].DeepClone(),["X-Combat-B-Refit-Version"]=headers["X-Combat-B-Refit-Version"].DeepClone()};auth=new BRefitPrivateIntake(copy);}
  internal void Authorize(UnityEngine.Networking.UnityWebRequest request)=>auth.Authorize(request);public void Dispose()=>auth.Dispose();
 }
 public sealed class BLocalFlowSourceResponse {
  readonly JObject original;public int Status {get;}public string Version {get;}public string CacheControl {get;}public JObject Body=>(JObject)original.DeepClone();
  internal BLocalFlowSourceResponse(UnityJsonExchangeResponse response){Status=response.Status;Version=response.Version;CacheControl=response.CacheControl;original=(JObject)response.Body.DeepClone();}
 }
 public sealed class BLocalFlowSourceHttpTransport : IDisposable {
  readonly OwnedUnityJsonExchange exchange;public string AccountId {get;}public int InFlightCount=>exchange.InFlightCount;
  public BLocalFlowSourceHttpTransport(BLocalFlowSourcePrivateIntake owned){if(owned==null)throw new ArgumentNullException(nameof(owned));AccountId=owned.AccountId;exchange=new OwnedUnityJsonExchange(owned.Origin,"X-Combat-B-Source-Version",owned.Authorize,owned);}
  public async Task<BLocalFlowSourceResponse> ReadAsync(CancellationToken stop=default){var response=await exchange.SendAsync("GET",BLocalFlowSourcePacket.Path,new Dictionary<string,string>{{"X-Combat-B-Refit-Version",BRefitRequest.Version},{"X-Combat-B-Source-Version",BLocalFlowSourcePacket.Version}},null,stop);return new BLocalFlowSourceResponse(response);}
  public void Dispose()=>exchange.Dispose();
 }
 // Owns one immutable candidate's source read, cancellation and publication. Battle state stays in the loop owner.
 public sealed class BLocalFlowSourceConnection : IDisposable {
  readonly BLocalFlowCandidate candidate;readonly BLocalFlowSourceHttpTransport transport;readonly string trustedHash;readonly CancellationTokenSource lifetime=new CancellationTokenSource();bool disposed;
  public bool InFlight {get;private set;}public BLocalFlowSourcePacket Packet {get;private set;}public BSelectionFailure LastError {get;private set;}
  public BLocalFlowSourceConnection(BLocalFlowCandidate borrowed,BLocalFlowSourceHttpTransport owned,string independentlyTrustedPacketHash){candidate=borrowed??throw new ArgumentNullException(nameof(borrowed));transport=owned??throw new ArgumentNullException(nameof(owned));if(candidate.AccountId!=transport.AccountId)throw new ArgumentException("b_local_source_account");if(string.IsNullOrEmpty(independentlyTrustedPacketHash))throw new ArgumentException("b_local_source_trust_required");trustedHash=independentlyTrustedPacketHash;}
  public async Task ReadAsync(CancellationToken cancellation=default){if(disposed)throw new ObjectDisposedException(nameof(BLocalFlowSourceConnection));if(InFlight)throw new InvalidOperationException("b_local_source_pending");InFlight=true;using var stop=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token,cancellation);try{var response=await transport.ReadAsync(stop.Token);stop.Token.ThrowIfCancellationRequested();if(disposed)throw new ObjectDisposedException(nameof(BLocalFlowSourceConnection));BuffWire.Need(response.CacheControl=="no-store"&&response.Version==BLocalFlowSourcePacket.Version);if(response.Status!=200){LastError=new BSelectionFailure(response.Status,response.Body);return;}var parsed=new BLocalFlowSourcePacket(candidate,response.Body,trustedHash);stop.Token.ThrowIfCancellationRequested();if(disposed)throw new ObjectDisposedException(nameof(BLocalFlowSourceConnection));Packet=parsed;LastError=null;}finally{InFlight=false;}}
  public void Dispose(){if(disposed)return;disposed=true;lifetime.Cancel();transport.Dispose();lifetime.Dispose();}
 }
}