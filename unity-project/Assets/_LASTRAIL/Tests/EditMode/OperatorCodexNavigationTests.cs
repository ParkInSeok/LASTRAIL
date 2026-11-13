using System.Linq;
using LastRail.Presentation.MissionShell;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Tests.EditMode
{
    public sealed class OperatorCodexNavigationTests
    {
        [Test] public void ActualMenuTabsOperatorListAndReturnUseExistingIdentityWithoutAccountReads()
        {
            var root=new GameObject("CodexNavigation",typeof(RectTransform),typeof(Canvas));((RectTransform)root.transform).sizeDelta=new Vector2(720,1600);
            var safe=new GameObject("SafeArea",typeof(RectTransform));safe.transform.SetParent(root.transform,false);var sr=(RectTransform)safe.transform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.sizeDelta=Vector2.zero;
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var deck=LobbyCommandDeck.Attach(root,font,"QA",0,new MissionShellMissionRowState[0],null,null);
                void Click(string name)=>root.GetComponentsInChildren<Button>().Single(b=>b.name==name).onClick.Invoke();
                Click("LobbyMenu");Click("OpenOperatorCodex");var view=root.GetComponentInChildren<OperatorCodexView>();Assert.IsNotNull(view);
                foreach(var glyph in view.GetComponentsInChildren<CodexShieldSymbol>())Assert.IsNotNull(glyph.GetComponent<CanvasRenderer>(),"Symbol requires its own renderer to submit UI geometry.");
                Assert.That(view.CurrentState.Selected.Id,Is.EqualTo("op_kyle"));Click("CodexTab2");Assert.That(view.CurrentState.Body,Is.EqualTo(OperatorCodexEntry.Unknown));
                Click("CodexTab0");Assert.That(view.CurrentState.Tab,Is.EqualTo(OperatorCodexTab.Basic));Click("CodexTab1");Assert.That(view.CurrentState.Tab,Is.EqualTo(OperatorCodexTab.Skills));
                Click("BackOperatorCodex");Assert.IsTrue(view.CurrentState.IsList);Click("CodexOperator1");Assert.That(view.CurrentState.Selected.Id,Is.EqualTo("op_reina"));
                Click("BackOperatorCodex");Click("CodexListReturn");Assert.IsNull(root.GetComponentInChildren<OperatorCodexView>());
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
