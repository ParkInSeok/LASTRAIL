using NUnit.Framework;
using LastRail.Presentation.Startup;
using LastRail.Presentation.MissionShell;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
namespace LastRail.Tests.EditMode
{
    public class LobbyCommandDeckTests
    {
        [Test]
        public void ApprovedLobbyHasSeparateEnvironmentalPresentation()
        {
            Assert.That(typeof(SharedRailMap).Assembly.GetType("LastRail.Presentation.MissionShell.LobbyCommandDeck"), Is.Not.Null,
                "Approved command desk must exist as a separate presentation, not animate the interactive canvas.");
        }
        [TestCase(720,1280)]
        [TestCase(1080,2400)]
        [TestCase(1600,900)]
        public void MotionLeavesControlsFixedAndReducedMotionStopsIt(int width,int height)
        {
            var root=new GameObject("LobbyTest",typeof(RectTransform));
            ((RectTransform)root.transform).sizeDelta=new Vector2(width,height);
            var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);
            var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
            try
            {
                var deck=LobbyCommandDeck.Attach(root,TMP_Settings.defaultFontAsset,"QA",20,new MissionShellMissionRowState[0],null,null);
                var button=deck.GetComponentsInChildren<Button>().Single(x=>x.name=="Departure");
                var before=button.transform.position;
                for(int i=0;i<120;i++)deck.Motion.Evaluate(.05f);
                Assert.That(button.transform.position,Is.EqualTo(before));
                Assert.That(deck.Motion.Art.anchoredPosition.magnitude,Is.LessThan(5));
                Assert.That(deck.Motion.Clock,Is.GreaterThan(5));
                deck.SetReducedMotion(true);float clock=deck.Motion.Clock;deck.Motion.Evaluate(2);
                Assert.That(deck.Motion.Art.anchoredPosition,Is.EqualTo(Vector2.zero));
                Assert.That(deck.Motion.Clock,Is.EqualTo(clock));
                var shortcuts=deck.GetComponentsInChildren<Button>().Where(x=>x.name.StartsWith("Shortcut")).OrderBy(x=>x.name).ToArray();
                Assert.That(shortcuts.Length,Is.EqualTo(4));
                for(int i=1;i<4;i++){Assert.That(shortcuts[i].transform.position.x,Is.EqualTo(shortcuts[0].transform.position.x));Assert.That(shortcuts[i].transform.position.y,Is.LessThan(shortcuts[i-1].transform.position.y));}
                button.onClick.Invoke();Assert.That(deck.IsDialogOpen,Is.True);
                deck.CloseDialog();Assert.That(deck.IsDialogOpen,Is.False);
                deck.gameObject.SetActive(false);Assert.That(deck.Motion.gameObject.activeSelf,Is.False);
                deck.gameObject.SetActive(true);Assert.That(deck.Motion.gameObject.activeSelf,Is.True);
                Assert.That(deck.GetComponentsInChildren<TMP_Text>().Where(x=>x.name=="Balance").All(x=>x.text=="—"),Is.True);
                var slots=deck.GetComponentsInChildren<TMP_Text>().Where(x=>x.name=="EmptySlot").ToArray();
                Assert.That(slots.Length,Is.EqualTo(5));
                Assert.That(slots.All(x=>x.text=="+"),Is.True);
                for(int i=0;i<slots.Length;i++)
                {
                    var portrait=deck.GetComponentsInChildren<RawImage>().Single(x=>x.name=="SquadPortrait"+i);
                    Assert.That(slots[i].rectTransform.position,Is.EqualTo(portrait.rectTransform.position));
                    Assert.That(slots[i].rectTransform.sizeDelta,Is.EqualTo(portrait.rectTransform.sizeDelta));
                }
                LobbyCommandDeck.Attach(root,TMP_Settings.defaultFontAsset,"Updated",24,new MissionShellMissionRowState[0],null,null);
                Assert.That(deck.GetComponentsInChildren<TMP_Text>().Single(x=>x.name=="PlayerProfileName").text,Is.EqualTo("Updated"));
                deck.OpenOperations(); // Parent destruction must clean an open dialog without a double destroy.
            } finally {Object.DestroyImmediate(root);}
        }
    }
}
