using System;
using LastRail.Auth;
using LastRail.Client;
using NUnit.Framework;
using LastRail.Diagnostics;
using Newtonsoft.Json.Linq;
namespace LastRail.Tests.EditMode {
 public sealed class DataLifecycleDiagnosticsTests {
  [TestCase(ClientEnvironment.Prd,ClientEnvironmentBootstrap.PrdClientConfigUrl,false)]
  [TestCase(ClientEnvironment.Dev,ClientEnvironmentBootstrap.PrdClientConfigUrl,false)]
  [TestCase(ClientEnvironment.Unconfigured,ClientEnvironmentBootstrap.DevClientConfigUrl,false)]
  [TestCase(ClientEnvironment.Dev,ClientEnvironmentBootstrap.DevClientConfigUrl,true)]
  public void TraceNeedsDevEnvironmentAndActualDevEndpoint(ClientEnvironment environment,string url,bool expected){
   var type=typeof(PlayerEntryRuntime).Assembly.GetType("LastRail.Diagnostics.DataLifecycleTrace");Assert.IsNotNull(type);
   using var trace=(IDisposable)Activator.CreateInstance(type,environment,url);
   Assert.AreEqual(expected,type.GetProperty("Enabled").GetValue(trace));
  }
  [Test] public void OverflowDropsOldestAndDisposeReleasesExport(){
   var trace=new DataLifecycleTrace(ClientEnvironment.Dev,ClientEnvironmentBootstrap.DevClientConfigUrl);
   for(int i=0;i<1000;i++)trace.Record(LifecycleData.Account,LifecycleEvent.CacheHit,LifecycleReason.Entry,3);
   var export=JObject.Parse(trace.Export());Assert.AreEqual(256,trace.Count);Assert.AreEqual(744,(long)export["droppedOldest"]);Assert.AreEqual(745,(long)export["records"][0]["sequence"]);
   trace.Dispose();Assert.AreEqual(0,trace.Count);Assert.AreEqual("",trace.Export());
  }
  [Test] public void PrdRecordsAndExportsNothing(){
   using var trace=new DataLifecycleTrace(ClientEnvironment.Prd,ClientEnvironmentBootstrap.PrdClientConfigUrl);
   trace.Record(LifecycleData.Account,LifecycleEvent.Requested,LifecycleReason.Entry);Assert.AreEqual(0,trace.Count);Assert.AreEqual("",trace.Export());
  }
 }
}
