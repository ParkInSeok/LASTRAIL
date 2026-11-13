#if UNITY_EDITOR
using System;
using LastRail.Battle;
using UnityEditor;
using UnityEngine;
public static class CombatOperatorRigInstall {
 public static void Run(){
  if(!Application.isBatchMode)throw new InvalidOperationException("Explicit batch install only");
  const string path="Assets/_LASTRAIL/Content/RemoteResources/CombatPresentation/operator-rigs.asset";
  var set=AssetDatabase.LoadAssetAtPath<CombatOperatorRigSet>(path);if(set==null){set=ScriptableObject.CreateInstance<CombatOperatorRigSet>();AssetDatabase.CreateAsset(set,path);}
  set.OperatorIds=new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira"};set.Sources=new Texture2D[5];set.Definitions=new TextAsset[5];
  for(int i=0;i<5;i++){var id=set.OperatorIds[i].Substring(3);set.Sources[i]=AssetDatabase.LoadAssetAtPath<Texture2D>(i==0?"Assets/_LASTRAIL/UI/Diagnostics/KyleParts/parts-source-v01.png":"Assets/_LASTRAIL/UI/Diagnostics/OperatorParts/"+id+"-source-v01.png");set.Definitions[i]=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LASTRAIL/UI/Diagnostics/OperatorParts/"+id+"-rig-exact-v04.json");if(set.Sources[i]==null||set.Definitions[i]==null)throw new InvalidOperationException("Missing approved source "+id);}
  var assets=LastRail.Resources.PreparedContent.Load<CombatPresentationAssets>("CombatPresentation/approved");assets.OperatorRigs=set;assets.CabinFigureScale=.86f;EditorUtility.SetDirty(set);EditorUtility.SetDirty(assets);AssetDatabase.SaveAssets();
 }
}
#endif
