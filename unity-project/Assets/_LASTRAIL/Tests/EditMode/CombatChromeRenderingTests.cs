using System.Linq;
using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
public class CombatChromeRenderingTests {
 [Test] public void AllCustomFramesAndWaveTrackHaveActualCanvasRendererAndMesh(){
  var driver=new CombatSceneDriver(CombatPresentationConnectionTests.Fresh,()=>null,(s,r)=>r+1,()=>true);driver.Open();var go=new GameObject("chrome-render",typeof(RectTransform),typeof(Canvas));
  try{var view=go.AddComponent<ApprovedCombatView>();view.Bind(driver,LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved"));Canvas.ForceUpdateCanvases();var frames=go.GetComponentsInChildren<CombatPanelChrome>();Assert.AreEqual(10,frames.Length);
   foreach(var graphic in frames.Cast<UnityEngine.UI.Graphic>().Concat(go.GetComponentsInChildren<CombatWaveTrack>())){Assert.IsNotNull(graphic.canvasRenderer,graphic.name+" cannot render with a RectTransform alone");var submitted=graphic.canvasRenderer.GetMesh();Assert.IsNotNull(submitted);Assert.Greater(submitted.vertexCount,0,graphic.name+" must have submitted geometry");}
  }finally{Object.DestroyImmediate(go);}
 }
}
