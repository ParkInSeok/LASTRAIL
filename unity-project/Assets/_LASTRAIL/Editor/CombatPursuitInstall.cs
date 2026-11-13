#if UNITY_EDITOR
using System;
using LastRail.Battle;
using UnityEditor;
using UnityEngine;
public static class CombatPursuitInstall {
 public static void Run(){
  if(!Application.isBatchMode)throw new InvalidOperationException("Explicit local batch only");
  const string root="Assets/_LASTRAIL/UI/ApprovedCombat/Pursuit/";
  foreach(string name in new[]{"road-surface-v01.png","mid-ridge-v01.png","landmarks-v01.png"}){
   var importer=AssetImporter.GetAtPath(root+name) as TextureImporter;if(importer==null)throw new Exception("Missing supplied asset: "+name);
   importer.textureType=TextureImporterType.Default;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.mipmapEnabled=false;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.SaveAndReimport();
  }
  var assets=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");
  assets.PursuitRoad=AssetDatabase.LoadAssetAtPath<Texture2D>(root+"road-surface-v01.png");assets.PursuitRidges=AssetDatabase.LoadAssetAtPath<Texture2D>(root+"mid-ridge-v01.png");assets.PursuitMarkers=AssetDatabase.LoadAssetAtPath<Texture2D>(root+"landmarks-v01.png");assets.PursuitLayout=AssetDatabase.LoadAssetAtPath<TextAsset>(root+"pursuit-layout.json");if(assets.PursuitLayout==null)throw new Exception("Missing inspected layout");
  // Direction's final normal-alpha composite is clean; do not harden the supplied edges.
  assets.PursuitCutout=null;
  // The original far asset contains no nearby hairpin. Keep the older terrain asset for special-scene work.
  assets.ExteriorMap=assets.DistantFar;EditorUtility.SetDirty(assets);AssetDatabase.SaveAssets();
 }
}
#endif
