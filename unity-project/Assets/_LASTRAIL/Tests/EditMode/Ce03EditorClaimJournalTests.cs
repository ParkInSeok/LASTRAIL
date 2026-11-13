using System;
using System.IO;
using System.Threading.Tasks;
using LastRail.Battle;
using LastRail.Run;
using LastRail.Diagnostics;
using NUnit.Framework;
public class Ce03EditorClaimJournalTests {
 [Test] public async Task DiskReopenRetainsOriginalClaimAndRejectsReplacement(){var directory=Path.Combine(Path.GetTempPath(),"ce03-claim-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);var path=Path.Combine(directory,"journal.json");try{var journal=new Ce03EditorClaimJournal(path,"a");var request=RunRewardClaimRequest.Create("run_test","preview_test","original_claim_key");var operation=PendingRunOperation.Claim("a",request,new AppBuildSnapshot(null,null),DateTimeOffset.UtcNow);Assert.IsTrue(await journal.PutAsync(operation));var reopened=new Ce03EditorClaimJournal(path,"a");var rows=await reopened.ReadAsync("a");Assert.AreEqual(1,rows.Count);Assert.AreEqual(operation.ComposedAt,rows[0].ComposedAt);Assert.IsTrue(await reopened.PutAsync(operation));Assert.AreEqual(operation.BodyJson,rows[0].BodyJson);Assert.AreEqual(operation.IdempotencyKey,rows[0].IdempotencyKey);Assert.IsFalse(await reopened.RemoveConfirmedAsync("a",PendingRunKind.Claim,operation.IdempotencyKey));var other=PendingRunOperation.Claim("a",RunRewardClaimRequest.Create("run_test","preview_test","replacement_claim_key"),new AppBuildSnapshot(null,null),DateTimeOffset.UtcNow);Assert.IsFalse(await reopened.PutAsync(other));Assert.ThrowsAsync<InvalidOperationException>(async()=>await reopened.ReadAsync("b"));}finally{if(File.Exists(path))File.Delete(path);Directory.Delete(directory);}}
}
