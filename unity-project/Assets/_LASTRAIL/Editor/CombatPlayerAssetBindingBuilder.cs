#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// Builds an asset-only evidence artifact. Does not modify START or claim admission.
public static class CombatPlayerAssetBindingBuilder
{
    static string Physical(string path)
    {
        if(!path.StartsWith("Packages/",StringComparison.Ordinal))return Path.GetFullPath(path);
        var package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);
        if(package==null)throw new InvalidOperationException("Unresolved asset package: "+path);
        return Path.Combine(package.resolvedPath,path.Substring(package.assetPath.Length+1));
    }
    static string Hash(string path)
    {
        using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Physical(path)))).Replace("-","").ToLowerInvariant();
    }
    public static CombatPlayerAssetBinding Bake()=>BakeConfiguration(null);
    public static CombatPlayerAssetBinding BakeConfiguration(CombatPresentationAssets config)
    {
        if(config==null)config=AssetDatabase.LoadAssetAtPath<CombatPresentationAssets>(Ce03RailPresentation.Root);
        string configurationPath=AssetDatabase.GetAssetPath(config);
        CombatPlayerAssetBinding.RequirePresentation(config);
        Ce03DynamicResourceClosure.Validate();
        var dynamicRows=(JArray)JObject.Parse(File.ReadAllText(Ce03DynamicResourceClosure.ManifestPath))["rows"];
        var paths=AssetDatabase.GetDependencies(configurationPath,true)
            .Concat(AssetDatabase.GetDependencies(dynamicRows.Select(r=>(string)r["path"]).ToArray(),true))
            .Distinct(StringComparer.Ordinal).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
        var files=new JArray();var rows=new JArray();var references=new List<UnityEngine.Object>();
        foreach(string path in paths)
        {
            if(!File.Exists(Physical(path))||!File.Exists(Physical(path+".meta")))throw new InvalidOperationException("Asset bytes missing: "+path);
            files.Add(new JObject{["path"]=path,["rawSha256"]=Hash(path),["metaSha256"]=Hash(path+".meta")});
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path).Where(a=>a!=null).OrderBy(a=>a.name,StringComparer.Ordinal).ThenBy(a=>a.GetType().FullName,StringComparer.Ordinal))
            {
                // File hashes retain editor provenance; prefab edit records and scripts are not Player objects.
                if(asset.GetType().Namespace?.StartsWith("UnityEditor",StringComparison.Ordinal)==true)continue;
                if(path.EndsWith(".prefab",StringComparison.Ordinal)&&asset.GetType()==typeof(UnityEngine.Object))continue; // Native PrefabInstance edit records are erased in Player builds.
                if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset,out string guid,out long id))throw new InvalidOperationException("Asset locator absent: "+path);
                rows.Add(new JObject{["id"]=guid+":"+id.ToString(CultureInfo.InvariantCulture),["name"]=asset.name,["type"]=asset.GetType().FullName,
                    ["width"]=asset is Texture2D texture?texture.width:0,["height"]=asset is Texture2D image?image.height:0});
                references.Add(asset);
            }
        }
        var roles=new JArray();
        void Role(string field,int index,UnityEngine.Object value)
        {
            if(value==null)return;
            int reference=references.IndexOf(value);if(reference<0)throw new InvalidOperationException("Role outside dependency closure: "+field);
            roles.Add(new JObject{["field"]=field,["index"]=index,["reference"]=reference});
        }
        foreach(var field in typeof(CombatPresentationAssets).GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance).OrderBy(f=>f.Name,StringComparer.Ordinal))
        {
            var value=field.GetValue(config);
            if(value is UnityEngine.Object obj)Role(field.Name,-1,obj);
            else if(value is UnityEngine.Object[] array)for(int i=0;i<array.Length;i++)Role(field.Name,i,array[i]);
        }
        var manifest=new JObject{["version"]="combat-player-assets-v1",["unityVersion"]=Application.unityVersion,
            ["formation"]=new JArray("op_kyle","op_reina","op_eve","op_sera","op_mira"),["configurationPath"]=configurationPath,
            ["files"]=files,["runtimeObjects"]=rows,["roles"]=roles,["configurationSettings"]=CombatPlayerAssetBinding.CaptureSettings(config),["nestedSettings"]=CombatPlayerAssetBinding.CaptureNestedSettings(config,references.ToArray()),["admissionGranted"]=false,
            ["validatorSourceSha256"]=Hash("Assets/_LASTRAIL/Scripts/Battle/CombatPlayerAssetBinding.cs"),
            ["builderSourceSha256"]=Hash("Assets/_LASTRAIL/Editor/CombatPlayerAssetBindingBuilder.cs")};
        var result=ScriptableObject.CreateInstance<CombatPlayerAssetBinding>();
        try
        {
            result.InitializeForBuild(Ce03CommandContract.Canonical(manifest),config,references.ToArray());
            using var lease=result.Acquire(result.ManifestHash,((JArray)manifest["formation"]).Values<string>().ToArray());
            return result;
        }
        catch{UnityEngine.Object.DestroyImmediate(result);throw;}
    }
}
#endif
