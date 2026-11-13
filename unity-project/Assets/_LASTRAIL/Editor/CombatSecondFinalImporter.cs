#if UNITY_EDITOR
using UnityEditor;
public sealed class CombatSecondFinalImporter:AssetPostprocessor {
 void OnPreprocessTexture(){if(!assetPath.StartsWith("Assets/_LASTRAIL/Resources/CombatSecondFinal/")&&!assetPath.StartsWith("Assets/_LASTRAIL/Content/RemoteResources/CombatSecondFinal/"))return;var importer=(TextureImporter)assetImporter;importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.wrapMode=UnityEngine.TextureWrapMode.Clamp;importer.filterMode=UnityEngine.FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=512;importer.npotScale=TextureImporterNPOTScale.None;importer.isReadable=false;}
}
#endif
