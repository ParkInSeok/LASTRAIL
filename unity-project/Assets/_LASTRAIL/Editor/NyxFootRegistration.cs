#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// Physical roof-slot FOOT only. This cannot grant Nyx hit, emission or zone execution readiness.
public static class NyxFootRegistration {
 const string Root="D:/Project/LASTRAIL/",Folder="Assets/_LASTRAIL/Content/Battle/NyxFootRegistrationV01/";
 const string Roof="Assets/_LASTRAIL/Content/Battle/OrdinaryRoofSpawnV01/OrdinaryRoofSpawnV01.prefab";
 const string Art="Assets/_LASTRAIL/Content/Battle/Approved821/op_nyx-idle-0.png";
 static string Hash(string path){using var hash=SHA256.Create();return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
 static void Need(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
 static JArray Mm(Vector3 v)=>new JArray(checked((long)Math.Round(v.x*1000,MidpointRounding.AwayFromZero)),checked((long)Math.Round(v.y*1000,MidpointRounding.AwayFromZero)),checked((long)Math.Round(v.z*1000,MidpointRounding.AwayFromZero)));
 public static void Build(){
  Need(Application.isBatchMode,"Batch registration required");Need(!Directory.Exists(Folder),"Preserve existing registration");
  var contract=JObject.Parse(File.ReadAllText(Root+"QA/CommanderS1-20260922/nyx-node-restart-source-contract-20261107.json"));
  var roof=contract["ordinaryRoofSource"];Need((string)roof["packet"]["operatorIds"][4]=="op_nyx"&&(string)roof["packet"]["status"]=="LAYOUT_BOUND_ANCHORS_UNVERIFIED","Original requested roof slot required");
  var intake=JObject.Parse(File.ReadAllText(Root+"QA/ObstacleTrial-20261106/actual-server-intake-v01.json"));var geometry=JObject.Parse((string)intake["geometryDefinition"]);
  Need((string)geometry["source"]["scope"]=="LOCAL_AUTHORED_JUDGEMENT_GEOMETRY_NOT_APPROVED_ART_CALIBRATION","Original source scope differs");
  Need(Hash(Art)=="03253cad690228291bba9995ee05657f4e47baaff96babb70567a704601834a5","Approved original art changed");
  var loaded=PrefabUtility.LoadPrefabContents(Roof);GameObject output=null;
  try{
   var slot=loaded.transform.Find("Gun/SpawnFoot_4");Need(slot!=null,"Physical slot missing");
   Vector3 position=slot.position+Vector3.right*((float)geometry["source"]["originalWorldToArcOffsetMm"]/1000f);
   var originalLayout=((JObject)geometry["source"]["liveLayout"]).Properties().Select(p=>p.Value).Single(v=>(int?)v["formationSlot"]==4);
   Need(JToken.DeepEquals(Mm(position),originalLayout["originMm"]),"Source slot coordinate differs: "+Mm(position)+" versus "+originalLayout["originMm"]);
   output=new GameObject("NyxFootRegistrationV01");var actor=new GameObject("op_nyx");actor.transform.SetParent(output.transform,false);actor.transform.localPosition=position;
   var foot=new GameObject("Foot");foot.transform.SetParent(actor.transform,false);
   Directory.CreateDirectory(Folder);string prefab=Folder+"NyxFootOnly.prefab";PrefabUtility.SaveAsPrefabAsset(output,prefab);AssetDatabase.SaveAssets();
   var verified=PrefabUtility.LoadPrefabContents(prefab);try{Need(verified.GetComponentsInChildren<Collider>(true).Length==0&&verified.GetComponentsInChildren<Renderer>(true).Length==0,"No unmeasured visual scale or hit shape allowed");Need(Mm(verified.transform.Find("op_nyx/Foot").position).ToString()==Mm(position).ToString(),"Saved FOOT differs");}finally{PrefabUtility.UnloadPrefabContents(verified);}
   long front=(long)contract["ordinaryRailSource"]["packet"]["physicalConsist"]["initialFrontArcMm"];
   var report=new JObject{["status"]="FOOT_ONLY_NOT_EXECUTABLE",["operatorId"]="op_nyx",["prefabPath"]=prefab,["prefabSha256"]=Hash(prefab),["sourceRoofPrefab"]=Roof,["sourceRoofSha256"]=Hash(Roof),["sourceSlotPath"]="Gun/SpawnFoot_4",["roofSourceHash"]=roof["sourceHash"].DeepClone(),["artAssetPath"]=Art,["artSha256"]=Hash(Art),["sourceProjectId"]="d4c4ba167d044a50bd67b70399bbf06d",["sourceFrame"]="camera-d2-idle-wind-v04/frame-01.png",["pixelFoot"]=new JArray(904,1223),
    ["anchors"]=new JArray(new JObject{["id"]="Foot",["role"]="FOOT",["positionMm"]=new JArray(0,0,0)}),["liveLayout"]=new JObject{["originMm"]=Mm(position),["relativeToInitialFrontMm"]=new JArray((long)Mm(position)[0]-front,(long)Mm(position)[1],(long)Mm(position)[2]),["basis"]=new JArray(new JArray(1,0,0),new JArray(0,1,0),new JArray(0,0,1)),["carOrder"]=2,["formationSlot"]=4},
    ["generation"]=null,["missing"]=new JArray("Approved art-to-mm scale and depth","EMISSION and HIT local metric anchors","Own HIT_VOLUME capsule","Actual run entity generation","Approved right-facing original"),["runtimeRegistered"]=false,["sourceArtModified"]=false};
   File.WriteAllText(Root+"QA/ObstacleTrial-20261106/requested-roster-01/nyx-foot-registration.json",report.ToString());
  }finally{if(output!=null)UnityEngine.Object.DestroyImmediate(output);PrefabUtility.UnloadPrefabContents(loaded);}
 }
}
#endif
