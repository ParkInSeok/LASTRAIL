using System.IO;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class Ce03R6RecoveryTests {
 [Test] public void ServerR6TransportFixtureParsesAndRecoversOriginalStart(){
  var fixture=Ce03R6RecoveryChecks.ReadJson(File.ReadAllText("D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/audit-results/ce03-local-host/synthetic-r6-exchanges/run_75f2337a-b608-4f5e-8c99-d53930107567.json"));
  Ce03R6RecoveryChecks.Run(fixture);
 }
}
