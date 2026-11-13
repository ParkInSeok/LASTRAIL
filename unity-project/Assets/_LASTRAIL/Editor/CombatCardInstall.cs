#if UNITY_EDITOR
using System;
using LastRail.Battle;
using UnityEditor;
using UnityEngine;
public static class CombatCardInstall {
 public static void Run(){
  if(!Application.isBatchMode)throw new InvalidOperationException("Explicit batch only");
  const string path="Assets/_LASTRAIL/UI/ApprovedCombat/ability-atlas-v01.png";
  AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
  importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
  var assets=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");assets.ActiveIconAtlas=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
  assets.ActiveIconIds=new[]{"op_kyle.active","op_reina.active","op_eve.active","op_sera.active","op_mira.active"};assets.ActiveIconRegions=new Rect[5];
  // Direction display UV v02: alpha>=8 retained with >=16px margin; immutable PNG, normal alpha.
  var bounds=new[]{new Rect(36,34,451,451),new Rect(554,43,424,424),new Rect(1077,58,391,391),new Rect(56,549,410,410),new Rect(556,529,437,437)};
  for(int i=0;i<5;i++){var b=bounds[i];assets.ActiveIconRegions[i]=new Rect(b.x/1536f,1-b.yMax/1024f,b.width/1536f,b.height/1024f);}
  assets.CardPolishEnabled=true;EditorUtility.SetDirty(assets);AssetDatabase.SaveAssets();
 }
}
#endif
