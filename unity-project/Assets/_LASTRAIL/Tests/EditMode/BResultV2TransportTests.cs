#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using LastRail.Run;
namespace LastRail.Tests.EditMode {public sealed class BResultV2TransportTests {
 [Test] public async Task ActualV2OwnerMustSelectV2ReportWithoutDowngrading(){var method=typeof(BRuntimeObservationDeliveryTests).GetMethod("ShortSession",BindingFlags.Static|BindingFlags.NonPublic);using(var c=await (Task<BRouteSessionController>)method.Invoke(null,null)){var q=new BResultReportRequest(c.CaptureOriginalRuntimeObservation(),"actual-v2");Assert.AreEqual("combat.b-result-report.v2.20261006",(string)q.Body()["version"]);Assert.NotNull(typeof(BRouteSessionController).Assembly.GetType("LastRail.Run.BDepartureRuntimeInputClient"));}}
 [Test] public void CurrentV2WholeBudgetKeepsV1Unchanged(){Assert.AreEqual(6000000,BResultReportRequest.V2ByteLimit);Assert.AreEqual(1800000,BResultReportRequest.ByteLimit);}
}}
#endif
