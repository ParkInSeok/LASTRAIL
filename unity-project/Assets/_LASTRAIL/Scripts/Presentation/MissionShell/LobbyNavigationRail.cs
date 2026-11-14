using System;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // The screen owns the borrowed frame/atlas; the rail owns only its input subscription.
    public sealed class LobbyNavigationRail : MonoBehaviour
    {
        Action<int> selected;
        Material iconMaterial;
        readonly RawImage[] icons=new RawImage[5];
        readonly Image[] fills=new Image[5];
        readonly GameObject[] highlights=new GameObject[5];
        public void SetSelection(int active)
        {
            for(int i=0;i<icons.Length;i++){
                fills[i].color=i==active?new Color(.025f,.3f,.48f,1):Color.clear;
                highlights[i].SetActive(i==active);icons[i].material=i==active?iconMaterial:null;
            }
        }
        public const string ObjectName = "UnifiedNavigation";
        public static RectTransform Create(Transform parent, Sprite frame, Texture atlas, int active, string prefix, Action<int> selected)
        {
            var root = Rect(ObjectName, parent, 14, 130, 112, 720);
            var view = root.gameObject.AddComponent<LobbyNavigationRail>();
            view.selected = selected;
            view.iconMaterial = new Material(LastRail.Resources.PreparedContent.Load<Shader>("DestinationPages/TabIcon"));
            var background = root.gameObject.AddComponent<Image>();
            background.color = new Color(.025f, .03f, .035f, 1);
            background.raycastTarget = false;
            int[] cells = { 10, 9, 11, 6, 13 };
            for (int i = 0; i < cells.Length; i++)
            {
                int index = i;
                var area = Rect(prefix + i, root, 7, 8 + i * 140, 98, 140);
                var fill = area.gameObject.AddComponent<Image>();
                fill.color = i == active ? new Color(.025f, .3f, .48f, 1) : Color.clear;
                var button = area.gameObject.AddComponent<Button>();
                button.targetGraphic = fill;
                button.onClick.AddListener(() => { if (view != null && view.isActiveAndEnabled) view.selected?.Invoke(index); });
                var glow=LobbyArtwork.BlueInset(area);glow.uvRect=new Rect(.1f,.24f,.8f,.52f);view.highlights[i]=glow.gameObject;
                var icon = Rect("Icon", area, 9, 30, 80, 80).gameObject.AddComponent<RawImage>();
                icon.texture = atlas;
                if(i==active)icon.material = view.iconMaterial;
                icon.uvRect = LobbyArtwork.IconUv(cells[i]);
                if(i==2||i==4){icon.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("LobbyDeck/navigation-shapes-v06");icon.uvRect=new Rect(i==2?.075f:.575f,.06f,.35f,.88f);}
                icon.color = Color.white;
                icon.raycastTarget = false;
                view.icons[i]=icon;view.fills[i]=fill;
                if (i > 0)
                {
                    var line = Rect("Divider", root, 8, 8 + i * 140, 96, 2).gameObject.AddComponent<Image>();
                    line.color = new Color(.65f, .47f, .22f, 1);
                    line.raycastTarget = false;
                }
            }
            var border = Rect("RailFrame", root, 0, 0, 112, 720).gameObject.AddComponent<Image>();
            border.sprite = frame;
            border.type = Image.Type.Sliced;
            border.fillCenter = false;
            border.raycastTarget = false;
            view.SetSelection(active);return root;
        }
        static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }
        void OnDestroy()
        {
            selected = null;
            if (iconMaterial != null) { if (Application.isPlaying) Destroy(iconMaterial); else DestroyImmediate(iconMaterial); }
        }
    }
}
