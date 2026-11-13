using System;
using LastRail.Run;
namespace LastRail.Battle {
 // The immutable entry and the current accepted attempt have different lifetimes.
 public sealed class OrdinaryNodeAttempt {
  readonly OrdinaryNodeStart origin;
  public string AccountId=>origin.AccountId;
  public string RunId=>origin.RunId;
  public string NodeId=>origin.NodeId;
  public string SnapshotHash=>origin.SnapshotHash;
  public long Generation {get;}
  public long Revision {get;}
  OrdinaryNodeAttempt(OrdinaryNodeStart node,long generation,long revision){origin=node;Generation=generation;Revision=revision;}
  public static OrdinaryNodeAttempt FromEntry(OrdinaryNodeStart node){
   if(node==null)throw new ArgumentNullException(nameof(node));return new OrdinaryNodeAttempt(node,node.Generation,node.EntryMapRevision);
  }
  public static OrdinaryNodeAttempt FromRestart(OrdinaryNodeRestartResult result){
   if(result?.Start==null||!result.Generation.HasValue)throw new ArgumentException("A confirmed restarted node is required.");
   return new OrdinaryNodeAttempt(result.Start,result.Generation.Value,result.Revision);
  }
  public static OrdinaryNodeAttempt FromCurrent(OrdinaryNodeStart node,OrdinaryCurrentAttempt current){
   if(node==null||current==null||current.AccountId!=node.AccountId||current.RunId!=node.RunId||current.NodeId!=node.NodeId||current.SnapshotHash!=node.SnapshotHash)
    throw new ArgumentException("Current attempt differs from original node.");
   var result=new OrdinaryNodeAttempt(node,current.Generation,current.Revision);result.RequireNode(node);return result;
  }
  internal void RequireNode(OrdinaryNodeStart node){
   if(node==null||node.AccountId!=AccountId||node.RunId!=RunId||node.NodeId!=NodeId||node.SnapshotHash!=SnapshotHash||Generation<node.Generation||Revision<node.EntryMapRevision)
    throw new ArgumentException("Current attempt differs from original node.");
  }
 }
}
