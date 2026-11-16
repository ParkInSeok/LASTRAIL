using System.IO;
using LastRail.Resources;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
namespace LastRail.Editor {
 public static class FormationPlanImport {
  [MenuItem("LASTRAIL/QA/Import Formation Plan")]
  public static void Run() {
   var settings=AddressableAssetSettingsDefaultObject.Settings;
   var group=settings.FindGroup("remote_resources");
   if(group==null)throw new System.InvalidOperationException("Remote resource group required");
   foreach(var entry in RemoteContentCatalog.Select(new[]{"FormationPlan/"})) {
    var importer=(TextureImporter)AssetImporter.GetAtPath(entry.Path);
    importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;importer.npotScale=TextureImporterNPOTScale.None;
    importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
    importer.maxTextureSize=entry.Key.EndsWith("background")||entry.Key.EndsWith("combat-roof-v02")||entry.Key.EndsWith("portrait-nyx")?2048:512;
    importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.isReadable=false;importer.SaveAndReimport();
    var item=settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(entry.Path),group);
    item.address=entry.Address;item.SetLabel("remote_resources",true,true);item.SetLabel(AddressableContentBackend.RequiredLabel,true,true);
   }
   EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
   Debug.Log("FORMATION_PLAN_REGISTERED");
  }
 }
}
