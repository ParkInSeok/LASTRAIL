#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using LastRail.Presentation.Audio;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
namespace LastRail.Editor {
 public static class BMeasuredAudioTrialAuthoring {
  static string Hash(string p){using(var sha=SHA256.Create())using(var f=File.OpenRead(p))return BitConverter.ToString(sha.ComputeHash(f)).Replace("-","").ToLowerInvariant();}
  public static void Configure(GameObject root,string folder,Camera camera,string output){
   const string original="D:/Project/LASTRAIL/QA/AudioProduction-20261020/battle-music-v03/approved-original.mp3";
   const string pinned="0d2f3aaa22670abdf1adea57c2d550aaf479624f3cfe3fff7beb719650d1d451";
   if(Hash(original)!=pinned)throw new InvalidOperationException("Approved BGM original hash mismatch.");
   string bgmPath=folder+"/approved-bgm-v03.mp3";File.Copy(original,bgmPath,false);AssetDatabase.ImportAsset(bgmPath,ImportAssetOptions.ForceSynchronousImport);
   const string contactOriginal="D:/Project/LASTRAIL/QA/AudioProduction-20261020/hit-local-v03/C.wav";
   const string contactHash="b434e431f78efa98e06c2d1998d1c3e2e30022cf1c4615dbb5248b5440f3bd43";
   if(Hash(contactOriginal)!=contactHash)throw new InvalidOperationException("Approved contact C hash mismatch.");
   string hitPath=folder+"/trial-contact-C.wav";
   File.Copy(contactOriginal,hitPath,false);AssetDatabase.ImportAsset(hitPath,ImportAssetOptions.ForceSynchronousImport);
   const string healPath="Assets/_LASTRAIL/Audio/Sfx/Combat/combat_heal.wav";
   var hit=AssetDatabase.LoadAssetAtPath<AudioClip>(hitPath);var heal=AssetDatabase.LoadAssetAtPath<AudioClip>(healPath);var bgm=AssetDatabase.LoadAssetAtPath<AudioClip>(bgmPath);
   if(!hit||!heal||!bgm)throw new InvalidOperationException("Trial clips missing.");
   AudioSource Source(string name){var go=new GameObject(name);go.transform.SetParent(root.transform,false);var s=go.AddComponent<AudioSource>();s.playOnAwake=false;s.volume=.5f;s.spatialBlend=0;go.AddComponent<LocalAudioOutputCapture>();return s;}
   // First Player trial observed enemy contact + own contact + recovery together.
   // Three supplied trial voices are evidence-driven QA capacity, not a new game tuning default.
   var music=Source("TrialApprovedMusic");var voices=new[]{Source("TrialVoice0"),Source("TrialVoice1"),Source("TrialVoice2")};
   var player=root.AddComponent<LastRailAudioCuePlayer>();
   player.ConfigureBindings(null,music,new[]{new LastRailAudioClipBinding(BMeasuredCombatAudio.MusicCue,bgm,true,1),
    new LastRailAudioClipBinding(BMeasuredCombatAudio.HitCue,hit,false,1),new LastRailAudioClipBinding(BMeasuredCombatAudio.HealCue,heal,false,1)});
   player.ConfigureCombatSources(voices,null);
   camera.gameObject.AddComponent<AudioListener>();var capture=camera.gameObject.AddComponent<LocalAudioOutputCapture>();
   var manifest=new JObject{["scope"]="EXPLICIT_LOCAL_AU01_TRIAL_NOT_FINAL_MIX",["bgmOriginal"]=original,["bgmHash"]=pinned,["bgmApproval"]="battle-music.approved.v03.20261020",
    ["loopSeamVerified"]=false,["bgmTrialShorterThanFirstLoop"]=true,["hit"]=hitPath,["hitHash"]=Hash(hitPath),["heal"]=healPath,["healHash"]=Hash(healPath),
    ["contactOriginal"]=contactOriginal,["contactApproval"]="hit-local-v03/verification.json: C가 낫네 이걸로 승인; timbre only",
    ["effectsStatus"]="APPROVED_C_CONTACT_TIMBRE_LOCAL_MAPPING_TRIAL_AND_PLACEHOLDER_HEAL",["sourceGain"]=.5,["gainStatus"]="TEMPORARY_TRIAL_NOT_APPROVED_TUNING",
    ["voiceCount"]=voices.Length,["voiceCountBasis"]="213 actual enemy contact plus own contact plus lifesteal; trial-only capacity",
    ["spatialPolicy"]="NEUTRAL_LOCAL_ONLY_NO_ORIGINAL_EVENT_POINT",["operatingAdmission"]=false};
   string path=folder+"/audio-trial-manifest.json";File.WriteAllText(path,manifest.ToString());File.WriteAllText(output+"/audio-trial-manifest.json",manifest.ToString());
   AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
   root.AddComponent<BMeasuredCombatAudio>().Initialize(player,voices,music,capture,AssetDatabase.LoadAssetAtPath<TextAsset>(path));
  }
 }
}
#endif
