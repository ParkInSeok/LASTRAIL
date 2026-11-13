#if UNITY_EDITOR
using System;
using LastRail.Battle;
using UnityEditor;
using UnityEngine;
public static class CombatDistantInstall {
 public static void Run(){
  if(!Application.isBatchMode)throw new InvalidOperationException("Explicit QA batch only");
  const string root="Assets/_LASTRAIL/UI/ApprovedCombat/Canyon/";
  foreach(string name in new[]{"canyon-far-v01.png","canyon-base-v01.png"}){var importer=AssetImporter.GetAtPath(root+name) as TextureImporter;if(importer==null)throw new InvalidOperationException("Missing supplied canyon image");importer.textureType=TextureImporterType.Default;importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
  var assets=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");assets.DistantFar=AssetDatabase.LoadAssetAtPath<Texture2D>(root+"canyon-far-v01.png");assets.DistantTerrain=AssetDatabase.LoadAssetAtPath<Texture2D>(root+"canyon-base-v01.png");assets.DistantTerrainLayout=AssetDatabase.LoadAssetAtPath<TextAsset>(root+"terrain-layers.json");if(assets.DistantFar==null||assets.DistantTerrain==null||assets.DistantTerrainLayout==null)throw new Exception("Incomplete canyon supply");assets.ExteriorMap=assets.DistantTerrain;assets.MapId="canyon";EditorUtility.SetDirty(assets);AssetDatabase.SaveAssets();
 }
}
#endif
