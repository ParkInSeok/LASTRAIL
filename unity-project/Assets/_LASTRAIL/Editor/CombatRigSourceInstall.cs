#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
public static class CombatRigSourceInstall {
 public static void Run(){
  if(!Application.isBatchMode)throw new InvalidOperationException("Explicit diagnostic source import only");
  foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/_LASTRAIL/UI/Diagnostics"})){
   var path=AssetDatabase.GUIDToAssetPath(guid);if(!(AssetImporter.GetAtPath(path) is TextureImporter importer))continue;
   bool mip=Array.IndexOf(Environment.GetCommandLineArgs(),"-rigMip")>=0;
   importer.textureType=TextureImporterType.Default;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=mip;importer.filterMode=mip?FilterMode.Trilinear:FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=2048;importer.SaveAndReimport();
  }
 }
}
#endif
