using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation {
 // Borrowed artwork; screen controllers retain input, data, and lifetime ownership.
 public sealed class ApprovedUiConnections:ScriptableObject {
  public Sprite DialogFrame,ButtonFrame,PanelFrame,ItemFrame;
  public Texture2D Medal,ButtonPlate,PanelTexture,MapBackground;
  public Texture2D Battle,Elite,Boss,Equipment,Enchant,CurrentLocation,Fuel,DepartureCabin,Pilot;
  public Texture2D MapIcon(string kind){switch(kind){case "COMBAT":return Battle;case "ELITE":return Elite;case "BOSS":return Boss;case "EQUIPMENT":return Equipment;case "ENCHANT":return Enchant;default:return null;}}
  public static Image Frame(Transform parent,string name,Sprite sprite,float multiplier){
   var node=new GameObject(name,typeof(RectTransform),typeof(Image));node.transform.SetParent(parent,false);
   var rect=(RectTransform)node.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
   var image=node.GetComponent<Image>();image.sprite=sprite;image.type=Image.Type.Sliced;image.fillCenter=false;image.pixelsPerUnitMultiplier=multiplier;image.raycastTarget=false;return image;
  }
 }
}
