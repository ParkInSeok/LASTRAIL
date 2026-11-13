using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // One visual state owns background, icon and label in both lobby and child pages.
    public sealed class LobbyTabVisual : MonoBehaviour
    {
        public bool IsSelected { get; private set; }
        Material iconMaterial;
        Sprite plate;
        public static void ApplyText(Button button, TMP_Text label, bool selected){Apply(button,null,label,selected);}
        public static void Apply(Button button, RawImage icon, TMP_Text label, bool selected, bool independentIcon=false)
        {
            var visual=button.gameObject.AddComponent<LobbyTabVisual>();
            visual.IsSelected=selected;
            button.GetComponent<Image>().color=selected?new Color(.025f,.19f,.32f,1):new Color(.027f,.032f,.038f,1);
            label.color=selected?new Color(.88f,.98f,1,1):new Color(.70f,.68f,.61f,1);
            label.fontStyle=selected?FontStyles.Bold:FontStyles.Normal;
            if(icon!=null&&!independentIcon){
            // Home uses the neutral icon from the operators atlas, never the baked selected tile.
            if(button.name=="Navigation0" || button.name=="PageNav0")icon.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("DestinationPages/operators-atlas");
            var shader=LastRail.Resources.PreparedContent.Load<Shader>("DestinationPages/TabIcon");
            visual.iconMaterial=new Material(shader);
            icon.material=visual.iconMaterial;
            icon.color=selected?new Color(.8f,.96f,1,1):new Color(.70f,.68f,.61f,1);
            }
            var oldFrame=button.transform.Find("MetalFrame")??button.transform.Find("Frame");
            if(oldFrame!=null)oldFrame.gameObject.SetActive(false);
            var underline=button.transform.Find("ActiveUnderline");if(underline!=null)underline.gameObject.SetActive(false);
            var outline=button.GetComponent<Outline>();if(outline!=null)outline.enabled=false;
            var atlas=LastRail.Resources.PreparedContent.Load<Texture2D>("DestinationPages/tab-states-v01");
            float x=selected?.507f:.008f;
            var crop=new Rect(x*atlas.width,.157f*atlas.height,.485f*atlas.width,.73f*atlas.height);
            visual.plate=Sprite.Create(atlas,crop,new Vector2(.5f,.5f),600,0,SpriteMeshType.FullRect,new Vector4(crop.width*.12f,crop.height*.19f,crop.width*.12f,crop.height*.17f));
            visual.plate.name=selected?"TabSelected":"TabIdle";
            var background=button.GetComponent<Image>();background.sprite=visual.plate;background.type=Image.Type.Sliced;background.color=Color.white;
            // Leave space for the inset frame and bottom lamp; center the icon-label group.
            if(icon!=null){
            icon.rectTransform.anchoredPosition=new Vector2(63,-24);icon.rectTransform.sizeDelta=new Vector2(55,45);
            label.rectTransform.anchoredPosition=new Vector2(18,-70);label.rectTransform.sizeDelta=new Vector2(145,32);label.fontSizeMax=26;label.fontSize=26;
            var shadow=icon.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.7f);shadow.effectDistance=new Vector2(1,-2);
            }else{
                var area=((RectTransform)button.transform).rect;
                label.rectTransform.anchorMin=label.rectTransform.anchorMax=label.rectTransform.pivot=new Vector2(.5f,.5f);
                label.rectTransform.anchoredPosition=new Vector2(0,2);label.rectTransform.sizeDelta=new Vector2(area.width-42,area.height-25);
                label.fontSizeMax=28;label.fontSize=28;label.fontSizeMin=22;label.alignment=TextAlignmentOptions.Center;
            }
            var colors=button.colors;colors.highlightedColor=new Color(1,1,1,1);colors.pressedColor=new Color(.70f,.78f,.85f,1);colors.fadeDuration=.09f;button.colors=colors;
        }
        void OnDestroy(){if(plate!=null){if(Application.isPlaying)Destroy(plate);else DestroyImmediate(plate);}if(iconMaterial!=null){if(Application.isPlaying)Destroy(iconMaterial);else DestroyImmediate(iconMaterial);}}
    }
}



