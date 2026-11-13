using System;
using System.Text.RegularExpressions;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Metadata from the existing authoritative map read, not a combat snapshot or receipt.
 public sealed class OrdinaryCurrentAttempt {
  public string AccountId{get;}public string RunId{get;}public string NodeId{get;}public string SnapshotHash{get;}
  public long Generation{get;}public long Revision{get;}
  internal OrdinaryCurrentAttempt(JObject source){
   BDirectAttackInput.Fields(source,"version","accountId","runId","nodeId","generation","mapRevision","snapshotHash");
   if((string)source["version"]!="ordinary.current-attempt.v1.20261102")throw new ArgumentException("Current attempt version differs.");
   AccountId=BDirectAttackInput.Text(source,"accountId",200);RunId=BDirectAttackInput.Text(source,"runId",200);NodeId=BDirectAttackInput.Text(source,"nodeId",200);
   Generation=OrdinaryNodeStart.Revision(source,"generation");Revision=OrdinaryNodeStart.Revision(source,"mapRevision");SnapshotHash=BDirectAttackInput.Text(source,"snapshotHash",64);
   if(!Regex.IsMatch(SnapshotHash,"^[a-f0-9]{64}$"))throw new ArgumentException("Current attempt snapshot hash differs.");
  }
 }
}
