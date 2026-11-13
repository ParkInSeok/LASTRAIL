using System;
using System.Collections.Generic;
using LastRail.Resources;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Borrows textures from the page scope. Logical slot IDs never depend on visual ordering.
    public sealed class FormationPlanView : MonoBehaviour
    {
        readonly Button[] slots = new Button[5];
        readonly RawImage[] portraits = new RawImage[5];
        readonly GameObject[] empty = new GameObject[5], highlights = new GameObject[5];
        Action<int> selected;
        // Approved source art is reused; these are display crops, not newly drawn portraits.
        public static Rect PortraitCrop(string id,Texture2D texture)
        {
            if(id=="op_nyx")return new Rect(.32f,.67f,.35f,.29f);
            if(id=="op_reina"||id=="op_kyle"||id=="op_eve"||id=="op_sera"||id=="op_mira")return new Rect(.20f,.38f,.63f,.60f);
            return OperatorVisualCatalog.Crop(texture,151,151);
        }
        public static void SelectionBorder(Transform parent,float width,float height)
        {
            Vector2[] points={new Vector2(9,0),new Vector2(width-9,0),new Vector2(width,9),new Vector2(width,height-9),new Vector2(width-9,height),new Vector2(9,height),new Vector2(0,height-9),new Vector2(0,9)};
            for(int i=0;i<points.Length;i++){
                var a=points[i];var b=points[(i+1)%points.Length];var delta=b-a;
                var r=Rect(parent,"SelectionEdge"+i,(a.x+b.x)/2,(a.y+b.y)/2,delta.magnitude,2);
                r.pivot=new Vector2(.5f,.5f);r.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
                var line=r.gameObject.AddComponent<Image>();line.color=new Color(.25f,.95f,1);line.raycastTarget=false;
                var glow=line.gameObject.AddComponent<Outline>();glow.effectColor=new Color(0,.65f,1,.6f);glow.effectDistance=new Vector2(2,2);
            }
        }
        public static Texture2D Portrait(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            string key = id=="op_mira"?"OperatorPortraits/mira":"FormationPlan/portrait-" + id.Replace("op_", "");
            return RemoteContentCatalog.Find(key) != null ? PreparedContent.Load<Texture2D>(key) : NumberedFormationView.Portrait(id);
        }
        public static FormationPlanView Create(Transform parent, TMP_FontAsset font)
        {
            var rect = Rect(parent, "FormationPlan", 0, 0, 1080, 450);
            var view = rect.gameObject.AddComponent<FormationPlanView>();
            view.Build(font);
            return view;
        }
        static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(width, height); return r;
        }
        static RawImage Art(Transform parent, string name, Texture2D texture, float x, float y, float w, float h)
        {
            var image = Rect(parent, name, x, y, w, h).gameObject.AddComponent<RawImage>();
            image.texture = texture; image.raycastTarget = false; return image;
        }
        static TMP_Text Label(Transform parent, TMP_FontAsset font, string name, string text, float x, float y, float w, float h, float size)
        {
            var t = Rect(parent, name, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font; t.text = text; t.fontSize = size; t.alignment = TextAlignmentOptions.Center;
            t.color = new Color(.94f, .88f, .73f); t.raycastTarget = false; return t;
        }
        void Build(TMP_FontAsset font)
        {
            Art(transform, "TrainPlanBackground", PreparedContent.Load<Texture2D>("FormationPlan/background"), 0, 0, 1080, 450);
            var frame = PreparedContent.Load<Texture2D>("FormationPlan/slot-frame");
            Vector2[] positions = { new Vector2(250, 55), new Vector2(460, 55), new Vector2(670, 55), new Vector2(355, 250), new Vector2(565, 250) };
            for (int i = 0; i < 5; i++)
            {
                int slot = i; var p = positions[i];
                var r = Rect(transform, "FormationSlot" + i, p.x, p.y, 175, 175);
                var plate = r.gameObject.AddComponent<Image>(); plate.color = new Color(.035f, .045f, .05f);
                var button = r.gameObject.AddComponent<Button>(); button.targetGraphic = plate; slots[i] = button;
                button.onClick.AddListener(() => { if (this != null && isActiveAndEnabled && button.interactable) selected?.Invoke(slot); });
                portraits[i] = Art(r, "Portrait", null, 12, 12, 151, 151);
                empty[i] = Label(r, font, "EmptyPlus", "+", 12, 12, 151, 151, 72).gameObject;
                Art(r, "Frame", frame, -7, -7, 189, 189);
                var highlight=Art(r,"Selected",frame,-7,-7,189,189);highlight.color=new Color(.08f,1,1);
                var glow=highlight.gameObject.AddComponent<Outline>();glow.effectColor=new Color(0,.8f,1,1);glow.effectDistance=new Vector2(4,4);glow.useGraphicAlpha=false;
                highlights[i] = highlight.gameObject;
                SelectionBorder(highlight.transform,189,189);
                var badge=Rect(r,"NumberPlate",66,-23,43,43).gameObject.AddComponent<RouletteSurfaceGraphic>();badge.Radius=.5f;badge.EdgeBrightness=1;badge.color=new Color(.035f,.045f,.05f);badge.raycastTarget=false;
                var rim=Rect(r,"NumberRim",66,-23,43,43).gameObject.AddComponent<RouletteSurfaceGraphic>();rim.Radius=.5f;rim.InnerRadius=.46f;rim.EdgeBrightness=1;rim.color=new Color(.8f,.7f,.5f);rim.raycastTarget=false;
                Label(r, font, "Number", (i + 1).ToString(), 66, -23, 43, 43, 29);
            }
        }
        public void Bind(Action<int> action) => selected = action;
        public void Unbind() => selected = null;
        void OnDisable() => Unbind();
        void OnDestroy() => Unbind();
        public void Render(IReadOnlyList<string> ids, int activeSlot = -1, bool editable = false)
        {
            if (ids == null || ids.Count != 5) throw new ArgumentException("Five logical positions required", nameof(ids));
            for (int i = 0; i < 5; i++)
            {
                var texture = Portrait(ids[i]); portraits[i].texture = texture; portraits[i].enabled = texture != null;
                portraits[i].uvRect = PortraitCrop(ids[i],texture);
                empty[i].SetActive(string.IsNullOrEmpty(ids[i])); highlights[i].SetActive(i == activeSlot);
                slots[i].interactable = editable && selected != null;
            }
        }
    }
}
