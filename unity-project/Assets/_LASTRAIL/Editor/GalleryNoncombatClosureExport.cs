#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
public static class GalleryNoncombatClosureExport
{
    public static void Write(string output)
    {
        string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        var rows=new JArray();foreach(var key in new[]{"EntryPopup/roulette-ring","EntryPopup/roulette-pointer","EntryPopup/roulette-room","NoncombatSupport/support-room","NoncombatSupport/support-host","NoncombatResult/environment","NoncombatResult/train","CombatSecondFinal/support_fire-v01","CombatSecondFinal/support_barrier-v01","CombatSecondFinal/support_repair-v01","OperatorIdentity/catalog"}){
            UnityEngine.Object asset=key=="OperatorIdentity/catalog"?(UnityEngine.Object)Resources.Load<TextAsset>(key):Resources.Load<Texture2D>(key);
            if(asset==null||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset,out string guid,out long id))throw new InvalidOperationException("Resource "+key);var path=AssetDatabase.GetAssetPath(asset);
            if(AssetDatabase.LoadMainAssetAtPath(path)!=asset)throw new InvalidOperationException("Resource object mismatch "+key);
            var row=new JObject{["resourceKey"]=key,["path"]=path,["guid"]=guid,["localFileId"]=id.ToString(),["type"]=asset.GetType().FullName,["assetRawSha256"]=Hash(path),["metaRawSha256"]=Hash(path+".meta"),["resourcesLoadSameObject"]=true};
            if(asset is Texture2D texture){row["width"]=texture.width;row["height"]=texture.height;}rows.Add(row);
        }
        File.WriteAllText(Path.Combine(output,"gallery-noncombat-closure-extension.json"),new JObject{["schema"]="gallery-noncombat-resource-closure-v1",["resources"]=rows,["actualAdmissionReady"]=false,["scope"]="Explicit dynamic resource identities for gallery19/20/21/27; includes3 existing shared combat support icons. Native primitives are source hashed separately. Not full-gallery or reference-equivalence approval."}.ToString());
    }
}
#endif
