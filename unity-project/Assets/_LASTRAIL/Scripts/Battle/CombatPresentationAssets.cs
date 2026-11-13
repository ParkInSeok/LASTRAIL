using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LastRail.Presentation.MissionShell;
namespace LastRail.Battle {
public sealed class CombatPresentationAssets:ScriptableObject {
 public Texture2D OrdinaryFieldBackground,OrdinaryFieldTrack;
 public OrdinaryRoofPresentationAssets OrdinaryRoof;
 public TMP_FontAsset Font;
 public Texture2D[] SecondFinalResources;
 public Texture2D ExteriorBezel;
 public bool SeparateSun;
 public Rect[] WindowOpenings;public Vector3[] WheelDiscs;
 public Texture2D Console,IconAtlas,Buggy,Bike,TurretRig;
 public Texture2D ActiveIconAtlas;public string[] ActiveIconIds;public Rect[] ActiveIconRegions;
 public bool CardPolishEnabled;
 public string[] OperatorIds;public Texture2D[] OperatorAtlases;
 public float FootBottom(string id,int cell){switch(id){
 case "op_kyle":return new float[]{0.984375f,0.951171875f,0.958984375f,0.919921875f,0.935546875f}[Mathf.Clamp(cell,0,4)];
 case "op_reina":return new float[]{0.984375f,0.982421875f,0.98828125f,0.876953125f,0.900390625f}[Mathf.Clamp(cell,0,4)];
 case "op_eve":return new float[]{0.994140625f,0.9765625f,0.984375f,0.859375f,0.90625f}[Mathf.Clamp(cell,0,4)];
 case "op_sera":return new float[]{0.92578125f,0.927734375f,0.93359375f,0.90625f,0.916015625f}[Mathf.Clamp(cell,0,4)];
 case "op_mira":return new float[]{0.94921875f,0.935546875f,0.947265625f,0.865234375f,0.935546875f}[Mathf.Clamp(cell,0,4)];
 case "enemy_raider_scout":return new float[]{0.9921875f,0.96875f,0.978515625f,0.90234375f,0.9375f}[Mathf.Clamp(cell,0,4)];
 default:return 1;}}
 public Rect PoseUv(string id,int cell){
  // Generated poses have uneven safe margins. Use inspected atlas regions without editing source PNGs.
  float x=(cell%3)*512,y=cell<3?0:512,w=512;
  if(id=="op_kyle"){
   float[] starts={0,480,1110,0,480,1060};float[] widths={480,630,426,480,570,476};x=starts[cell];w=widths[cell];
  }
  if(id=="op_mira"){
   // Use the clean aimed pose for special as well; the generated special rifle overlaps the portrait cell.
   if(cell==4)cell=1;
   float[] starts={0,530,1090,0,530,1160};float[] widths={525,560,446,550,560,376};x=starts[cell];w=widths[cell];y=cell<3?0:512;
  }
  return new Rect(x/1536f,1-(y+512)/1024f,w/1536f,.5f);
 }
 public Texture2D ActorAtlas(string id){int i=OperatorIds==null?-1:Array.IndexOf(OperatorIds,id);return i>=0&&OperatorAtlases!=null&&i<OperatorAtlases.Length?OperatorAtlases[i]:null;}
 public Texture2D DistantFar,DistantTerrain;public TextAsset DistantTerrainLayout;
 public Texture2D PursuitRoad,PursuitRidges,PursuitMarkers;public TextAsset PursuitLayout;
 public Material PursuitCutout;
 public CombatOperatorRigSet OperatorRigs;
 public CombatOperatorFrames OperatorFrames;
 public Texture2D SeraCastingCrystal,SeraIdleCrystal;public Shader SeraCrystalShader;
 public CombatSupportFireAssets SupportFire;
 [Range(.75f,1f)] public float CabinFigureScale=1f;
 public Texture2D Background,ExteriorMap,ExteriorTrain,Interior,DefaultTurret; public string MapId="wasteland",SkinId="default"; public Sprite Frame,Primary,Secondary; public Texture2D Hit,Skill,Shield;
 public string[] Keys; public Texture2D[] Frames;
 Dictionary<string,Texture2D> cache;
 public Texture2D Find(string key){if(cache==null){cache=new Dictionary<string,Texture2D>();for(int i=0;i<Keys.Length;i++)cache[Keys[i]]=Frames[i];}return cache.TryGetValue(key,out var t)?t:null;}
}
}

