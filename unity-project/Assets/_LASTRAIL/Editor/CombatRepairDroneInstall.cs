#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
public static class CombatRepairDroneInstall {
 public static void Run(){foreach(var file in new[]{"parts-source-v01.png","weld-fx-source-v01.png"}){var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/_LASTRAIL/Content/RemoteResources/CombatPresentation/RepairDrone/"+file);importer.textureType=TextureImporterType.Default;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.alphaSource=TextureImporterAlphaSource.None;importer.mipmapEnabled=file.StartsWith("weld");importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();}AssetDatabase.SaveAssets();}
}
#endif
