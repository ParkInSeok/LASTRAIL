using System.Linq;
using LastRail.Auth;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class PlayerEntryNavigationTests
    {
        GameObject root;
        PlayerEntryPanel panel;
        T Find<T>(string name) where T : Component => root.GetComponentsInChildren<T>(true).Single(x => x.name == name);
        [SetUp] public void Setup()
        {
            root = new GameObject("LoginNavigationFixture", typeof(RectTransform));
            panel = root.AddComponent<PlayerEntryPanel>();
            panel.Initialize(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset"));
            panel.ShowLogin(true, false);
        }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(root); }
        [Test] public void ChoiceStartsWithoutCredentialsAndBackClearsSecretWithoutAuthenticating()
        {
            int submitted = 0, cancelled = 0;
            panel.LoginSubmitted += (_, __) => submitted++;
            panel.CancelRequested += () => cancelled++;
            Assert.That(Find<TMP_InputField>("LoginId").gameObject.activeInHierarchy, Is.False);
            Find<Button>("ChooseIdLogin").onClick.Invoke();
            Assert.That(Find<TMP_InputField>("LoginId").gameObject.activeInHierarchy, Is.True);
            Assert.That(Find<Button>("Guest").gameObject.activeInHierarchy, Is.False);
            Find<TMP_InputField>("Password").text = "synthetic_secret";
            Find<Button>("BackToMethods").onClick.Invoke();
            Assert.That(Find<TMP_InputField>("Password").text, Is.Empty);
            Assert.That(Find<Button>("Guest").gameObject.activeInHierarchy, Is.True);
            Assert.That(submitted, Is.Zero); Assert.That(cancelled, Is.Zero);
        }
        [Test] public void ClosedGuestAndGoogleGatesSurviveRoundTrip()
        {
            panel.ShowLogin(false, false);
            Find<Button>("ChooseIdLogin").onClick.Invoke();
            Assert.That(Find<Button>("Register").interactable, Is.False);
            Find<Button>("BackToMethods").onClick.Invoke();
            Assert.That(Find<Button>("Guest").interactable, Is.False);
            Assert.That(Find<Button>("Google").interactable, Is.False);
        }
        [Test] public void CredentialSubmitRemainsSingleAndClearsPassword()
        {
            int submits = 0;
            panel.LoginSubmitted += (id, password) => { Assert.That(id, Is.EqualTo("fixture")); Assert.That(password, Is.EqualTo("synthetic_secret")); submits++; };
            Find<Button>("ChooseIdLogin").onClick.Invoke();
            Find<TMP_InputField>("LoginId").text = "fixture";
            Find<TMP_InputField>("Password").text = "synthetic_secret";
            Find<Button>("Login").onClick.Invoke();
            Assert.That(submits, Is.EqualTo(1));
            Assert.That(Find<TMP_InputField>("Password").text, Is.Empty);
        }
    }
}
