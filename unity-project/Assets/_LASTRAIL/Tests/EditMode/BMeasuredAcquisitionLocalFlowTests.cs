#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Battle;
using LastRail.Presentation.AugmentSelection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Tests.EditMode {
 public class BMeasuredAcquisitionLocalFlowTests {
  [Test] public void DisableHandlerCancelsOwnedAsyncLifetime(){
   var root=new GameObject("LOCAL disabled lifetime");var player=root.AddComponent<BMeasuredAcquisitionLocalPlayer>();var lifetime=new CancellationTokenSource();
   try{
    typeof(BMeasuredAcquisitionLocalPlayer).GetField("lifetime",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(player,lifetime);
    // EditMode does not dispatch runtime MonoBehaviour lifecycle messages.
    typeof(BMeasuredAcquisitionLocalPlayer).GetMethod("OnDisable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(player,null);
    Assert.IsTrue(lifetime.IsCancellationRequested);
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  [Test,Timeout(600000)] public async Task OriginalHealAndRecoveryNeverPromoteUnfinishedNativeNodeToResult(){
   using var wallLimit=new CancellationTokenSource(TimeSpan.FromMinutes(8));
   var envelope=(JObject)typeof(BMeasuredAcquisitionLocalSourceTests).GetMethod("Envelope").Invoke(null,null);
   var source=BMeasuredAcquisitionLocalSource.Read(envelope,BDirectAttackInput.Digest(envelope));
   string directory="D:/Project/LASTRAIL/LocalTemp/MeasuredAcquisitionFlowTests/"+Guid.NewGuid().ToString("N");
   var root=new GameObject("LOCAL measured selection test",typeof(RectTransform),typeof(Canvas));
   var screen=root.AddComponent<BRouteAugmentScreen>();
   var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
   try{
    using(var flow=new BMeasuredAcquisitionLocalFlow(source,screen,font,directory,(message,native)=>{})){
     await flow.OpenSelectionAsync(CancellationToken.None);
     screen.View.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Select0").onClick.Invoke();
     Assert.AreEqual("ACQUIRED_PENDING_COMBAT",screen.ArrivalState);
     await flow.CompleteAfterSelectionAsync(CancellationToken.None);
    }
    var evidence=JObject.Parse(File.ReadAllText(Path.Combine(directory,"evidence.json")));
    Assert.Greater((long)evidence["hit"]["lifesteal"]["actualHpGain"],0);
    Assert.AreEqual(1,(int)evidence["selectionCalls"]);
    using(var cancelled=new CancellationTokenSource())
    using(var interrupted=new BMeasuredAcquisitionLocalFlow(source,screen,font,directory,(message,native)=>{if(message.Contains("advancing to"))cancelled.Cancel();})){
     await interrupted.RestoreOnlyAsync(CancellationToken.None);
     var complete=typeof(BMeasuredAcquisitionLocalFlow).GetMethod("CompleteNodeResultAsync");
     Assert.IsNotNull(complete);
     try{await (Task<AugmentNodeResult>)complete.Invoke(interrupted,new object[]{cancelled.Token});Assert.Fail("Cancelled progression must stop");}
     catch(OperationCanceledException){}
    }
    using(var restart=new BMeasuredAcquisitionLocalFlow(source,screen,font,directory,(message,native)=>{})){
     await restart.RestoreOnlyAsync(CancellationToken.None);
     var complete=typeof(BMeasuredAcquisitionLocalFlow).GetMethod("CompleteNodeResultAsync");
     Assert.IsNotNull(complete,"Existing native flow must reach a committed node result, not stop after the first heal");
     try{await (Task<AugmentNodeResult>)complete.Invoke(restart,new object[]{wallLimit.Token});Assert.Fail("This original spatial source cannot reach terminal after all operators are down");}
     catch(NotSupportedException e){StringAssert.Contains("result not fabricated",e.Message);}
     var pending=JObject.Parse(File.ReadAllText(Path.Combine(directory,"node-result-pending.json")));
     Assert.AreEqual("RUNNING",(string)pending["outcome"]);Assert.AreEqual("ALL_SUPPLIED_OPERATORS_DOWN_NO_SUPPLIED_TERMINAL_ROUTE",(string)pending["reason"]);
     Assert.IsFalse(File.Exists(Path.Combine(directory,"node-result.json")),"No finalized result may be created from an unfinished original");
    }
    var restored=JObject.Parse(File.ReadAllText(Path.Combine(directory,"process-restart.json")));
    Assert.IsTrue((bool)restored["restoredIdentically"]);Assert.AreEqual("EXPLICIT_LOCAL_OWNER_RESTORE",(string)restored["classification"]);
    Assert.AreEqual((int)restored["originalProcessId"],(int)restored["restoreProcessId"],"An in-process test must not imply separate-process verification");
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
 }
}
#endif
