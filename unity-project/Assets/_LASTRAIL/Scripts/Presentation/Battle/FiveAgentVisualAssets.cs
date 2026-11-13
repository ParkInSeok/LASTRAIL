using System;
using System.Collections.Generic;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Presentation.Battle {
 public sealed class FiveAgentVisualAssets:ScriptableObject {
  [Serializable]public sealed class TextureEntry{public string Path;public Texture2D Texture;public Vector2 NativeSize;}
  [SerializeField,HideInInspector]string contractJson,manifestJson,projectileJson,contractHash;
  [SerializeField]TextureEntry[] textures;
  public string ContractHash=>contractHash;
  public void Initialize(string contract,string manifest,string projectile,TextureEntry[] entries){if(!string.IsNullOrEmpty(contractJson))throw new InvalidOperationException("Asset binding already initialized.");contractJson=contract;manifestJson=manifest;projectileJson=projectile;contractHash=BDirectAttackInput.Digest(JObject.Parse(contract));textures=(TextureEntry[])entries.Clone();CreateModel();}
  public Texture2D Texture(string path)=>textures?.FirstOrDefault(t=>t.Path==path)?.Texture??throw new ArgumentException("Visual texture not bound: "+path);
  public FiveAgentVisualReference CreateModel()=>FiveAgentVisualReference.Read(JObject.Parse(contractJson),JObject.Parse(manifestJson),JObject.Parse(projectileJson),contractHash,p=>{var t=textures.FirstOrDefault(e=>e.Path==p)??throw new ArgumentException("Unbound reference size.");return t.NativeSize;});
 }
}