#if UNITY_EDITOR || LASTRAIL_EXPLICIT_LOCAL_QA_PLAYER
using System;
using LastRail.Battle;
using UnityEngine;
namespace LastRail.Diagnostics {
public sealed class CombatScenePlayerProbe:MonoBehaviour {
 [SerializeField] CombatPlayerAssetBinding binding;[SerializeField] TextAsset preparedSource;
 public void Initialize(CombatPlayerAssetBinding assets,TextAsset prepared){binding=assets;preparedSource=prepared;}
 async void Start(){try{await CombatScenePlayerTrial.Run(binding,preparedSource.text,Environment.GetEnvironmentVariable("LASTRAIL_COMBAT_SCENE_TRIAL"));Application.Quit(0);}catch(Exception error){Debug.LogException(error);Application.Quit(1);}}
}
}
#endif
