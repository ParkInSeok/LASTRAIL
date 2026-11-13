using System;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {
 public sealed class RewardOpportunityContractTests {
  // TEST_ONLY transformations follow the server's explicit v2 nullable-context pin.
  internal static JObject V2(JObject source,bool combat,bool purchase=true){
   var o=(JObject)source.DeepClone();o["contractVersion"]="reward-opportunity-v2";
   o["approval"]=new JObject{["cause"]=purchase?"PURCHASE_ENTITLEMENT":"AD_SSV",["approvedAt"]=(long)o["createdAt"]+1};
   o["adContext"]=purchase?null:new JObject{["userId"]=o["userId"],["customData"]=o["customData"],["adUnit"]=o["adUnit"],["expiresAt"]=o["expiresAt"],["callbackDeadline"]=combat?o["callbackDeadline"]:(long)o["expiresAt"]+1};
   o.Remove("userId");o.Remove("customData");o.Remove("adUnit");
   if(purchase){o["expiresAt"]=null;if(combat)o["callbackDeadline"]=null;}
   if(!combat)o["baseRewardLines"]=JArray.Parse("[{\"id\":\"cur_credits\",\"quantity\":120}]");return o;
  }
  [Test] public void PurchaseOfflineHasNoFakeAdContextAndPreservesReceipt(){var o=V2(JObject.Parse(OfflineAdContractTests.Sample("verified")),false);var parsed=OfflineAdIntent.Parse(o.ToString());Assert.That(parsed.CanSpin,Is.True);Assert.That(parsed.UserId,Is.Null);Assert.That(parsed.AdUnit,Is.Null);var result=JObject.Parse(OfflineAdContractTests.Sample("claimed-restored"))["result"];Assert.That(parsed.WithResult(result.ToString()).Status,Is.EqualTo("CLAIMED"));}
  [Test] public void PurchaseCombatHasNoAdClockButRetainsServerActions(){var o=V2(CombatAdControllerTests.Sample("AD_VERIFIED"),true);var parsed=CombatAdIntent.Parse(o.ToString(),"run_test","preview_test");Assert.That(parsed.CanSpin,Is.True);Assert.That(parsed.CustomData,Is.Null);Assert.That(parsed.WithResult(CombatAdControllerTests.Result().ToString()).Status,Is.EqualTo("CLAIMED"));}
  [Test] public void LegacyUpgradePreservesChallengeAndRejectsApprovalRewrite(){var old=CombatAdIntent.Parse(CombatAdControllerTests.Sample("AD_VERIFIED").ToString(),"run_test","preview_test");var o=V2(CombatAdControllerTests.Sample("AD_VERIFIED"),true,false);var upgraded=CombatAdIntent.Parse(o.ToString(),"run_test","preview_test");Assert.DoesNotThrow(()=>upgraded.RequireSame(old));o["approval"]["cause"]="PURCHASE_ENTITLEMENT";Assert.Throws<FormatException>(()=>CombatAdIntent.Parse(o.ToString(),"run_test","preview_test").RequireSame(upgraded));}
  [TestCase("approval")][TestCase("adContext")][TestCase("contractVersion")]
  public void IncompleteV2CannotGrantSpin(string field){var o=V2(CombatAdControllerTests.Sample("AD_VERIFIED"),true);o.Remove(field);Assert.Throws<FormatException>(()=>CombatAdIntent.Parse(o.ToString(),"run_test","preview_test"));}
  [Test] public void PaidIntentCannotDowngradeOrChangePinnedBase(){var o=V2(JObject.Parse(OfflineAdContractTests.Sample("verified")),false);var old=OfflineAdIntent.Parse(o.ToString());o["baseRewardLines"][0]["quantity"]=121;Assert.Throws<FormatException>(()=>OfflineAdIntent.Parse(o.ToString()).RequireSame(old));o["contractVersion"]="reward-opportunity-v3";Assert.Throws<FormatException>(()=>OfflineAdIntent.Parse(o.ToString()));}
  [Test] public void PresentBaseMetadataMustBeWellTyped(){var o=JObject.Parse(AdSkipUseTests.Sample("08-offline-approved"));o["baseRewardLines"][0]["kind"]=new JObject();Assert.Throws<FormatException>(()=>OfflineAdIntent.Parse(o.ToString()));}
 }
}
