using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Presentation.Battle {
 public sealed class FourAgentV03Assets:ScriptableObject {
  [SerializeField,HideInInspector]string catalogJson,cropsJson,projectileJson;
  [SerializeField]FiveAgentVisualAssets.TextureEntry[] textures;
  public void Initialize(JObject catalog,JObject crops,JObject projectile,FiveAgentVisualAssets.TextureEntry[] entries){if(!string.IsNullOrEmpty(catalogJson))throw new InvalidOperationException("Preserve existing v03 catalog.");catalogJson=catalog.ToString();cropsJson=crops.ToString();projectileJson=projectile.ToString();textures=(FiveAgentVisualAssets.TextureEntry[])entries.Clone();CreateCatalog();}
  public FourAgentV03Catalog CreateCatalog()=>FourAgentV03Catalog.Restore(JObject.Parse(catalogJson));
  public JObject Crops=>JObject.Parse(cropsJson);
  public JObject Projectile=>JObject.Parse(projectileJson);
  public Texture2D Texture(string path)=>textures.FirstOrDefault(t=>t.Path==path)?.Texture??throw new ArgumentException("Unbound v03 texture: "+path);
  public Vector2 Size(string path)=>textures.FirstOrDefault(t=>t.Path==path)?.NativeSize??throw new ArgumentException("Unbound v03 canvas: "+path);
 }
}
