#if UNITY_EDITOR
using System;
using LastRail.Battle;
using UnityEditor;
using UnityEngine;
public static class CombatSupportFireInstall {
 public static void Run(){
  if(!Application.isBatchMode)throw new InvalidOperationException("Explicit installation only");
  const string root="Assets/_LASTRAIL/Content/RemoteResources/CombatPresentation/SupportMissile/";
  foreach(var file in new[]{"hardware-source-v02.png","fx-additive-source-v01.png"}){var importer=(TextureImporter)AssetImporter.GetAtPath(root+file);importer.textureType=TextureImporterType.Default;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.alphaSource=TextureImporterAlphaSource.None;importer.mipmapEnabled=file.StartsWith("fx-");importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();}
  var asset=AssetDatabase.LoadAssetAtPath<CombatSupportFireAssets>(root+"support-missile.asset");if(asset==null){asset=ScriptableObject.CreateInstance<CombatSupportFireAssets>();AssetDatabase.CreateAsset(asset,root+"support-missile.asset");}
  asset.Hardware=AssetDatabase.LoadAssetAtPath<Texture2D>(root+"hardware-source-v02.png");asset.Effects=AssetDatabase.LoadAssetAtPath<Texture2D>(root+"fx-additive-source-v01.png");asset.Masks=AssetDatabase.LoadAssetAtPath<TextAsset>(root+"hardware-cache-rig-v01.json");asset.HardwareMetadata=AssetDatabase.LoadAssetAtPath<TextAsset>(root+"hardware-metadata-v01.json");asset.FxMetadata=AssetDatabase.LoadAssetAtPath<TextAsset>(root+"fx-metadata-v01.json");
  var material=AssetDatabase.LoadAssetAtPath<Material>(root+"additive.mat");if(material==null){material=new Material(AssetDatabase.LoadAssetAtPath<Shader>(root+"SupportAdditive.shader"));AssetDatabase.CreateAsset(material,root+"additive.mat");}asset.Additive=material;
  var approved=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");approved.SupportFire=asset;EditorUtility.SetDirty(asset);EditorUtility.SetDirty(approved);AssetDatabase.SaveAssets();
 }
}
#endif
