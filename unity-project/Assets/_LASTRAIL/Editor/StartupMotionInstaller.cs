#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastRail.Editor
{
    public static class StartupMotionInstaller
    {
        [System.Serializable] class Manifest { public Layer[] coverage; }
        [System.Serializable] class Layer { public string id; public string exportPath; }
        public static string Install()
        {
            var source=Path.GetFullPath(Path.Combine(Application.dataPath,"../QA/Design/StartupMotion-20260908"));
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(source,"manifest.json")));
            if(manifest.coverage.Length!=14) throw new System.Exception("Expected exact fourteen-layer manifest.");
            const string dest="Assets/_LASTRAIL/Resources/StartupMotion";
            Directory.CreateDirectory(dest);
            foreach(var layer in manifest.coverage)
            {
                var input=Path.GetFullPath(Path.Combine(source,layer.exportPath));
                if(!input.StartsWith(source+Path.DirectorySeparatorChar,System.StringComparison.OrdinalIgnoreCase)) throw new System.Exception("Manifest path escapes source.");
                var path=dest+"/"+Path.GetFileName(input);
                if(!File.Exists(path)) File.Copy(input,path);
                else if(System.Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(input)))!=System.Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(path)))) throw new System.Exception("Preserving conflicting existing import: "+path);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Default; importer.alphaIsTransparency=true;
                importer.mipmapEnabled=false; importer.isReadable=false; importer.maxTextureSize=4096;
                importer.npotScale=TextureImporterNPOTScale.None;
                importer.wrapModeU=layer.id.Contains("outside")||layer.id.Contains("terrain")||layer.id.Contains("ruins")?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
                importer.wrapModeV=layer.id.Contains("rail-sleepers")?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
                importer.filterMode=FilterMode.Bilinear;
                var android=importer.GetPlatformTextureSettings("Android"); android.overridden=true; android.maxTextureSize=4096; android.format=TextureImporterFormat.ASTC_6x6;
                importer.SetPlatformTextureSettings(android); importer.SaveAndReimport();
            }
            var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/_LASTRAIL/UI/StartupRail.shader");
            if(shader==null) throw new System.Exception("Rail shader missing.");
            if(AssetDatabase.LoadAssetAtPath<Material>(dest+"/StartupRail.mat")==null) AssetDatabase.CreateAsset(new Material(shader),dest+"/StartupRail.mat");
            AssetDatabase.SaveAssets(); return "Installed exact 14 layers; original art preserved; Android ASTC6x6, no mip/read-write; runtime QA pending.";
        }
    }
}
#endif
