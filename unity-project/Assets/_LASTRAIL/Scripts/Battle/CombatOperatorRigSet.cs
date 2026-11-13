using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Battle {
// Explicit asset references keep approved source art/data in player builds.
[CreateAssetMenu(menuName="LAST RAIL/Combat Operator Rigs")]
public sealed class CombatOperatorRigSet:ScriptableObject {
 public string[] OperatorIds=Array.Empty<string>();
 public Texture2D[] Sources=Array.Empty<Texture2D>();
 public TextAsset[] Definitions=Array.Empty<TextAsset>();
 public void Install(ApprovedCombatView view){
  if(OperatorIds.Length!=Sources.Length||OperatorIds.Length!=Definitions.Length)throw new InvalidOperationException("Incomplete operator rig registry");
  for(int i=0;i<OperatorIds.Length;i++){
   if(!view.HasOperatorRigTarget(OperatorIds[i]))continue;
   if(Sources[i]==null||Definitions[i]==null)throw new InvalidOperationException("Missing approved operator source: "+OperatorIds[i]);
   view.SetCaptureOperatorRig(OperatorIds[i],Sources[i],JObject.Parse(Definitions[i].text));
  }
  view.EnableCaptureRigMotion();view.SetCaptureCabinSpread();
 }
}
}
