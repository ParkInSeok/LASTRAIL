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
        readonly Button[] slots = new Button[10];
        readonly RawImage[] portraits = new RawImage[10];
        readonly GameObject[] empty = new GameObject[10], highlights = new GameObject[10];
        Action<int> selected;
        // Approved source art is reused; these are display crops, not newly drawn portraits.
        public static Rect PortraitCrop(string id,Texture2D texture)
        {
            if(id=="op_nyx")return new Rect(.12f,.18f,.76f,.76f);
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
            var rect = Rect(parent, "FormationPlan", 0, 0, 750, 620);
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
            var frame = PreparedContent.Load<Texture2D>("FormationPlan/slot-frame");
            var roof = PreparedContent.Load<Texture2D>("FormationPlan/combat-roof-v02");
            for(int row=0;row<2;row++)
            {
                float top=row*310;
                var panel=Rect(transform,"Encounter"+row,0,top,750,302).gameObject.AddComponent<Image>();
                panel.color=new Color(.025f,.04f,.045f,.93f);panel.raycastTarget=false;
                Label(transform,font,"EncounterTitle"+row,row==0?"← 적    좌측 교전 · 최후미 객차":"우측 교전 · 기관차 뒤 객차    적 →",8,top+3,734,34,24);
                Art(transform,"CombatRoof"+row,roof,5,top+40,740,247);
                for(int car=0;car<6;car++){
                    var marker=Rect(transform,"CarMarker"+row+"-"+car,175+car*65,top+284,58,9).gameObject.AddComponent<Image>();
                    marker.color=car==(row==0?0:4)?new Color(.1f,.85f,1):new Color(.55f,.43f,.23f);marker.raycastTarget=false;
                }
                Label(transform,font,"EngineDirection"+row,"▶",565,top+277,35,23,20);
                for (int slot = 0; slot < 5; slot++)
                {
                int logicalSlot=slot;int i=row*5+slot;float x=72+(row==0?slot:4-slot)*124;float y=top+(slot%2==0?145:70);
                var r = Rect(transform, (row==0?"FormationSlot":"RightFormationSlot") + slot, x, y, 100, 100);
                var plate = r.gameObject.AddComponent<Image>(); plate.color = new Color(.035f, .045f, .05f);
                var button = r.gameObject.AddComponent<Button>(); button.targetGraphic = plate; slots[i] = button;
                button.onClick.AddListener(() => { if (this != null && isActiveAndEnabled && button.interactable) selected?.Invoke(logicalSlot); });
                portraits[i] = Art(r, "Portrait", null, 6, 6, 88, 88);
                empty[i] = Label(r, font, "EmptyPlus", "+", 6, 6, 88, 88, 48).gameObject;
                Art(r, "Frame", frame, -4, -4, 108, 108);
                var highlight=Art(r,"Selected",frame,-4,-4,108,108);highlight.color=new Color(.08f,1,1);
                var glow=highlight.gameObject.AddComponent<Outline>();glow.effectColor=new Color(0,.8f,1,1);glow.effectDistance=new Vector2(4,4);glow.useGraphicAlpha=false;
                highlights[i] = highlight.gameObject;
                SelectionBorder(highlight.transform,108,108);
                var badge=Rect(r,"NumberPlate",32,-21,36,36).gameObject.AddComponent<RouletteSurfaceGraphic>();badge.Radius=.5f;badge.EdgeBrightness=1;badge.color=new Color(.035f,.045f,.05f);badge.raycastTarget=false;
                var rim=Rect(r,"NumberRim",32,-21,36,36).gameObject.AddComponent<RouletteSurfaceGraphic>();rim.Radius=.5f;rim.InnerRadius=.46f;rim.EdgeBrightness=1;rim.color=new Color(.8f,.7f,.5f);rim.raycastTarget=false;
                Label(r, font, "Number", (slot + 1).ToString(), 32, -21, 36, 36, 25);
                }
            }
        }
        public void Bind(Action<int> action) => selected = action;
        public void Unbind() => selected = null;
        void OnDisable() => Unbind();
        void OnDestroy() => Unbind();
        public void Render(IReadOnlyList<string> ids, int activeSlot = -1, bool editable = false)
        {
            if (ids == null || ids.Count != 5) throw new ArgumentException("Five logical positions required", nameof(ids));
            for (int i = 0; i < 10; i++)
            {
                int slot=i%5;
                var texture = Portrait(ids[slot]); portraits[i].texture = texture; portraits[i].enabled = texture != null;
                portraits[i].uvRect = PortraitCrop(ids[slot],texture);
                empty[i].SetActive(string.IsNullOrEmpty(ids[slot])); highlights[i].SetActive(slot == activeSlot);
                slots[i].interactable = editable && selected != null;
            }
        }
    }
}
