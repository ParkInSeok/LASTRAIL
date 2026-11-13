#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LastRail.Battle;
public static class ApprovedCombatBuilder {
 public static void Build(){
  string root="Assets/_LASTRAIL/UI/ApprovedCombat/";
  Texture2D Load(string name)=>AssetDatabase.LoadAssetAtPath<Texture2D>(root+name+".png");
  foreach(string path in Directory.GetFiles(root,"*.png")){
   var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));importer.alphaIsTransparency=true;importer.maxTextureSize=2048;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
  }
  const string pathAsset="Assets/_LASTRAIL/Content/RemoteResources/CombatPresentation/approved.asset";Directory.CreateDirectory(Path.GetDirectoryName(pathAsset));
  var a=AssetDatabase.LoadAssetAtPath<CombatPresentationAssets>(pathAsset)??ScriptableObject.CreateInstance<CombatPresentationAssets>();
  a.ExteriorMap=Load("wasteland-map");a.ExteriorTrain=Load("hull-polish-v01");a.Interior=Load("cabin-polish-v01");a.DefaultTurret=Load("cannon-module");
  a.TurretRig=Load("turret-rig-v01");a.Console=Load("console-off-v02");a.IconAtlas=Load("icons-polish-v01");a.Buggy=Load("buggy-polish-v01");a.Bike=Load("bike-polish-v01");
  a.OperatorIds=new[]{"op_kyle","op_reina","op_eve","op_sera","op_mira","enemy_raider_scout"};a.OperatorAtlases=a.OperatorIds.Select(id=>Load(id+"-polish-v01")).ToArray();if(a.OperatorAtlases.Any(t=>t==null))throw new Exception("Missing polished operator atlas");
  string skins="Assets/_LASTRAIL/UI/Common/Skins/";
  a.Frame=AssetDatabase.LoadAssetAtPath<Sprite>(skins+"ui_button_secondary_9slice.png");a.Primary=AssetDatabase.LoadAssetAtPath<Sprite>(skins+"ui_button_primary_9slice.png");a.Secondary=a.Frame;
  string vfx="Assets/_LASTRAIL/UI/CombatResources/VFX/";
  a.Hit=AssetDatabase.LoadAssetAtPath<Texture2D>(vfx+"vfx_basic_hit_spark.png");a.Skill=AssetDatabase.LoadAssetAtPath<Texture2D>(vfx+"vfx_skill_activation.png");a.Shield=AssetDatabase.LoadAssetAtPath<Texture2D>(vfx+"vfx_shield_status.png");
  var keys=new List<string>();var frames=new List<Texture2D>();
  var ids=new[]{"op_kyle","op_reina","op_sera","op_mira","op_eve","enemy_raider_scout","enemy_spike_cart_gunner","enemy_railbike_marauder"};
  foreach(var id in ids)foreach(var state in new[]{"idle","attack","hit","down_or_defeated","skill_or_special"})for(int i=1;i<=4;i++){
   var path="Assets/_LASTRAIL/UI/CombatAnimations/"+(id.StartsWith("op_")?"Operators":"Enemies")+"/"+id+"/"+state+"/"+id+"_"+state+"_"+i.ToString("D3")+".png";
   var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(texture==null)throw new Exception("Missing "+path);keys.Add(id+"/"+state+"/"+i);frames.Add(texture);
  }
  a.WindowOpenings=new[]{new Rect(136,507,167,68),new Rect(423,507,159,68)};
  a.WheelDiscs=new[]{new Vector3(130,616,24),new Vector3(213,621,24),new Vector3(312,624,25),new Vector3(639,638,26),new Vector3(744,645,26),new Vector3(945,649,26),new Vector3(1097,656,26),new Vector3(1219,660,27),new Vector3(1336,660,27)};
  a.Font=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LASTRAIL/Prefabs/Screens/FullyAnimatedCombatHud.prefab").GetComponentsInChildren<TMPro.TMP_Text>(true).First().font;
  a.Keys=keys.ToArray();a.Frames=frames.ToArray();if(!AssetDatabase.Contains(a))AssetDatabase.CreateAsset(a,pathAsset);EditorUtility.SetDirty(a);AssetDatabase.SaveAssets();
 }
}
#endif

