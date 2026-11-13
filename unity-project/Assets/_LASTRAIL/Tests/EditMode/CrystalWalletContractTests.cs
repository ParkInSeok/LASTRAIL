using System;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using LastRail.Presentation.MissionShell;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class CrystalWalletContractTests
    {
        private const string TypeName = "LastRail.Presentation.MissionShell.CrystalWalletRead, LastRail.Runtime";
        private static Type ReadType
        {
            get { var type = Type.GetType(TypeName); Assert.That(type, Is.Not.Null, "Actual wallet DTO reader is missing."); return type; }
        }
        private static object Parse(int code, string json) => ReadType.GetMethod("Parse").Invoke(null, new object[] { code, json });
        private static object Value(object result, string name) => result.GetType().GetProperty(name).GetValue(result);
        private static string Wallet() => new JObject { ["currencyId"] = "fixture-currency", ["policyVersion"] = "fixture-policy",
            ["freeBalance"] = 12, ["paidBalance"] = 3, ["totalBalance"] = 15,
            ["spendOrder"] = new JArray("FREE", "PAID"), ["ticketExchange"] = null, ["purchaseConfigured"] = false }.ToString();

        [Test]
        public void ReadsActualGetShapeWithoutInventingAcceptedOrDisplayNameFields()
        {
            var result = Parse(200, Wallet());
            Assert.That(Value(result, "Status").ToString(), Is.EqualTo("Ready"));
            Assert.That(Value(result, "CurrencyId"), Is.EqualTo("fixture-currency"));
            Assert.That(Value(result, "PolicyVersion"), Is.EqualTo("fixture-policy"));
            Assert.That(Value(result, "FreeBalance"), Is.EqualTo(12L));
            Assert.That(Value(result, "PaidBalance"), Is.EqualTo(3L));
            Assert.That(Value(result, "TotalBalance"), Is.EqualTo(15L));
        }

        [TestCase("freeBalance", "-1")]
        [TestCase("freeBalance", "2.5")]
        [TestCase("freeBalance", "9007199254740992")]
        [TestCase("totalBalance", "16")]
        public void InvalidOrInconsistentBalancesNeverBecomeDisplayValues(string field, string raw)
        {
            var json = JObject.Parse(Wallet()); json[field] = JToken.Parse(raw);
            Assert.That(Value(Parse(200, json.ToString()), "Status").ToString(), Is.EqualTo("Invalid"));
        }

        [Test]
        public void UnconfiguredResponseClearsStaleWalletAndConfirmation()
        {
            var modelType = Type.GetType("LastRail.Presentation.MissionShell.CrystalPolicyPresentation, LastRail.Runtime");
            var model = Activator.CreateInstance(modelType);
            modelType.GetMethod("SetWallet").Invoke(model, new object[] { "old", "15", "12", "3", Array.Empty<string>() });
            modelType.GetMethod("SetRecoveryQuote").Invoke(model, new object[] { "old", "1", "reward", "15", true });
            var result = Parse(503, "{\"accepted\":false,\"errorCode\":\"crystal_policy_unconfigured\"}");
            Assert.That(Value(result, "Status").ToString(), Is.EqualTo("Unconfigured"));
            modelType.GetMethod("ApplyWalletRead").Invoke(model, new[] { result });
            Assert.That(Value(model, "WalletAvailable"), Is.False);
            Assert.That(Value(model, "Total"), Is.Null);
            Assert.That(Value(model, "Cost"), Is.Null);
            Assert.That(Value(model, "CanConfirm"), Is.False);
        }

        [TestCase(401, "AuthRequired")]
        [TestCase(403, "Unavailable")]
        [TestCase(500, "Unavailable")]
        public void HttpFailuresNeverMasqueradeAsZeroBalance(int code, string state) =>
            Assert.That(Value(Parse(code, "{}"), "Status").ToString(), Is.EqualTo(state));

        [Test]
        public void NewWalletReadInvalidatesAQuoteBasedOnTheOldBalance()
        {
            var model = new CrystalPolicyPresentation();
            model.SetRecoveryQuote("old-quote", "1", "reward", "old-balance", true);
            model.ApplyWalletRead(CrystalWalletRead.Parse(200, Wallet()));
            Assert.That(model.WalletAvailable, Is.True);
            Assert.That(model.RecoveryStatus, Is.EqualTo(RecoveryConfirmationStatus.Unavailable));
            Assert.That(model.Cost, Is.Null);
        }

        [Test]
        public void PanelShowsUnconfiguredNoticeWithoutZeroOrStaleConfirmation()
        {
            var root = new GameObject("WalletDtoViewFixture");
            try
            {
                var view = root.AddComponent<CrystalPolicyPanel>();
                var labelObject = new GameObject("Wallet", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(root.transform, false);
                var label = labelObject.GetComponent<TMP_Text>();
                var buttonObject = new GameObject("Confirm", typeof(RectTransform), typeof(Button));
                buttonObject.transform.SetParent(root.transform, false);
                var button = buttonObject.GetComponent<Button>();
                typeof(CrystalPolicyPanel).GetField("walletText", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, label);
                typeof(CrystalPolicyPanel).GetField("confirmButton", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, button);
                view.ApplyWalletResponse(200, Wallet());
                Assert.That(label.text, Does.Contain("FREE 12").And.Contain("PAID 3"));
                view.ApplyWalletResponse(503, "{\"accepted\":false,\"errorCode\":\"crystal_policy_unconfigured\"}");
                Assert.That(label.text, Is.EqualTo("\uC7AC\uD654 \uC124\uC815\uC744 \uC900\uBE44 \uC911\uC785\uB2C8\uB2E4."));
                Assert.That(button.interactable, Is.False);
                Assert.That(label.richText, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase("PAID", true)]
        [TestCase("FREE", false)]
        public void PremiumPassRejectsPaidRewardsOrMissingPurchaseLink(string source, bool linked)
        {
            var json = new JObject { ["kind"] = "CRYSTAL", ["id"] = "fixture-currency", ["quantity"] = 2,
                ["source"] = source, ["purchaseLinked"] = linked }.ToString();
            Assert.Throws<InvalidOperationException>(() => CrystalWalletRead.ParseReward(json, "PREMIUM_PASS"));
        }

        [TestCase("ATTENDANCE", false)]
        [TestCase("FREE_PASS", false)]
        [TestCase("PREMIUM_PASS", true)]
        public void ActualCrystalRewardLinesPreserveFreeSourceAndOrderLink(string origin, bool linked)
        {
            var json = new JObject { ["kind"] = "CRYSTAL", ["id"] = "fixture-currency", ["quantity"] = 2,
                ["source"] = "FREE", ["purchaseLinked"] = linked }.ToString();
            var result = ReadType.GetMethod("ParseReward").Invoke(null, new object[] { json, origin });
            Assert.That(Value(result, "Source"), Is.EqualTo("FREE"));
            Assert.That(Value(result, "PurchaseLinked"), Is.EqualTo(linked));
            Assert.That(Value(result, "Quantity"), Is.EqualTo(2L));
        }
    }
}
