using System;
using System.Threading.Tasks;
using LastRail.Auth;
using LastRail.Run;

namespace LastRail.Presentation.MissionShell {
 public sealed partial class AccountScreenRuntime {
  string ordinaryMapRun;
  string ordinaryMapAccount;
  long ordinaryMapGeneration;
  long ordinaryMapMinimumRevision;
  Task<OperationMapStatus> ordinaryMapRead;
  Task<BDepartureSourceBundle> ordinaryTravelSourceRead;
  void ResetOrdinaryMap(){ordinaryMapRun=null;ordinaryMapAccount=null;ordinaryMapRead=null;ordinaryTravelSourceRead=null;ordinaryMapMinimumRevision=0;}
  public void InvalidateOrdinaryMap(string run,long acceptedRevision){
   if(ordinaryMapRun!=run)return;ordinaryMapMinimumRevision=Math.Max(ordinaryMapMinimumRevision,acceptedRevision);
   if(ordinaryMapRead?.Status==TaskStatus.RanToCompletion&&ordinaryMapRead.Result.Revision<ordinaryMapMinimumRevision)ordinaryMapRead=null;
  }
  void BindOrdinaryMap(string run){
   ReadOrdinaryTravel(run);
   if(ordinaryMapRun==run&&ordinaryMapAccount==owner.AccountId&&ordinaryMapGeneration==ownerGeneration)return;
   ResetOrdinaryMap();ordinaryMapRun=run;ordinaryMapAccount=owner.AccountId;ordinaryMapGeneration=ownerGeneration;
  }
  public Task<OperationMapStatus> ReadOrdinaryMapAsync(string run){
   BindOrdinaryMap(run);if(ordinaryMapRead==null||ordinaryMapRead.IsFaulted||ordinaryMapRead.IsCanceled)ordinaryMapRead=Read();return ordinaryMapRead;
   async Task<OperationMapStatus> Read(){
    var original=owner;var stamp=ownerGeneration;
    var reply=await new GameApiAuthenticatedHttpTransport(wire).SendAsync(new RunApiHttpRequest("GET","/v1/runs/"+Uri.EscapeDataString(run)+"/operation-map",null,null),original.AccessToken);
    if(!Current(original,stamp)||ordinaryMapRun!=run)throw new InvalidOperationException("Map owner changed.");
    if(reply.Outcome!=GameApiAuthenticatedHttpOutcome.Succeeded||reply.Response.StatusCode!=200)throw new InvalidOperationException("Operation map could not be read.");
    var result=OperationMapStatus.Read(reply.Response.BodyJson,run);
    if(result.CurrentAttempt!=null&&result.CurrentAttempt.AccountId!=original.AccountId)throw new InvalidOperationException("Map attempt account changed.");
    if(result.Revision<ordinaryMapMinimumRevision)throw new InvalidOperationException("Map response predates an accepted transition.");return result;
   }
  }
  public Task<BDepartureSourceBundle> ReadOrdinaryTravelSourceAsync(string run){
   BindOrdinaryMap(run);if(ordinaryTravelSourceRead==null||ordinaryTravelSourceRead.IsFaulted||ordinaryTravelSourceRead.IsCanceled)ordinaryTravelSourceRead=Read();return ordinaryTravelSourceRead;
   async Task<BDepartureSourceBundle> Read(){
    var original=owner;var stamp=ownerGeneration;
    var source=await new BDepartureSourceBundleHttpClient(new GameApiAuthenticatedHttpTransport(wire)).ReadAsync(original.AccountId,run,original.AccessToken);
    if(!Current(original,stamp)||ordinaryMapRun!=run)throw new InvalidOperationException("Travel source owner changed.");return source;
   }
  }
 }
}
