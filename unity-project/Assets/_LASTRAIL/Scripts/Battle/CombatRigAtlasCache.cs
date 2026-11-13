using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Battle {
// Render-only alpha cache. Immutable source PNGs stay unchanged; clipping happens before mip filtering.
public static class CombatRigAtlasCache {
 public sealed class Lease {public RenderTexture Texture;public Material Material;internal int Owners;internal string Key;}
 static readonly Dictionary<string,Lease> cache=new Dictionary<string,Lease>();
 public static Lease Acquire(Texture2D source,JObject data){
  string key=source.GetInstanceID()+":"+Hash128.Compute(data["parts"].ToString(Newtonsoft.Json.Formatting.None));
  if(cache.TryGetValue(key,out var old)){old.Owners++;return old;}
  var bakeShader=LastRail.Resources.PreparedContent.Load<Shader>("CombatPresentation/RigCacheBake");var uiShader=LastRail.Resources.PreparedContent.Load<Shader>("CombatPresentation/RigPremultiplied");if(bakeShader==null||uiShader==null)throw new InvalidOperationException("Rig cache shaders missing");
  int width=(int)data["sourceSize"][0],height=(int)data["sourceSize"][1];var material=new Material(bakeShader){mainTexture=source};
  var multisample=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Default,4);var previous=RenderTexture.active;
  var texture=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32){name="NativeRigAlphaCache",useMipMap=true,autoGenerateMips=false,filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp,mipMapBias=-.25f};texture.Create();
  try{
   RenderTexture.active=multisample;GL.Clear(true,true,Color.clear);GL.PushMatrix();
   try{GL.LoadPixelMatrix(0,width,height,0);foreach(var part in (JArray)data["parts"]){
    if(!(part["meshVertices"] is JArray vertices))throw new ArgumentException("Validated exact union mesh required before caching");
    var points=vertices.Select(p=>new Vector3((float)p[0],(float)p[1],0)).ToArray();var mesh=new Mesh();
    try{mesh.vertices=points;mesh.uv=points.Select(p=>new Vector2(p.x/width,1-p.y/height)).ToArray();mesh.triangles=((JArray)part["meshTriangles"]).Select(i=>(int)i).ToArray();material.SetPass(0);Graphics.DrawMeshNow(mesh,Matrix4x4.identity);}
    finally{UnityEngine.Object.DestroyImmediate(mesh);}
   }}finally{GL.PopMatrix();}
   Graphics.Blit(multisample,texture);texture.GenerateMips();
  }catch{texture.Release();UnityEngine.Object.DestroyImmediate(texture);throw;}
  finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(multisample);UnityEngine.Object.DestroyImmediate(material);}
  var lease=new Lease{Texture=texture,Material=new Material(uiShader),Owners=1,Key=key};cache.Add(key,lease);return lease;
 }
 public static void Release(Lease lease){if(lease==null||--lease.Owners>0)return;cache.Remove(lease.Key);lease.Texture.Release();UnityEngine.Object.DestroyImmediate(lease.Texture);UnityEngine.Object.DestroyImmediate(lease.Material);}
}
}
