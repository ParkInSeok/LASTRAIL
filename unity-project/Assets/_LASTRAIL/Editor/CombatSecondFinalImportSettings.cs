#if UNITY_EDITOR
using UnityEditor;
public static class CombatSecondFinalImportSettings {
 public static void Apply(){
  var textures=new System.Collections.Generic.List<UnityEngine.Texture2D>();
  foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",Folders())){
   string path=AssetDatabase.GUIDToAssetPath(guid);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
   if(importer.npotScale!=TextureImporterNPOTScale.None||importer.maxTextureSize!=512){importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=512;importer.SaveAndReimport();}
   // Retain earlier imported Reina v01 on disk but bind only the selected v02.
   if(!path.EndsWith("ultimate_reina-v01.png",System.StringComparison.Ordinal))textures.Add(AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path));
  }
  textures.Sort((a,b)=>System.StringComparer.Ordinal.Compare(a.name,b.name));
  var root=AssetDatabase.LoadAssetAtPath<LastRail.Battle.CombatPresentationAssets>(LastRail.Resources.RemoteContentCatalog.AssetPath("CombatPresentation/approved"));root.SecondFinalResources=textures.ToArray();EditorUtility.SetDirty(root);AssetDatabase.SaveAssets();
 }
 public static void ApplyThenCapture(){
  foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",Folders())){
   var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
   if(importer.npotScale!=TextureImporterNPOTScale.None){importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();}
  }
  CombatKyleGrantCapture.Run();
 }
 static string[] Folders()=>System.Array.FindAll(new[]{"Assets/_LASTRAIL/Resources/CombatSecondFinal","Assets/_LASTRAIL/Content/RemoteResources/CombatSecondFinal"},AssetDatabase.IsValidFolder);
}
#endif
