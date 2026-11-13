using System;
using System.Collections.Generic;
using System.Reflection;
using LastRail.Diagnostics;
using LastRail.Presentation.MissionShell;
using LastRail.Run;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class LobbyRunSelectionTests
    {
        static PendingRunOperation Start(string run, string account="account_a", int day=1, bool confirmed=true)
        {
            var request=PendingRunOperation.Start(account,RunStartRequest.Create("preview_"+run,"original_start_key_"+run),new AppBuildSnapshot("1",1),new DateTimeOffset(2026,10,day,0,0,0,TimeSpan.Zero));
            return confirmed?request.Confirmed(new JObject{["accepted"]=true,["startStatus"]="STARTED",["runId"]=run,["operationPreviewId"]="preview_"+run,["balanceVersion"]="1",["serverStartedAt"]="2026-10-01T00:00:00.000Z",["runVersionSnapshot"]=null}.ToString()):request;
        }
        static object Select(IEnumerable<PendingRunOperation> rows,RunLifecycleSnapshot lifecycle=null,PendingAccountAction pending=null)
        {
            var type=typeof(AccountScreenRuntime).Assembly.GetType("LastRail.Presentation.MissionShell.LobbyRunSelection");
            Assert.IsNotNull(type,"Missing single run selection contract");
            return type.GetMethod("Resolve").Invoke(null,new object[]{"account_a",rows,lifecycle,null,null,pending,null});
        }
        static object Field(object selection,string name)=>selection.GetType().GetProperty(name).GetValue(selection);
        [Test] public void DifferentRunsRemainConflictRegardlessOfReceiptTimeOrOrder()
        {
            foreach(var rows in new[]{new[]{Start("run_a"),Start("run_b",day:20)},new[]{Start("run_b",day:20),Start("run_a")}}){
                var value=Select(rows);Assert.AreEqual("Conflict",Field(value,"Kind").ToString());Assert.IsNull(Field(value,"RunId"));
            }
        }
        [Test] public void SameRunCollapsesAndForeignAccountNeverParticipates()
        {
            var value=Select(new[]{Start("run_a"),Start("run_a",day:20),Start("run_b","account_b")});
            Assert.AreEqual("Recovery",Field(value,"Kind").ToString());Assert.AreEqual("run_a",Field(value,"RunId"));
        }
        [Test] public void UnconfirmedRequestIsNotEvidenceOfAbsenceOrAnActiveRun()
        {
            var value=Select(new[]{Start("run_a",confirmed:false)});
            Assert.AreEqual("AwaitingConfirmation",Field(value,"Kind").ToString());Assert.IsNull(Field(value,"RunId"));
        }
        [Test] public void SamePreviewUncertainRecordCollapsesIntoItsConfirmedRun()
        {
            var pending=new PendingAccountAction("account_a","departure-start","preview_run_a",0,"original_start_key_run_a","{\"operationPreviewId\":\"preview_run_a\"}");
            var value=Select(new[]{Start("run_a")},pending:pending);
            Assert.AreEqual("Recovery",Field(value,"Kind").ToString());Assert.AreEqual("run_a",Field(value,"RunId"));
        }
        [Test] public void AnotherRequestForSamePreviewRemainsUncertain()
        {
            var pending=new PendingAccountAction("account_a","departure-start","preview_run_a",0,"different_original_key","{\"operationPreviewId\":\"preview_run_a\"}");
            var value=Select(new[]{Start("run_a")},pending:pending);
            Assert.AreEqual("AwaitingConfirmation",Field(value,"Kind").ToString());Assert.IsNull(Field(value,"RunId"));
        }
        [Test] public void CurrentTerminalLifecycleOutranksOldStartedReceipt()
        {
            var method=typeof(RunLifecycleTests).GetMethod("Data",BindingFlags.Static|BindingFlags.NonPublic);
            var json=(JObject)method.Invoke(null,new object[]{"EXPIRED"});
            var value=Select(new[]{Start("run_test",day:20)},new RunLifecycleSnapshot(json.ToString(),"run_test"));
            Assert.AreEqual("Terminal",Field(value,"Kind").ToString());Assert.AreEqual("run_test",Field(value,"RunId"));
        }
    }
}
