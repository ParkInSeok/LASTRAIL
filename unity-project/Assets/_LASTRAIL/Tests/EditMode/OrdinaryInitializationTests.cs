using System;
using System.Reflection;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
public class OrdinaryInitializationTests {
 [TestCase("PREPARED")][TestCase("RUNNING")][TestCase("NOT_APPLICABLE")]
 public void MissingReplayKeepsServerReviewPathReadable(string status){var data=Data();data["ordinaryInitialization"]=Initialization(status);data["startResponse"]=null;data["nextAction"]="REVIEW_REQUIRED";data["disposition"]="REVIEW_REQUIRED";var state=new RunLifecycleSnapshot(data.ToString(),"run_test");Assert.AreEqual("REVIEW_REQUIRED",state.NextAction);Assert.IsFalse(state.NewAttemptAuthorized);Assert.IsFalse(state.OrdinaryInitialization.MatchesOriginalStart);}
 [Test] public void MissingFieldRemainsUnknownAndUnavailableHasNoRevision(){var data=Data();Assert.IsNull(new RunLifecycleSnapshot(data.ToString(),"run_test").OrdinaryInitialization);data["ordinaryInitialization"]=Initialization("UNAVAILABLE");data["ordinaryInitialization"]["revision"]=3;Assert.Throws<FormatException>(()=>new RunLifecycleSnapshot(data.ToString(),"run_test"));}
 static JObject Data(){var type=typeof(LastRail.Tests.EditMode.RunLifecycleTests);return (JObject)type.GetMethod("Data",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"NOT_AVAILABLE"});}
 static JObject Initialization(string status)=>new JObject{["version"]="combat.ordinary-initialization-state.v1.20261025",["status"]=status,["revision"]=status=="PREPARED"?new JValue(1):status=="RUNNING"?new JValue(3):JValue.CreateNull(),["originHash"]=status=="PREPARED"||status=="RUNNING"?new string('a',64):null,["runtimeExecutionVerified"]=false};
 [TestCase("PREPARED")][TestCase("RUNNING")][TestCase("UNAVAILABLE")][TestCase("NOT_APPLICABLE")]
 public void ReadsStateWithoutAuthorizingExecution(string status){var data=Data();data["ordinaryInitialization"]=Initialization(status);if(status=="PREPARED"||status=="RUNNING")data["startResponse"]["ordinaryStatSource"]=new JObject{["originHash"]=new string('a',64),["origin"]=new JObject{["runId"]="run_test"}};
  var snapshot=new RunLifecycleSnapshot(data.ToString(),"run_test");var property=typeof(RunLifecycleSnapshot).GetProperty("OrdinaryInitialization");Assert.NotNull(property);var state=property.GetValue(snapshot);Assert.NotNull(state);Assert.AreEqual(status,state.GetType().GetProperty("Status").GetValue(state));Assert.IsFalse((bool)state.GetType().GetProperty("RuntimeExecutionVerified").GetValue(state));Assert.IsFalse(snapshot.NewAttemptAuthorized);Assert.AreEqual("RECOVER_COMBAT_CONSUMER",snapshot.NextAction);
 }
 [TestCase("execution")][TestCase("revision")][TestCase("hash")][TestCase("missingStart")]
 public void InvalidMetadataCannotBecomeReady(string kind){var data=Data();var state=Initialization("RUNNING");data["ordinaryInitialization"]=state;data["startResponse"]["ordinaryStatSource"]=new JObject{["originHash"]=new string('a',64),["origin"]=new JObject{["runId"]="run_test"}};if(kind=="execution")state["runtimeExecutionVerified"]=true;if(kind=="revision")state["revision"]=1;if(kind=="hash")state["originHash"]=new string('b',64);if(kind=="missingStart")((JObject)data["startResponse"]).Remove("ordinaryStatSource");Assert.Throws<FormatException>(()=>new RunLifecycleSnapshot(data.ToString(),"run_test"));}
}
