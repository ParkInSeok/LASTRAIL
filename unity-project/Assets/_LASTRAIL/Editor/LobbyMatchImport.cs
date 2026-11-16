using LastRail.Resources;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using TMPro;
using UnityEngine.TextCore.LowLevel;

namespace LastRail.Editor
{
    public static class LobbyMatchImport
    {
        public static void Run()
        {
            var settings=AddressableAssetSettingsDefaultObject.Settings;
            var group=settings.FindGroup("remote_resources");
            if(group==null)throw new System.InvalidOperationException("Remote resource group required");
            const string fontPath="Assets/_LASTRAIL/Content/RemoteResources/LobbyDeck/Typography/LobbySerif-v07.asset";
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if(font==null){
                var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/_LASTRAIL/Content/RemoteResources/LobbyDeck/Typography/NanumMyeongjo-Bold.ttf");
                if(source==null)throw new System.IO.FileNotFoundException("Lobby serif source font");
                font=TMP_FontAsset.CreateFontAsset(source,64,7,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);
                font.name="LobbySerif-v07";font.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>();
                AssetDatabase.CreateAsset(font,fontPath);AssetDatabase.AddObjectToAsset(font.material,font);
                foreach(var texture in font.atlasTextures)AssetDatabase.AddObjectToAsset(texture,font);
                font.TryAddCharacters("지휘관현재편성다음작전마지막출발출석패스임무운행보급확인필요0123456789—+ /UNITYQA");EditorUtility.SetDirty(font);
            }
            foreach(var entry in RemoteContentCatalog.Select(new[]{"LobbyDeck/navigation-shapes-v06","LobbyDeck/frame-v06","LobbyDeck/button-surface-v06","LobbyDeck/commander-rim-v07","LobbyDeck/currency-icons-v07","LobbyDeck/serif-v07"}))
            {
                if(entry.ResourceType==typeof(Texture2D)){
                var importer=(TextureImporter)AssetImporter.GetAtPath(entry.Path);
                if(importer==null)throw new System.IO.FileNotFoundException(entry.Path);
                importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;
                importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=true;
                importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
                importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;
                importer.isReadable=false;importer.SaveAndReimport();
                }
                var item=settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(entry.Path),group);
                item.address=entry.Address;item.SetLabel("remote_resources",true,true);item.SetLabel(AddressableContentBackend.RequiredLabel,true,true);
            }
            EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();Debug.Log("LOBBY_MATCH_V07_REGISTERED");
        }
    }
}
