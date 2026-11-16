#if UNITY_EDITOR
using LastRail.Resources;
using UnityEditor;
namespace LastRail.Editor {
 public static class PreparedContentEditorAdapter {
  [InitializeOnLoadMethod] static void Register(){PreparedContent.EditorAsset=(path,type)=>AssetDatabase.LoadAssetAtPath(path,type);EditorApplication.playModeStateChanged-=StateChanged;EditorApplication.playModeStateChanged+=StateChanged;}
  static void StateChanged(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredEditMode)Register();else if(state==PlayModeStateChange.ExitingEditMode)PreparedContent.EditorAsset=null;}
  [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)] static void RegisterRuntime()=>PreparedContent.EditorAsset=null;
 }
}
#endif
