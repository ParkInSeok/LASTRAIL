using System;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
namespace LastRail.Run {
 // Compares Server consistency facts with the unchanged original local journal.
 // This comparison does not prove combat truth or mint an opaque Server reference.
 internal static class BResultJournalReceipt {
  internal static void RequireMatches(JObject verified,JObject original){
   if(verified==null||original==null||!(original["records"] is JArray records)||records.Count!=3)throw new ArgumentException("b_result_journal_receipt_missing");
   BDirectAttackInput.Fields(verified,"version","journalHash","waves","terminal");
   JObject Fact(JToken row){var b=row["binding"];return new JObject{["eventRef"]=row["eventRef"].DeepClone(),["commandId"]=b["originalCommand"]["eventId"].DeepClone(),["commandRevision"]=b["commandRevision"].DeepClone(),["rawSequence"]=b["rawEvent"]["sequence"].DeepClone(),["pointSequence"]=row["checkpoint"]["sequence"].DeepClone(),["tick"]=b["rawEvent"]["tick"].DeepClone(),["checkpointHash"]=b["checkpointHash"].DeepClone()};}
   var expected=new JObject{["version"]="combat.b-original-journal-consistency.v1.20261006",["journalHash"]=BDirectAttackInput.Digest(original),["waves"]=new JArray(records.Take(2).Select(Fact)),["terminal"]=Fact(records[2])};
   if(!JToken.DeepEquals(expected,verified))throw new ArgumentException("b_result_journal_receipt_original_conflict");
  }
 }
}
