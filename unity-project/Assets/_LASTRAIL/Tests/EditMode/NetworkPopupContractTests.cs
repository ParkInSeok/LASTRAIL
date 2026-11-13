using System.Reflection;
using LastRail.Presentation.Startup;
using NUnit.Framework;
namespace LastRail.Tests.EditMode {public sealed class NetworkPopupContractTests {
 sealed class UpdateDestination:LastRail.Resources.ITrustedUpdateDestination {public string PlayUrl=>"https://play.google.com/store/apps/details?id=fixture.update.test";}
 [TestCase(1080f)][TestCase(864f)] public void PopupFrameFitsAvailableWidthAndRestoresAuthScale(float width){
  var root=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab"));try{
   root.GetComponent<UnityEngine.UI.CanvasScaler>().enabled=false;root.GetComponent<UnityEngine.Canvas>().renderMode=UnityEngine.RenderMode.WorldSpace;((UnityEngine.RectTransform)root.transform).sizeDelta=new UnityEngine.Vector2(width,1920);
   var view=root.GetComponent<StartupScreenRootBehaviour>();UnityEngine.Canvas.ForceUpdateCanvases();
   foreach(var state in new[]{StartupScreenViewState.InternalAccessDenied(),StartupScreenViewState.NetworkInterrupted("연결 확인"),StartupScreenViewState.UpdateNotice("fixture","")}){
    view.Render(state);UnityEngine.Canvas.ForceUpdateCanvases();var band=(UnityEngine.RectTransform)root.transform.Find("SafeArea/StartupActionBand");var parent=(UnityEngine.RectTransform)band.parent;var corners=new UnityEngine.Vector3[4];band.GetWorldCorners(corners);
    foreach(var corner in corners){var point=parent.InverseTransformPoint(corner);Assert.That(point.x,Is.InRange(parent.rect.xMin+1,parent.rect.xMax-1),state.Kind.ToString());}
   }
   view.Render(StartupScreenViewState.Waiting());Assert.That(root.transform.Find("SafeArea/StartupActionBand").localScale,Is.EqualTo(UnityEngine.Vector3.one));
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 [TestCase("missing")][TestCase("failed")][TestCase("accepted")]
 public void UpdateActionNoticeIsFullyReadable(string outcome){
  var root=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab"));try{
   var destination=new UpdateDestination();var action=new LastRail.Resources.RequiredUpdateAction(outcome=="missing"?null:destination,new[]{destination.PlayUrl},_=>outcome=="accepted");action.OpenFromUserClick();
   var state=StartupScreenViewState.UpdateNotice("fixture",action.Notice);var view=root.GetComponent<StartupScreenRootBehaviour>();view.Render(state);UnityEngine.Canvas.ForceUpdateCanvases();
   var details=System.Linq.Enumerable.Single(root.GetComponentsInChildren<TMPro.TMP_Text>(true),t=>t.name=="StartupDetails");details.ForceMeshUpdate(true);
   Assert.That(details.isTextTruncated,Is.False,action.Notice);Assert.That(details.isTextOverflowing,Is.False,action.Notice);
   var followup=System.Linq.Enumerable.Single(root.GetComponentsInChildren<TMPro.TMP_Text>(true),t=>t.name=="PopupFollowup");followup.ForceMeshUpdate(true);
   Assert.That(details.text+"\n\n"+followup.text,Is.EqualTo(state.Details),"Complete notice must survive paragraph layout");
   Assert.That(followup.isTextTruncated,Is.False,action.Notice);Assert.That(followup.isTextOverflowing,Is.False,action.Notice);
   var primary=System.Linq.Enumerable.Single(root.GetComponentsInChildren<UnityEngine.UI.Button>(true),b=>b.name=="StartupPrimary");
   Assert.That(-details.rectTransform.anchoredPosition.y+details.rectTransform.rect.height,Is.LessThan(-((UnityEngine.RectTransform)primary.transform).anchoredPosition.y),"Notice must remain above update input");
   Assert.That(-followup.rectTransform.anchoredPosition.y-followup.textBounds.min.y,Is.LessThanOrEqualTo(-((UnityEngine.RectTransform)primary.transform).anchoredPosition.y-24),"Follow-up ink must clear update input");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 [Test] public void SubscriberRemovalAfterPopupChildDestructionIsSafe(){
  var root=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab"));try{
   var view=root.GetComponent<StartupScreenRootBehaviour>();view.Render(StartupScreenViewState.NetworkInterrupted("연결 확인"));System.Action callback=()=>{};view.PrimaryActionRequested+=callback;view.SecondaryActionRequested+=callback;
   var support=System.Linq.Enumerable.Single(root.GetComponentsInChildren<UnityEngine.UI.Button>(true),b=>b.name=="StartupSupport");UnityEngine.Object.DestroyImmediate(support.gameObject);
   Assert.DoesNotThrow(()=>{view.PrimaryActionRequested-=callback;view.SecondaryActionRequested-=callback;view.SetSupportAction(null,null);});
   Assert.DoesNotThrow(()=>typeof(StartupScreenRootBehaviour).GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,null));
   Assert.DoesNotThrow(()=>typeof(StartupScreenRootBehaviour).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,null));
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 [Test] public void NetworkSupportIsOwnedAndUnavailableWithoutAuthentication(){
  var root=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab"));try{
   var view=root.GetComponent<StartupScreenRootBehaviour>();view.Render(StartupScreenViewState.NetworkInterrupted("연결 확인"));var method=typeof(StartupScreenRootBehaviour).GetMethod("SetSupportAction");Assert.That(method,Is.Not.Null);
   int count=0;bool available=false;System.Action click=()=>count++;System.Func<bool> ready=()=>available;method.Invoke(view,new object[]{click,ready});
   var button=System.Linq.Enumerable.Single(root.GetComponentsInChildren<UnityEngine.UI.Button>(true),b=>b.name=="StartupSupport");Assert.That(button.interactable,Is.False);button.onClick.Invoke();Assert.That(count,Is.Zero);
   available=true;view.Render(StartupScreenViewState.NetworkInterrupted("연결 확인"));view.Render(StartupScreenViewState.NetworkInterrupted("연결 확인"));button.onClick.Invoke();Assert.That(count,Is.EqualTo(1));
   var separator=System.Linq.Enumerable.Single(root.GetComponentsInChildren<UnityEngine.UI.Image>(true),i=>i.name=="BodySeparator");Assert.That(separator.gameObject.activeInHierarchy,Is.True);
   root.SetActive(false);button.onClick.Invoke();Assert.That(count,Is.EqualTo(1));root.SetActive(true);view.Render(StartupScreenViewState.Waiting());Assert.That(button.gameObject.activeInHierarchy,Is.False);
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 [Test] public void NetworkFailureHasRetryAndExplicitLoginReturn(){var method=typeof(StartupScreenViewState).GetMethod("NetworkInterrupted",BindingFlags.Public|BindingFlags.Static);Assert.That(method,Is.Not.Null);var state=(StartupScreenViewState)method.Invoke(null,new object[]{"네트워크 상태를 확인한 뒤 다시 시도해 주세요."});Assert.That(state.Kind,Is.EqualTo(StartupScreenKind.DownloadError));Assert.That(state.PrimaryLabel,Is.EqualTo("다시 시도"));Assert.That(state.SecondaryLabel,Is.EqualTo("로그인 화면으로"));Assert.That(state.PhaseKey,Is.EqualTo("network_interrupted"));}
}}
