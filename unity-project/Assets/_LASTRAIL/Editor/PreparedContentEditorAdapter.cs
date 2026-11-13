#if UNITY_EDITOR
using LastRail.Resources;
using UnityEditor;
namespace LastRail.Editor {
 public static class PreparedContentEditorAdapter {
  [InitializeOnLoadMethod] static void Register()=>PreparedContent.EditorAsset=(path,type)=>AssetDatabase.LoadAssetAtPath(path,type);
  [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)] static void RegisterRuntime()=>Register();
 }
}
#endif
