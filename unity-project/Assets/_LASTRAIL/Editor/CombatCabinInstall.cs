#if UNITY_EDITOR
using System;
using LastRail.Battle;
using UnityEditor;
using UnityEngine;
public static class CombatCabinInstall {
 public static void Run(){if(!Application.isBatchMode)throw new InvalidOperationException("Explicit local batch only");var assets=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");assets.CabinFigureScale=.86f;EditorUtility.SetDirty(assets);AssetDatabase.SaveAssets();}
}
#endif
