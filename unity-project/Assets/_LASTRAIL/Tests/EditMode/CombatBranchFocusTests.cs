using LastRail.Battle;
using NUnit.Framework;
using UnityEngine;
public class CombatBranchFocusTests {
 [Test] public void BackgroundingCancelsUnreleasedLeverGesture(){var go=new GameObject("focus-lever",typeof(RectTransform));try{var lever=go.AddComponent<CombatBranchLever>();int commits=0;lever.Commit=()=>commits++;lever.BeginGesture(7);go.SendMessage("OnApplicationFocus",false,SendMessageOptions.DontRequireReceiver);go.SendMessage("OnApplicationFocus",true,SendMessageOptions.DontRequireReceiver);lever.EndGesture(7,true);Assert.AreEqual(0,commits);lever.BeginGesture(8);go.SendMessage("OnApplicationPause",true,SendMessageOptions.DontRequireReceiver);go.SendMessage("OnApplicationPause",false,SendMessageOptions.DontRequireReceiver);lever.EndGesture(8,true);Assert.AreEqual(0,commits);lever.BeginGesture(9);lever.EndGesture(9,true);Assert.AreEqual(1,commits);}finally{Object.DestroyImmediate(go);}}
}
