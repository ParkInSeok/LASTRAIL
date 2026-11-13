using UnityEngine;
namespace LastRail.Battle {
 // References the preserved authoring sources, never a copied runtime geometry contract.
 public sealed class OrdinaryRoofPresentationAssets:ScriptableObject {
  public GameObject Prefab;
  public GameObject EnemyPrefab;
  public TextAsset LiveDefinition;
  public TextAsset Definition;
  public Texture2D[] CarBodies;
  public Rect[] CarBounds;
 }
}
