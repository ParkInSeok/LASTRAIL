using System;
using System.Reflection;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class CrystalPolicyPresentationTests
    {
        private static Type ModelType => Type.GetType("LastRail.Presentation.MissionShell.CrystalPolicyPresentation, LastRail.Runtime");
        private static object Model() { Assert.That(ModelType, Is.Not.Null); return Activator.CreateInstance(ModelType); }
        private static object Call(object model, string name, params object[] args) => ModelType.GetMethod(name).Invoke(model, args);
        private static object Value(object model, string name) => ModelType.GetProperty(name).GetValue(model);

        [Test]
        public void TmpViewDisplaysAuthoritativeValuesAndDisablesUnboundActions()
        {
            var type = Type.GetType("LastRail.Presentation.MissionShell.CrystalPolicyPanel, LastRail.Runtime");
            Assert.That(type, Is.Not.Null, "TMP policy view is missing.");
            var root = new UnityEngine.GameObject("PolicyViewFixture");
            try
            {
                var view = root.AddComponent(type);
                foreach (var name in new[] { "walletText", "historyText", "recoveryText" })
                {
                    var child = new UnityEngine.GameObject(name, typeof(UnityEngine.RectTransform), typeof(TMPro.TextMeshProUGUI));
                    child.transform.SetParent(root.transform, false);
                    type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, child.GetComponent<TMPro.TMP_Text>());
                }
                var buttonObject = new UnityEngine.GameObject("Confirm", typeof(UnityEngine.RectTransform), typeof(UnityEngine.UI.Button));
                buttonObject.transform.SetParent(root.transform, false);
                var button = buttonObject.GetComponent<UnityEngine.UI.Button>();
                type.GetField("confirmButton", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, button);
                var model = Model();
                Call(model, "SetWallet", "fixture currency", "fixture total", "fixture free", "fixture paid", new[] { "receipt FREE" });
                Call(model, "SetRecoveryQuote", "fixture", "fixture cost", "fixture reward", "fixture balance", true);
                type.GetMethod("Bind").Invoke(view, new[] { model });
                var text = (TMPro.TMP_Text)type.GetField("walletText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
                Assert.That(text.text, Does.Contain("fixture total").And.Contain("fixture free").And.Contain("fixture paid"));
                Assert.That(text.richText, Is.False);
                Assert.That(button.interactable, Is.False, "No financial callback is installed.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void MissingContractNeverEnablesConfirmation()
        {
            var model = Model();
            Assert.That(Value(model, "WalletAvailable"), Is.False);
            Assert.That(Value(model, "CanConfirm"), Is.False);
            Assert.That(Call(model, "Confirm"), Is.EqualTo(0L));
        }

        [Test]
        public void RefreshFailureSettlesAndLateRefreshCannotReopenResetView()
        {
            var model = Model(); long token = 0; var requests = 0;
            Assert.That(ModelType.GetMethod("FailRefresh"), Is.Not.Null);
            ModelType.GetEvent("RefreshRequested").AddEventHandler(model, new Action<long>(value => { token = value; requests++; }));
            Call(model, "RequestRefresh"); Call(model, "RequestRefresh");
            Assert.That(requests, Is.EqualTo(1));
            Call(model, "FailRefresh", token);
            Assert.That(Value(model, "RecoveryStatus").ToString(), Is.EqualTo("Error"));
            Call(model, "RequestRefresh"); Call(model, "Reset");
            Assert.That(Call(model, "ApplyRefreshedQuote", token, "old", "cost", "reward", "balance", true), Is.False);
            Assert.That(Value(model, "RecoveryStatus").ToString(), Is.EqualTo("Unavailable"));
        }

        [Test]
        public void WalletDisplaysSuppliedTotalAndKeepsHistoryCopy()
        {
            var model = Model(); var history = new[] { "fixture receipt FREE" };
            Call(model, "SetWallet", "fixture currency", "server total", "server free", "server paid", history);
            history[0] = "changed";
            Assert.That(Value(model, "Total"), Is.EqualTo("server total"));
            Assert.That(Value(model, "Free"), Is.EqualTo("server free"));
            Assert.That(Value(model, "Paid"), Is.EqualTo("server paid"));
            Assert.That((System.Collections.IEnumerable)Value(model, "History"), Does.Contain("fixture receipt FREE"));
        }

        [TestCase("ATTENDANCE")] [TestCase("FREE_PASS")] [TestCase("PREMIUM_PASS")]
        public void ApprovedRewardOriginsAreFree(string origin)
        {
            Model();
            Assert.That(ModelType.GetMethod("RewardSourceLabel").Invoke(null, new object[] { origin }), Is.EqualTo("FREE"));
        }

        [Test]
        public void ConfirmationIsSingleFlightAndStaleResponsesAreIgnored()
        {
            var model = Model(); var sends = 0;
            Action<long, string> callback = (attempt, selection) => sends++;
            ModelType.GetEvent("ConfirmRequested").AddEventHandler(model, callback);
            Call(model, "SetRecoveryQuote", "fixture selection", "server cost", "server reward", "server balance", true);
            var first = (long)Call(model, "Confirm");
            Assert.That(first, Is.GreaterThan(0));
            Assert.That(Call(model, "Confirm"), Is.EqualTo(0L));
            Assert.That(sends, Is.EqualTo(1));
            Call(model, "Reset");
            Call(model, "Resolve", first, true);
            Assert.That(Value(model, "CanConfirm"), Is.False);
            Assert.That(Value(model, "RecoveryStatus").ToString(), Is.EqualTo("Unavailable"));
        }

        [Test]
        public void UncertainFailureRequiresFreshServerQuoteBeforeAnotherSubmit()
        {
            var model = Model();
            ModelType.GetEvent("ConfirmRequested").AddEventHandler(model, new Action<long, string>((a, s) => { }));
            Call(model, "SetRecoveryQuote", "fixture", "cost", "reward", "balance", true);
            var attempt = (long)Call(model, "Confirm");
            Call(model, "Resolve", attempt, false);
            Assert.That(Value(model, "RecoveryStatus").ToString(), Is.EqualTo("Error"));
            Assert.That(Call(model, "Confirm"), Is.EqualTo(0L));
        }
    }
}
