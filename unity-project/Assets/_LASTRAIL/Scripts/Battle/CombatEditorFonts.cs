using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
#endif
namespace LastRail.Battle {
// TMP's Editor saves dynamic glyph caches back into persistent source assets.
// Keep those presentation caches transient in Editor; player font behavior is unchanged.
public static class CombatEditorFonts {
#if UNITY_EDITOR
 static readonly Dictionary<TMP_FontAsset,TMP_FontAsset> copies=new Dictionary<TMP_FontAsset,TMP_FontAsset>();
 public static TMP_FontAsset Resolve(TMP_FontAsset source){
  if(source==null)return null;if(copies.TryGetValue(source,out var found)&&found!=null)return found;
  var copy=Object.Instantiate(source);copy.name=source.name+" (transient Editor cache)";copy.hideFlags=HideFlags.HideAndDontSave;copies[source]=copy;
  copy.atlasTextures=source.atlasTextures.Select(t=>{if(t==null)return null;var texture=Object.Instantiate(t);texture.hideFlags=HideFlags.HideAndDontSave;return texture;}).ToArray();
  if(source.material!=null){copy.material=Object.Instantiate(source.material);copy.material.hideFlags=HideFlags.HideAndDontSave;if(copy.atlasTextures.Length>0)copy.material.mainTexture=copy.atlasTextures[0];}
  var fallbacks=new List<TMP_FontAsset>();if(source.fallbackFontAssetTable!=null)fallbacks.AddRange(source.fallbackFontAssetTable);if(TMP_Settings.fallbackFontAssets!=null)fallbacks.AddRange(TMP_Settings.fallbackFontAssets);copy.fallbackFontAssetTable=fallbacks.Where(f=>f!=null&&f!=source).Distinct().Select(Resolve).ToList();
  for(int i=0;i<copy.fontWeightTable.Length;i++){copy.fontWeightTable[i].regularTypeface=Resolve(source.fontWeightTable[i].regularTypeface);copy.fontWeightTable[i].italicTypeface=Resolve(source.fontWeightTable[i].italicTypeface);}
  copy.ReadFontAssetDefinition();return copy;
 }
#else
 public static TMP_FontAsset Resolve(TMP_FontAsset source)=>source;
#endif
}
}
