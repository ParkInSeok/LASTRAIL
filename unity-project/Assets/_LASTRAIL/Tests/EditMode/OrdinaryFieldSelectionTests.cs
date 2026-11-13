using System.Linq;
using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class OrdinaryFieldSelectionTests {
 [TestCase(false)][TestCase(true)]
 public void ExplicitContentSelectionPreservesExecutionAndOriginalCanyon(bool selected){
  var original=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/OrdinaryPresentation");
  var art=Object.Instantiate(original);var far=new Texture2D(1672,941);var track=new Texture2D(1672,941);
  var driver=CombatSceneDriver.ForOrdinaryNode(OrdinaryLiveExecutionTests.CreateFactory(OrdinaryLiveExecutionTests.CreateInput()),()=>true);driver.Open();
  var root=new GameObject("FieldSelection",typeof(RectTransform),typeof(Canvas));
  try{
   if(selected){
    var farField=typeof(CombatPresentationAssets).GetField("OrdinaryFieldBackground");
    var trackField=typeof(CombatPresentationAssets).GetField("OrdinaryFieldTrack");
    Assert.NotNull(farField,"Product content has no independent field binding.");Assert.NotNull(trackField);
    art.MapId="refinery-viaduct-v01";farField.SetValue(art,far);trackField.SetValue(art,track);
   }
   string state=driver.StateJson;
   var view=root.AddComponent<ApprovedCombatView>();view.Bind(driver,art);view.UseLandscapeLayout();view.Refresh();view.Refresh();
   var images=root.GetComponentsInChildren<RawImage>(true);
   Assert.AreEqual(selected?1:0,images.Count(i=>i.name=="OrdinaryFieldBackground"));
   Assert.AreEqual(selected?1:0,images.Count(i=>i.name=="OrdinaryFieldTrack"));
   if(selected){
    Assert.AreSame(far,images.Single(i=>i.name=="OrdinaryFieldBackground").texture);
    Assert.AreSame(track,images.Single(i=>i.name=="OrdinaryFieldTrack").texture);
    Assert.AreEqual(images.Single(i=>i.name=="OrdinaryFieldBackground").uvRect,images.Single(i=>i.name=="OrdinaryFieldTrack").uvRect);
   }
   Assert.AreEqual(state,driver.StateJson);Assert.AreEqual("canyon",original.MapId);
  }finally{Object.DestroyImmediate(root);driver.Retire();Object.DestroyImmediate(art);Object.DestroyImmediate(far);Object.DestroyImmediate(track);}
 }
}
