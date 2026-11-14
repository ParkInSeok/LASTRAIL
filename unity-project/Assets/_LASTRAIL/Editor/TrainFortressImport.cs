using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using LastRail.Resources;
namespace LastRail.Editor {
 public static class TrainFortressImport {
  public static void Run(){
   var settings=AddressableAssetSettingsDefaultObject.Settings;var group=settings.FindGroup("remote_resources");
   if(group==null)throw new System.InvalidOperationException("Remote resource group required");
   foreach(var entry in RemoteContentCatalog.Select(new[]{"TrainFortress/"})){
    var importer=(TextureImporter)AssetImporter.GetAtPath(entry.Path);if(importer==null)throw new System.IO.FileNotFoundException(entry.Path);
    importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
    var item=settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(entry.Path),group);item.address=entry.Address;item.SetLabel("remote_resources",true,true);item.SetLabel(AddressableContentBackend.RequiredLabel,true,true);
   }
   EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
  }
 }
}
