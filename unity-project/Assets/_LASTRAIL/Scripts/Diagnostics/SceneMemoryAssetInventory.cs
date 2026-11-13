#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.U2D;
using UnityEngine.UI;
namespace LastRail.Diagnostics {
 public static class SceneMemoryAssetInventory {
  public static JObject Capture(){
   var all=UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
   var assets=all.Where(o=>o is Texture||o is SpriteAtlas||o is AudioClip||o is Mesh||o is AnimationClip||o is RuntimeAnimatorController||o is Material||o is Shader).GroupBy(o=>o.GetInstanceID()).Select(g=>g.First()).ToArray();
   var references=new Dictionary<int,List<string>>();
   void Ref(UnityEngine.Object target,string label){if(target==null)return;int id=target.GetInstanceID();if(!references.TryGetValue(id,out var list))references[id]=list=new List<string>();list.Add(label);}
   foreach(var raw in all.OfType<RawImage>())Ref(raw.texture,raw.name+":RawImage:active="+raw.gameObject.activeInHierarchy);
   foreach(var img in all.OfType<Image>())if(img.sprite!=null)Ref(img.sprite.texture,img.name+":Image.sprite:active="+img.gameObject.activeInHierarchy);
   foreach(var material in all.OfType<Material>())foreach(string property in material.GetTexturePropertyNames())Ref(material.GetTexture(property),material.name+":Material."+property);
   var rows=new JArray();foreach(var o in assets){var row=new JObject{["instanceId"]=o.GetInstanceID(),["name"]=o.name,["type"]=o.GetType().Name,["runtimeReportedBytes"]=Profiler.GetRuntimeMemorySizeLong(o),["hideFlags"]=o.hideFlags.ToString()};
    if(o is Texture t){row["width"]=t.width;row["height"]=t.height;row["dimension"]=t.dimension.ToString();row["graphicsFormat"]=t.graphicsFormat.ToString();}
    if(o is Texture2D t2){row["readable"]=t2.isReadable;row["mipCount"]=t2.mipmapCount;}
    if(o is RenderTexture rt){row["depthBits"]=rt.depth;row["antiAliasing"]=rt.antiAliasing;row["created"]=rt.IsCreated();}
    if(o is AudioClip a){row["loadType"]=a.loadType.ToString();row["loadState"]=a.loadState.ToString();row["samples"]=a.samples;row["channels"]=a.channels;row["frequency"]=a.frequency;}
    if(o is Mesh m){row["vertices"]=m.vertexCount;row["readable"]=m.isReadable;}
    if(o is SpriteAtlas atlas)row["spriteCount"]=atlas.spriteCount;
    row["observedDirectReferences"]=references.TryGetValue(o.GetInstanceID(),out var refs)?new JArray(refs):new JArray();row["referenceScope"]="UI texture and material texture properties only; absence does not mean unreferenced. Serialized probe/catalog, engine, TMP/atlas and managed roots require further analysis.";rows.Add(row);
   }
   var groups=new JArray(rows.OfType<JObject>().GroupBy(r=>(string)r["type"]).Select(g=>new JObject{["type"]=g.Key,["count"]=g.Count(),["reportedBytesByUniqueInstance"]=g.Sum(r=>(long)r["runtimeReportedBytes"])}));
   var duplicates=new JArray(rows.OfType<JObject>().GroupBy(r=>(string)r["type"]+"|"+(string)r["name"]+"|"+(string)r["width"]+"x"+(string)r["height"]).Where(g=>g.Count()>1).Select(g=>new JObject{["key"]=g.Key,["instances"]=new JArray(g.Select(r=>r["instanceId"])),["interpretation"]="Same name/type/dimensions candidate only; not proven duplicate bytes or unnecessary allocation."}));
   var types=new JArray(all.GroupBy(o=>o.GetType().FullName).Select(g=>new JObject{["type"]=g.Key,["count"]=g.Count()}));
   var inputRefs=new JArray(all.Where(o=>o.GetType().FullName=="UnityEngine.InputSystem.InputActionReference").Select(o=>new JObject{["id"]=o.GetInstanceID(),["name"]=o.name,["action"]=o.GetType().GetProperty("action")?.GetValue(o)?.ToString(),["hideFlags"]=o.hideFlags.ToString()}));
   return new JObject{["allLoadedUnityObjectCount"]=all.Length,["objectTypes"]=types,["inputActionReferences"]=inputRefs,["assetInstanceCount"]=rows.Count,["groups"]=groups,["items"]=rows,["sameNameCandidates"]=duplicates,["accounting"]="Unique instance IDs, no separate Sprite texture count. Runtime object-size group sums are diagnostic, not additive CPU+GPU/total process accounting; dependencies may overlap."};
  }
 }
}
#endif
