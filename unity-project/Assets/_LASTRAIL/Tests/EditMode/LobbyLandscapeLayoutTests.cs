using NUnit.Framework;
using System.Linq;
using LastRail.Presentation.MissionShell;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Tests.EditMode
{
    public sealed class LobbyLandscapeLayoutTests
    {
        [TestCase(LobbyPage.Operators)]
        [TestCase(LobbyPage.Shop)]
        public void SyntheticCardArtNamesAndActionsStayTogetherBesideNavigation(LobbyPage page)
        {
            var root=new GameObject("SyntheticLandscapeRows",typeof(RectTransform));
            ((RectTransform)root.transform).sizeDelta=new Vector2(1600,900);
            try
            {
                var view=LobbyDestinationPages.Create(root.transform,TMP_Settings.defaultFontAsset,()=>{});
                view.Open(page,true);
                var all=view.GetComponentsInChildren<RectTransform>();
                var positions=all.Select(x=>x.anchoredPosition).ToArray();
                var scales=all.Select(x=>x.localScale).ToArray();
                for(int repeat=0;repeat<3;repeat++)
                    typeof(LobbyDestinationPages).GetMethod("Fit",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(view,null);
                CollectionAssert.AreEqual(positions,all.Select(x=>x.anchoredPosition).ToArray());
                CollectionAssert.AreEqual(scales,all.Select(x=>x.localScale).ToArray());
                Rect Bounds(RectTransform rect)
                {
                    var corners=new Vector3[4];rect.GetWorldCorners(corners);
                    return Rect.MinMaxRect(corners.Min(c=>c.x),corners.Min(c=>c.y),corners.Max(c=>c.x),corners.Max(c=>c.y));
                }
                var navigation=Bounds(all.Single(x=>x.name=="PageNav0"));
                var names=all.Where(x=>x.name==(page==LobbyPage.Operators?"MemberName":"ProductName")).ToArray();
                int count=page==LobbyPage.Operators?9:6;
                Assert.That(names.Length,Is.EqualTo(count));
                for(int i=0;i<count;i++)
                {
                    var card=Bounds(all.Single(x=>x.name==(page==LobbyPage.Operators?"MemberCard":"Product")+i));
                    var name=Bounds(names[i]);
                    var action=Bounds(all.Single(x=>x.name==(page==LobbyPage.Operators?"SelectMember":"Buy")+i));
                    Assert.That(card.xMin,Is.GreaterThan(navigation.xMax),"Card "+i+" must stay beside navigation.");
                    Assert.That(name.xMin,Is.GreaterThanOrEqualTo(card.xMin-.1f));
                    Assert.That(name.xMax,Is.LessThanOrEqualTo(card.xMax+.1f));
                    Assert.That(name.yMin,Is.GreaterThanOrEqualTo(card.yMin-.1f),"Name must stay on its own card.");
                    Assert.That(name.yMax,Is.LessThanOrEqualTo(card.yMax+.1f));
                    Assert.That(action.yMin,Is.GreaterThanOrEqualTo(card.yMin-.1f));
                    Assert.That(action.yMax,Is.LessThanOrEqualTo(card.yMax+.1f));
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void AttendanceUsesSeparateCalendarAndStatusColumnsWithoutCumulativeLayout()
        {
            var root=new GameObject("Landscape",typeof(RectTransform));
            ((RectTransform)root.transform).sizeDelta=new Vector2(1600,900);
            try
            {
                var view=MonthlyAttendanceView.Create(root.transform,TMP_Settings.defaultFontAsset);
                view.SetViewportHeight(900);
                var cell=(RectTransform)view.transform.Find("MonthlyCell28");
                var status=(RectTransform)view.transform.Find("MonthlyStatusPanel");
                Assert.That(cell.anchoredPosition.x,Is.GreaterThan(status.anchoredPosition.x+status.rect.width*status.localScale.x));
                var position=cell.anchoredPosition;
                for(int i=0;i<3;i++)view.SetViewportHeight(900);
                Assert.That(cell.anchoredPosition,Is.EqualTo(position));
                Assert.That(-cell.anchoredPosition.y+cell.rect.height*cell.localScale.y,Is.LessThanOrEqualTo(900));
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void PassKeepsScrollableRewardListBesideSummaryAndClaim()
        {
            var root=new GameObject("Landscape",typeof(RectTransform));
            ((RectTransform)root.transform).sizeDelta=new Vector2(1600,900);
            try
            {
                var view=SeasonPassView.Create(root.transform,TMP_Settings.defaultFontAsset);
                view.SetViewportHeight(900);
                var viewport=(RectTransform)view.transform.Find("PassViewport");
                var claim=(RectTransform)view.transform.Find("PassBulk");
                Assert.That(viewport.GetComponent<ScrollRect>().content,Is.Not.Null);
                Assert.That(viewport.anchoredPosition.x,Is.GreaterThan(claim.anchoredPosition.x+claim.rect.width*claim.localScale.x));
                Assert.That(-viewport.anchoredPosition.y+viewport.rect.height,Is.LessThan(900));
                Assert.That(-claim.anchoredPosition.y+claim.rect.height*claim.localScale.y,Is.LessThan(900));
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
