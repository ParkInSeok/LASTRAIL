using UnityEngine;
namespace LastRail.Battle {
public sealed class CombatSupportFireAssets:ScriptableObject {
 public Texture2D Hardware,Effects;
 public TextAsset Masks,HardwareMetadata,FxMetadata;
 public Material Additive;
 public Vector2 RoofAnchor=new Vector2(.54f,.31f);
 public float NativeScale=.4f;
}
}
