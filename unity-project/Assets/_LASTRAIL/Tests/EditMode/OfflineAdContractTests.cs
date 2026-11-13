using System;
using System.IO;
using System.Reflection;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class OfflineAdContractTests {
  internal static string Sample(string name)=>File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"../QA/Unity1Artifacts/Reports/Server2-AdSamples-20260914/DisplayMultipliers01/"+name+".json"));
  static object Parse(string json){var type=typeof(AccountScreenRuntime).Assembly.GetType("LastRail.Presentation.MissionShell.OfflineAdIntent");Assert.That(type,Is.Not.Null,"Strict ad contract is required");return type.GetMethod("Parse",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{json});}
  [TestCase("pending")][TestCase("verified")][TestCase("claimed-restored")]
  public void FrozenServerIntentsParse(string name){Assert.That(Parse(Sample(name)),Is.Not.Null);}
  [TestCase("canSpin")][TestCase("displayMultipliers")][TestCase("policyVersion")]
  public void MissingRequiredStateIsRejected(string field){var data=JObject.Parse(Sample("verified"));data.Remove(field);Assert.Throws<TargetInvocationException>(()=>Parse(data.ToString()));}
  [Test] public void PendingCannotEnableSpin(){var data=JObject.Parse(Sample("pending"));data["canSpin"]=true;Assert.Throws<TargetInvocationException>(()=>Parse(data.ToString()));}
  [Test] public void ClaimedMultiplierMustBelongToPinnedPolicy(){var data=JObject.Parse(Sample("claimed-restored"));data["result"]["multiplier"]=3;Assert.Throws<TargetInvocationException>(()=>Parse(data.ToString()));}
  static JObject Weighted(){var data=JObject.Parse(Sample("verified"));data["displayMultipliers"]=new JArray(2,3,4);data["probabilities"]=JArray.Parse("[{\"multiplier\":2,\"weightBps\":6123},{\"multiplier\":3,\"weightBps\":2500},{\"multiplier\":4,\"weightBps\":1377}]");return data;}
  static string ProbabilityText(OfflineAdIntent intent){var property=typeof(OfflineAdIntent).GetProperty("ProbabilityText");Assert.That(property,Is.Not.Null,"Intent must expose validated probability display text");return (string)property.GetValue(intent);}
  [Test] public void LegacyJournalKeepsExplicitUnavailableProbability(){Assert.That(ProbabilityText(OfflineAdIntent.Parse(Sample("verified"))),Does.Contain("제공되지 않습니다"));}
  [Test] public void PinnedWeightsDisplayExactPercentagesIndependentOfCulture(){var old=System.Globalization.CultureInfo.CurrentCulture;try{System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.GetCultureInfo("fr-FR");var text=ProbabilityText(OfflineAdIntent.Parse(Weighted().ToString()));Assert.That(text,Does.Contain("2배  61.23%"));Assert.That(text,Does.Contain("3배  25%"));Assert.That(text,Does.Contain("4배  13.77%"));}finally{System.Globalization.CultureInfo.CurrentCulture=old;}}
  [TestCase("null")][TestCase("{}")][TestCase("[]")]
  [TestCase("[{\"multiplier\":2,\"weightBps\":10000},{\"multiplier\":2,\"weightBps\":0}]")]
  [TestCase("[{\"multiplier\":2,\"weightBps\":9999}]")]
  [TestCase("[{\"multiplier\":2,\"weightBps\":10001},{\"multiplier\":3,\"weightBps\":-1}]")]
  [TestCase("[{\"multiplier\":2,\"weightBps\":10000.0}]")]
  [TestCase("[{\"multiplier\":2.0,\"weightBps\":10000}]")]
  [TestCase("[{\"multiplier\":5,\"weightBps\":10000}]")]
  [TestCase("[{\"multiplier\":2,\"weightBps\":\"10000\"}]")]
  [TestCase("[{\"multiplier\":2}]")][TestCase("[null]")]
  public void MalformedPresentProbabilityIsRejected(string json){var data=JObject.Parse(Sample("verified"));data["probabilities"]=JToken.Parse(json);Assert.Throws<FormatException>(()=>OfflineAdIntent.Parse(data.ToString()));}
  [TestCase("[{\"multiplier\":2,\"weightBps\":10000}]")]
  [TestCase("[{\"multiplier\":2,\"weightBps\":10000},{\"multiplier\":3,\"weightBps\":0}]")]
  public void SparseProbabilityPolicyIsRejectedEvenWhenSupportMatches(string json){var data=JObject.Parse(Sample("verified"));data["probabilities"]=JToken.Parse(json);Assert.Throws<FormatException>(()=>OfflineAdIntent.Parse(data.ToString()));}
  [Test] public void PositiveProbabilitySupportMustMatchDisplayMultipliers(){var data=Weighted();data["displayMultipliers"]=new JArray(2,4);Assert.Throws<FormatException>(()=>OfflineAdIntent.Parse(data.ToString()));}
  [Test] public void ZeroWeightOutcomeIsNotDisplayedAsPossible(){var data=Weighted();data["displayMultipliers"]=new JArray(2,4);data["probabilities"][0]["weightBps"]=8623;data["probabilities"][1]["weightBps"]=0;var intent=OfflineAdIntent.Parse(data.ToString());CollectionAssert.AreEqual(new[]{2,4},intent.Multipliers);Assert.That(ProbabilityText(intent),Does.Contain("3배  0%"));}
  [Test] public void KnownProbabilityCannotChangeOrDisappearOnRefresh(){var original=OfflineAdIntent.Parse(Weighted().ToString());var changed=Weighted();changed["probabilities"][0]["weightBps"]=6124;changed["probabilities"][2]["weightBps"]=1376;Assert.Throws<FormatException>(()=>OfflineAdIntent.Parse(changed.ToString()).RequireSame(original));changed.Remove("probabilities");Assert.Throws<FormatException>(()=>OfflineAdIntent.Parse(changed.ToString()).RequireSame(original));}
  [Test] public void LegacyRefreshCanAcquireProbabilitiesAndRepeatedRefreshIsStable(){var data=Weighted();var known=OfflineAdIntent.Parse(data.ToString());data.Remove("probabilities");Assert.DoesNotThrow(()=>known.RequireSame(OfflineAdIntent.Parse(data.ToString())));Assert.DoesNotThrow(()=>OfflineAdIntent.Parse(known.Json).RequireSame(known));}
  [Test] public void ProbabilityOrderIsNotPolicyChange(){var data=Weighted();var original=OfflineAdIntent.Parse(data.ToString());var rows=(JArray)data["probabilities"];var first=rows[0];first.Remove();rows.Add(first);Assert.DoesNotThrow(()=>OfflineAdIntent.Parse(data.ToString()).RequireSame(original));}
  [Test] public void SpinResultPreservesPinnedWeightsAndExternalCopiesCannotMutateThem(){var data=Weighted();var intent=OfflineAdIntent.Parse(data.ToString());data["probabilities"][0]["weightBps"]=0;var exported=JObject.Parse(intent.Json);exported["probabilities"][0]["weightBps"]=0;var result=(JObject)JObject.Parse(Sample("claimed-restored"))["result"];result["intentId"]=intent.Id;result["policyVersion"]=intent.Policy;var claimed=intent.WithResult(result.ToString());Assert.That(ProbabilityText(claimed),Does.Contain("2배  61.23%"));Assert.That(JToken.DeepEquals(JObject.Parse(claimed.Json)["probabilities"],JObject.Parse(intent.Json)["probabilities"]),Is.True);}
 }
}
