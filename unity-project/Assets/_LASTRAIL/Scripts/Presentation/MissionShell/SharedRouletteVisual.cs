using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using LastRail.Auth;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    /// <summary>Shared, display-only composition. Receipts and quantities remain owned by the caller.</summary>
    public sealed class SharedRouletteVisual : MonoBehaviour
    {
        static readonly int[] Segments = { 4, 3, 2, 3, 4, 2, 3, 2 };
        const float SpinDurationSeconds = 6.5f;
        const float CountDurationSeconds = .6f;
        const int SpinRevolutions = 8;
        enum RevealPhase { Waiting, Spinning, Counting, Complete }
        readonly RouletteRewardCountup countup = new RouletteRewardCountup();
        readonly List<TMP_Text> quantityLabels = new List<TMP_Text>();
        readonly List<Graphic> rewardGlows = new List<Graphic>();
        readonly List<TMP_Text> segmentLabels = new List<TMP_Text>();
        readonly List<SharedRouletteSectorGraphic> sectors = new List<SharedRouletteSectorGraphic>();
        readonly List<SharedRouletteSectorGraphic> segmentLights = new List<SharedRouletteSectorGraphic>();
        int[] displayedMultipliers = (int[])Segments.Clone();
        TMP_FontAsset font;
        TMP_Text multiplierLabel, overflowHint;
        Material multiplierNeonMaterial;
        Material multiplierBloomMaterial;
        TMP_Text multiplierBloom;
        RouletteAmbientLightGraphic hubTextSpill;
        RectTransform rewardContent;
        ScrollRect rewardScroll;
        string displayedRows;
        RectTransform rotor;
        string lastReceipt;
        float phaseBegan;
        Coroutine revealRoutine;
        RevealPhase phase;
        bool sawWaiting, hasResult;
        bool spinning => phase == RevealPhase.Spinning;
        bool rewardsVisible = true;
        public bool IsAnimating => phase == RevealPhase.Spinning || phase == RevealPhase.Counting;
        public event Action RevealCompleted;
        public bool RequiresRewardScroll => rewardScroll.horizontal;
        int displayedWinner;

        public static SharedRouletteVisual Create(Transform parent, TMP_FontAsset font)
        {
            var root = AuthFormVisuals.Rect("SharedRouletteVisual", parent);
            AuthFormVisuals.Place(root, 0, 0, 800, 980);
            var view = root.gameObject.AddComponent<SharedRouletteVisual>();
            view.font = font;
            view.BuildWheel();
            view.BuildRewardViewport();
            view.Present(null, null);
            view.sawWaiting = false;
            return view;
        }

        public void ConfigureMultipliers(int[] allowed)
        {
            var filtered = new List<int>();
            foreach (int value in Segments)
                if (allowed == null || Array.IndexOf(allowed, value) >= 0) filtered.Add(value);
            displayedMultipliers = filtered.ToArray();
            RenderSegments();
        }

        public void SetRewardsVisible(bool visible)
        {
            rewardsVisible = visible;
            RefreshReveal();
        }

        public void Present(JObject result, JArray rewardLines)
        {
            displayedWinner = result?["multiplier"]?.Value<int>() ?? 0;
            hasResult = result != null;
            if (result == null)
            {
                StopReveal();
                countup.CapturePreview(rewardLines);
                sawWaiting = true;
                lastReceipt = null;
                phase = RevealPhase.Waiting;
                ResetRotor();
            }
            else
            {
                string receipt = (string)result["rewardTransactionId"] ?? (string)result["rewardClaimId"]
                    ?? result.ToString(Newtonsoft.Json.Formatting.None);
                if (receipt != lastReceipt)
                {
                    StopReveal();
                    lastReceipt = receipt;
                    countup.Begin(result["rewardLines"] as JArray);
                    phase = sawWaiting && Application.isPlaying && isActiveAndEnabled ? RevealPhase.Spinning : RevealPhase.Complete;
                    phaseBegan = Time.unscaledTime;
                    ResetRotor();
                }
                sawWaiting = false;
            }
            // A committed receipt always takes precedence over the preview supplied by the caller.
            var rows = result?["rewardLines"] as JArray ?? rewardLines;
            var signature = rows?.ToString(Newtonsoft.Json.Formatting.None) ?? "null";
            if (signature != displayedRows)
            {
                displayedRows = signature;
                if (hasResult) countup.Begin(rows);
                RebuildRewardCards(rows);
            }
            ApplyQuantities();
            RefreshReveal();
            if (IsAnimating && revealRoutine == null) revealRoutine = StartCoroutine(Reveal());
        }

        void RenderSegments()
        {
            int offset = Array.IndexOf(displayedMultipliers, displayedWinner);
            if (offset < 0) offset = 0;
            for (int i = 0; i < segmentLabels.Count; i++)
            {
                segmentLabels[i].text = displayedMultipliers.Length == 0 ? "—" : displayedMultipliers[(i + offset) % displayedMultipliers.Length] + "배";
                segmentLabels[i].color = hasResult && !spinning && i == 0 ? Color.white : new Color(.55f, .83f, 1);
                sectors[i].Selected = hasResult && !spinning && i == 0;
                segmentLights[i].Selected = hasResult && !spinning && i == 0;
                sectors[i].SetVerticesDirty();
                segmentLights[i].SetVerticesDirty();
            }
        }

        IEnumerator Reveal()
        {
            while (spinning)
            {
                yield return null;
                float progress = Mathf.Clamp01((Time.unscaledTime - phaseBegan) / SpinDurationSeconds);
                rotor.localRotation = Quaternion.Euler(0, 0, 360 * SpinRevolutions * (1 - Mathf.Pow(1 - progress, 3)));
                KeepSegmentLabelsUpright();
                if (progress >= 1)
                {
                    ResetRotor();phase=RevealPhase.Counting;phaseBegan=Time.unscaledTime;
                    ApplyQuantities();RefreshReveal();
                }
            }
            while (phase == RevealPhase.Counting)
            {
                yield return null;
                if (Time.unscaledTime - phaseBegan >= CountDurationSeconds) phase=RevealPhase.Complete;
                ApplyQuantities();
            }
            RefreshReveal();
            // Release ownership before notifying; a subscriber may start a new receipt immediately.
            revealRoutine = null;
            RevealCompleted?.Invoke();
        }

        void StopReveal()
        {
            if (revealRoutine == null) return;
            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }

        void KeepSegmentLabelsUpright()
        {
            if (rotor == null) return;
            var counterRotation = Quaternion.Inverse(rotor.localRotation);
            foreach (var label in segmentLabels) label.rectTransform.localRotation = counterRotation;
        }

        void ResetRotor()
        {
            if (rotor != null) rotor.localRotation = Quaternion.identity;
            KeepSegmentLabelsUpright();
        }

        void ApplyQuantities()
        {
            if (!hasResult) return;
            double progress=phase==RevealPhase.Complete?1:phase==RevealPhase.Counting?Mathf.Clamp01((Time.unscaledTime-phaseBegan)/CountDurationSeconds):0;
            for(int i=0;i<quantityLabels.Count&&i<countup.Count;i++) quantityLabels[i].text=countup.Text(i,progress);
        }

        public void CancelAndClear()
        {
            StopReveal();
            phase=RevealPhase.Waiting;sawWaiting=hasResult=false;lastReceipt=null;displayedRows=null;displayedWinner=0;
            countup.Clear();ResetRotor();
            foreach(var label in quantityLabels)if(label!=null)label.text="";
            if(rewardContent!=null)rewardContent.gameObject.SetActive(false);
            if(multiplierLabel!=null)RefreshReveal();
        }

        void RefreshReveal()
        {
            bool revealed = hasResult && !spinning;
            multiplierLabel.text = revealed ? "×" + displayedWinner : "LAST\nRAIL";
            multiplierLabel.fontSizeMax = multiplierLabel.fontSize = revealed ? 106 : 38;
            multiplierBloom.gameObject.SetActive(revealed);
            hubTextSpill.gameObject.SetActive(revealed);
            multiplierBloom.text = multiplierLabel.text;
            multiplierBloom.fontSizeMax = multiplierBloom.fontSize = multiplierLabel.fontSizeMax;
            rewardScroll.gameObject.SetActive(rewardsVisible);
            overflowHint.gameObject.SetActive(rewardsVisible);
            foreach(var glow in rewardGlows)glow.gameObject.SetActive(IsAnimating);
            RenderSegments();
        }

        void OnDisable() { StopReveal();phase=hasResult?RevealPhase.Complete:RevealPhase.Waiting;ResetRotor();ApplyQuantities();sawWaiting=false;if(multiplierLabel!=null)RefreshReveal(); }

        void OnDestroy()
        {
            StopReveal();
            foreach(var owned in new[]{multiplierNeonMaterial,multiplierBloomMaterial})
            {if(owned==null)continue;if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned);}
        }

        void BuildWheel()
        {
            var wheel = Rect("Wheel", transform, 60, 20, 680, 680);
            var metal = LastRail.Resources.PreparedContent.Load<Texture2D>("NoncombatCodex/codex-dark-steel-surface-v01");
            var glass = LastRail.Resources.PreparedContent.Load<Texture2D>("EntryPopup/roulette-glass-v02");
            Circle("SteelBacking", wheel, 0, 0, 680, .465f, 0, new Color(.23f, .29f, .34f), metal);
            Circle("OuterBlueHalo", wheel, 0, 0, 680, .426f, .402f, new Color(.05f, .6f, 1), null);
            rotor = AuthFormVisuals.Rect("Rotor", wheel);
            for (int i = 0; i < Segments.Length; i++)
            {
                var sector = AuthFormVisuals.Rect("Segment" + i, rotor).gameObject.AddComponent<SharedRouletteSectorGraphic>();
                sector.Angle = 90 - i * 45;
                sector.texture = glass;
                sector.raycastTarget = false;
                sectors.Add(sector);
                var light = AuthFormVisuals.Rect("SegmentLight" + i, rotor).gameObject.AddComponent<SharedRouletteSectorGraphic>();
                light.Angle = sector.Angle; light.EdgesOnly = true; light.raycastTarget = false;
                segmentLights.Add(light);
                float angle = (90 - i * 45) * Mathf.Deg2Rad;
                var text = Label("Multiplier" + i, rotor, "", 340 + Mathf.Cos(angle) * 190 - 62,
                    340 - Mathf.Sin(angle) * 190 - 30, 124, 60, 41);
                // Counter-rotate around the text center without moving it away from its sector.
                text.rectTransform.pivot = new Vector2(.5f, .5f);
                text.rectTransform.anchoredPosition += new Vector2(62, -30);
                text.fontStyle = FontStyles.Normal;
                segmentLabels.Add(text);
            }
            // The imported mechanical rim and pointer are independent alpha resources.
            Texture("MechanicalRing", wheel, "EntryPopup/roulette-ring", 0, 0, 680, 680);
            Circle("HubOcclusion", wheel, 190, 196, 300, .5f, 0, new Color(0, .01f, .02f, .65f), null);
            for (int glow = 6; glow >= 1; glow--)
                Circle("HubHalo" + glow, wheel, 199 - glow, 199 - glow, 282 + glow * 2,
                    .5f, .475f, new Color(0, .6f, 1, .07f), null);
            Texture("HubMechanical", wheel, "EntryPopup/roulette-hub-v01", 199, 199, 282, 282);
            hubTextSpill=Rect("HubTextSpill",wheel,231,261,218,158).gameObject.AddComponent<RouletteAmbientLightGraphic>();
            hubTextSpill.color=new Color(0,.68f,1,.25f);hubTextSpill.raycastTarget=false;
            multiplierBloom=Label("HubTextBloom",wheel,"",246,271,188,138,106);
            multiplierLabel = Label("CommittedMultiplier", wheel, "LAST\nRAIL", 246, 271, 188, 138, 94);
            ConfigureMultiplierNeon();
            Texture("Pointer", wheel, "EntryPopup/roulette-pointer", 267, -28, 146, 134);
            var pointerLight = Rect("PointerCyanInset", wheel, 317, 16, 46, 49).gameObject.AddComponent<SharedRoulettePointerGraphic>();
            pointerLight.color = new Color(.12f, .91f, 1); pointerLight.raycastTarget = false;
        }

        void ConfigureMultiplierNeon()
        {
            // Each view owns its material; changing a receipt never mutates the shared font asset.
            multiplierNeonMaterial = new Material(multiplierLabel.fontSharedMaterial);
            multiplierNeonMaterial.name = "Roulette multiplier neon (owned)";
            multiplierNeonMaterial.SetColor("_FaceColor", new Color(.025f, .13f, .19f, 1));
            multiplierNeonMaterial.SetColor("_OutlineColor", new Color(.72f, 1, 1, 1));
            multiplierNeonMaterial.SetFloat("_OutlineWidth", .10f);
            multiplierNeonMaterial.EnableKeyword("OUTLINE_ON");
            if (multiplierNeonMaterial.HasProperty("_UnderlayColor"))
            {
                multiplierNeonMaterial.EnableKeyword("UNDERLAY_ON");
                multiplierNeonMaterial.SetColor("_UnderlayColor", new Color(0, .72f, 1, 1));
                multiplierNeonMaterial.SetFloat("_UnderlayOffsetX", 0);
                multiplierNeonMaterial.SetFloat("_UnderlayOffsetY", 0);
                multiplierNeonMaterial.SetFloat("_UnderlayDilate", .36f);
                multiplierNeonMaterial.SetFloat("_UnderlaySoftness", .5f);
            }
            multiplierLabel.fontSharedMaterial = multiplierNeonMaterial;
            multiplierLabel.color = Color.white;
            multiplierLabel.extraPadding = true;
            // A separate soft text silhouette supplies bloom without blurring the readable core.
            multiplierBloomMaterial=new Material(multiplierNeonMaterial){name="Roulette multiplier bloom (owned)"};
            multiplierBloomMaterial.SetColor("_FaceColor",new Color(0,.7f,1,.2f));
            multiplierBloomMaterial.SetColor("_OutlineColor",new Color(0,.65f,1,.7f));
            multiplierBloomMaterial.SetFloat("_OutlineWidth",.35f);
            multiplierBloomMaterial.SetFloat("_OutlineSoftness",.8f);
            if(multiplierBloomMaterial.HasProperty("_UnderlayColor"))
            {
                multiplierBloomMaterial.SetColor("_UnderlayColor",new Color(0,.65f,1,.8f));
                multiplierBloomMaterial.SetFloat("_UnderlayDilate",.7f);
                multiplierBloomMaterial.SetFloat("_UnderlaySoftness",1);
            }
            multiplierBloom.fontSharedMaterial=multiplierBloomMaterial;
            multiplierBloom.extraPadding=true;multiplierBloom.raycastTarget=false;
        }

        void BuildRewardViewport()
        {
            var scrollRoot = Rect("Rewards", transform, 30, 710, 740, 240);
            rewardScroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            rewardScroll.horizontal = true; rewardScroll.vertical = false;
            rewardScroll.movementType = ScrollRect.MovementType.Clamped;
            rewardScroll.scrollSensitivity = 38;
            var viewport = AuthFormVisuals.Rect("Viewport", scrollRoot);
            var input = viewport.gameObject.AddComponent<Image>(); input.color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            rewardContent = Rect("Content", viewport, 0, 0, 740, 240);
            rewardScroll.viewport = viewport; rewardScroll.content = rewardContent;
            overflowHint = Label("MoreRewardsHint", transform, "", 0, 953, 800, 27, 18);
            overflowHint.color = new Color(.62f, .77f, .81f);
        }

        void RebuildRewardCards(JArray rows)
        {
            quantityLabels.Clear();rewardGlows.Clear();rewardContent.gameObject.SetActive(true);
            int count = rows?.Count ?? 0;
            for(int i=rewardContent.childCount-1;i>=0;i--){var child=rewardContent.GetChild(i);if(child.name=="EmptyRewards"){child.gameObject.SetActive(false);continue;}if(child.name.StartsWith("RewardCard")&&int.TryParse(child.name.Substring(10),out int index)&&index>=count){child.gameObject.SetActive(false);child.SetParent(null,false);if(Application.isPlaying)Destroy(child.gameObject);else DestroyImmediate(child.gameObject);}}            float baseWidth = count > 3 ? 238 : count > 0 ? (740 - (count - 1) * 12) / count : 740;
            rewardContent.sizeDelta = new Vector2(740, 240);
            rewardContent.anchoredPosition = Vector2.zero;
            rewardScroll.StopMovement(); rewardScroll.horizontalNormalizedPosition = 0;
            rewardScroll.horizontal = false;
            overflowHint.text = "";
            if (count == 0)
            {
                var empty=rewardContent.Find("EmptyRewards")?.gameObject;if(empty!=null)empty.SetActive(true);else Label("EmptyRewards", rewardContent, "보상 내역 확인 중", 0, 90, 740, 50, 26);
                return;
            }
            // Keep each server row, including repeated IDs: no client aggregation or multiplication.
            float nextX = 0;
            for (int i = 0; i < count; i++)
            {
                var row = rows[i] as JObject;
                // TMP must Awake under the active visual before measuring newly received rows.
                // Attach to the viewport after measuring so disabled parents cannot defer TMP initialization.
                var card=rewardContent.Find("RewardCard"+i) as RectTransform;bool created=card==null;if(created)card=Rect("RewardCard"+i,transform,nextX,0,baseWidth,240);
                string amount = row?["quantity"]?.Type == JTokenType.Integer
                    ? row["quantity"].Value<long>().ToString("N0", CultureInfo.InvariantCulture) : "—";
                var quantity=card.Find("RewardQuantity")?.GetComponent<TMP_Text>()??Label("RewardQuantity",card,"",14,184,baseWidth-28,44,38);quantity.text=amount;
                quantityLabels.Add(quantity);
                // Preserve exact digits and normal type size; overflow belongs to the scroll viewport.
                quantity.enableAutoSizing = false;
                // Independent digit advances make the widest-digit bound valid for every intermediate value.
                // Keep one fixed card width for the whole animation; never shrink, clip or resize as digits change.
                quantity.fontFeatures = new List<UnityEngine.TextCore.OTL_FeatureTag>();
                Vector2 preferred = QuantityEnvelope(quantity, amount);
                float width = Mathf.Max(baseWidth, Mathf.Ceil(preferred.x) + 32);
                float quantityHeight = Mathf.Max(44, Mathf.Ceil(preferred.y) + 2);
                card.SetParent(rewardContent, false);
                AuthFormVisuals.Place(card, nextX, 0, width, 240);
                AuthFormVisuals.Place(quantity.rectTransform, 14, 206 - quantityHeight / 2, width - 28, quantityHeight);
                nextX += width + 12;
                if(created){var surface = IndependentMetalSurface.Create(card, false, new Color(.57f, .59f, .6f));
                surface.Frame.gameObject.SetActive(false);RouletteInsetFrameGraphic.Create(card,false);
                surface.Backplate.Inset = new Vector2(9, 9); surface.Backplate.Bevel = 8;
                surface.Lights.gameObject.SetActive(false);
                var glow=AuthFormVisuals.Rect("RewardSpinGlow",card).gameObject.AddComponent<RouletteButtonGlowGraphic>();
                glow.color=new Color(.45f,.7f,1,.3f);glow.raycastTarget=false;}rewardGlows.Add(card.Find("RewardSpinGlow").GetComponent<RouletteButtonGlowGraphic>());
                string id = (string)row?["id"] ?? (string)row?["kind"] ?? "";
                var name=card.Find("RewardName")?.GetComponent<TMP_Text>()??Label("RewardName",card,"",15,14,width-30,39,25);name.text=RewardName(id,(string)row?["kind"]);AuthFormVisuals.Place(name.rectTransform,15,14,width-30,39);
                float iconWidth = Mathf.Min(190, width - 32);
                RewardIconPresentation.Create(card, font, id, new Rect((width - iconWidth) / 2, 43, iconWidth, 154));
                quantity.transform.SetAsLastSibling();
            }
            float contentWidth = Mathf.Max(740, nextX - 12);
            rewardContent.sizeDelta = new Vector2(contentWidth, 240);
            rewardScroll.horizontal = contentWidth > 740;
            overflowHint.text = rewardScroll.horizontal ? "◀  좌우로 밀어 전체 보상 확인  ▶" : "";
        }

        static Vector2 QuantityEnvelope(TMP_Text label, string formattedMaximum)
        {
            Vector2 size=label.GetPreferredValues(formattedMaximum,Mathf.Infinity,Mathf.Infinity);
            float digitWidth=0;
            for(char digit='0';digit<='9';digit++)
                digitWidth=Mathf.Max(digitWidth,label.GetPreferredValues(digit.ToString(),Mathf.Infinity,Mathf.Infinity).x);
            float bound=0;
            foreach(char symbol in formattedMaximum)
                bound+=symbol>='0'&&symbol<='9'?digitWidth:label.GetPreferredValues(symbol.ToString(),Mathf.Infinity,Mathf.Infinity).x;
            size.x=Mathf.Max(size.x,bound);
            return size;
        }

        static string RewardName(string id, string kind)
        {
            switch (id)
            {
                case "cur_credits": return "크레딧";
                case "cur_machine_parts": return "기계 부품";
                case "cur_operator_exp": return "대원 경험치";
                case "cur_tactical_data": return "전술 자료";
                case "cur_command_ticket": return "지휘 티켓";
                case "cur_special_core": return "특수 코어";
                default: return kind == "CRYSTAL" ? "크리스탈" : string.IsNullOrEmpty(id) ? "보상" : id;
            }
        }

        TMP_Text Label(string name, Transform parent, string value, float x, float y, float w, float h, float size)
        {
            var text = AuthFormVisuals.Text(parent, name, value, font, size, false, TextAlignmentOptions.Center);
            AuthFormVisuals.Place(text.rectTransform, x, y, w, h);
            text.color = new Color(.94f, .96f, .97f);
            text.enableAutoSizing = true; text.fontSizeMin = Mathf.Min(16, size); text.fontSizeMax = size;
            return text;
        }

        static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var rect = AuthFormVisuals.Rect(name, parent); AuthFormVisuals.Place(rect, x, y, w, h); return rect;
        }

        static void Texture(string name, Transform parent, string path, float x, float y, float w, float h)
        {
            var image = Rect(name, parent, x, y, w, h).gameObject.AddComponent<RawImage>();
            image.texture = LastRail.Resources.PreparedContent.Load<Texture2D>(path); image.raycastTarget = false;
            image.enabled = image.texture != null;
        }

        static void Circle(string name, Transform parent, float x, float y, float size, float radius, float inner, Color color, UnityEngine.Texture texture)
        {
            var image = Rect(name, parent, x, y, size, size).gameObject.AddComponent<RouletteSurfaceGraphic>();
            image.Radius = radius; image.InnerRadius = inner; image.color = color;
            image.texture = texture; image.EdgeBrightness = .8f; image.raycastTarget = false;
        }
    }

    /// <summary>One textured annular wedge. Geometry, light and border are presentation only.</summary>
    public sealed class SharedRouletteSectorGraphic : RawImage
    {
        public float Angle;
        public bool Selected;
        public bool EdgesOnly;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); var rect = rectTransform.rect;
            float diameter = Mathf.Min(rect.width, rect.height);
            if (diameter <= 0) return;
            Vector2 Point(float radius, float degrees) => rect.center + new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad)) * radius * diameter;
            void GradientStrip(float inner, float outer, float start, float end, Color innerShade, Color outerShade)
            {
                const int steps = 18;
                for (int s = 0; s < steps; s++)
                {
                    float a = Mathf.Lerp(start, end, s / (float)steps), b = Mathf.Lerp(start, end, (s + 1) / (float)steps);
                    int first = mesh.currentVertCount;
                    var points = new[] { Point(inner, a), Point(outer, a), Point(outer, b), Point(inner, b) };
                    for (int v = 0; v < points.Length; v++)
                    {
                        var point = points[v];
                        mesh.AddVert(point, v == 0 || v == 3 ? innerShade : outerShade,
                            new Vector2((point.x - rect.x) / rect.width, (point.y - rect.y) / rect.height));
                    }
                    mesh.AddTriangle(first, first + 1, first + 2); mesh.AddTriangle(first, first + 2, first + 3);
                }
            }
            void Strip(float inner, float outer, float start, float end, Color shade) =>
                GradientStrip(inner, outer, start, end, shade, shade);
            float start = Angle - 21.8f, end = Angle + 21.8f;
            if (!EdgesOnly)
            {
                // Smooth radial tint remains useful even if the optional glass resource is unavailable.
                Color inner = texture != null ? new Color(.35f, .75f, 1) : new Color(.015f, .105f, .21f);
                Color middle = texture != null ? new Color(.35f, .55f, .85f) : new Color(.01f, .035f, .105f);
                Color outer = texture != null ? new Color(.65f, .85f, 1) : new Color(.015f, .21f, .37f);
                if (Selected)
                {
                    inner = new Color(.55f, .97f, 1);
                    middle = new Color(.17f, .62f, .9f);
                    outer = new Color(.65f, 1, 1);
                }
                GradientStrip(.165f, .275f, start, end, inner, middle);
                GradientStrip(.275f, .429f, start, end, middle, outer);
                return;
            }
            // Transparent inner light bands keep the body readable instead of filling the winner solid cyan.
            var clearBlue = new Color(0, .46f, 1, 0);
            if(Selected)
            {
                GradientStrip(.224f,.292f,start+1,end-1,new Color(.01f,.42f,.8f,.30f),new Color(.01f,.16f,.45f,.22f));
                GradientStrip(.292f,.354f,start+1,end-1,new Color(.01f,.16f,.45f,.22f),new Color(.04f,.55f,1,.34f));
            }
            var reflectedBlue = new Color(.02f, .67f, 1, Selected ? .85f : .18f);
            // Keep illumination in the visible glass annulus, inside the rim and outside the hub.
            GradientStrip(.218f, .265f, start + 1, end - 1, reflectedBlue, clearBlue);
            GradientStrip(.315f, .364f, start + 1, end - 1, clearBlue, reflectedBlue);
            GradientStrip(.318f, .334f, start + 2, end - 2, new Color(.12f, .53f, 1, .09f), clearBlue);
            for(int glow=9;glow>=1;glow--)
            {
                float spread=glow*.003f;var cyan=new Color(.02f,.65f,1,Selected ? .11f : .022f);
                Strip(.354f-spread,.361f+spread,start+1,end-1,cyan);
                Strip(.216f-spread,.222f+spread,start+1,end-1,cyan);
                Strip(.218f,.366f,start-glow*.13f,start+.65f+glow*.13f,cyan);
                Strip(.218f,.366f,end-.65f-glow*.13f,end+glow*.13f,cyan);
            }
            // Each divider has a dark brass rail beside the luminous glass edge.
            var rail = new Color(.24f, .20f, .13f, 1);
            Strip(.18f, .426f, start - 1.6f, start + .1f, rail);
            Strip(.18f, .426f, end - .1f, end + 1.6f, rail);
            Strip(.18f, .426f, start - 1.6f, start - 1.1f, new Color(.62f,.49f,.29f));
            Strip(.18f, .426f, end + 1.1f, end + 1.6f, new Color(.07f,.065f,.05f));
            var light = Selected ? new Color(.72f, 1, 1) : new Color(.12f, .69f, 1);
            Strip(.356f, .360f, start + 1, end - 1, light);
            Strip(.218f, .222f, start + 1, end - 1, light);
            Strip(.18f, .414f, start, start + .65f, light);
            Strip(.18f, .414f, end - .65f, end, light);
            if (Selected)
            {
                GradientStrip(.325f,.356f,start+1,end-1,clearBlue,new Color(.65f,1,1,.85f));
                Strip(.353f, .362f, start + 1, end - 1, Color.white);
                Strip(.218f, .366f, start, start + .8f, Color.white);
                Strip(.218f, .366f, end - .8f, end, Color.white);
            }
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SharedRoulettePointerGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); var r = rectTransform.rect;
            Vector2 center = new Vector2(r.center.x, Mathf.Lerp(r.yMin, r.yMax, 2f / 3f));
            var corners = new[] { new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax), new Vector2(r.center.x, r.yMin) };
            void Ring(float outer, float inner, Color shade)
            {
                for (int i = 0; i < 3; i++)
                {
                    int first = mesh.currentVertCount, next = (i + 1) % 3;
                    mesh.AddVert(center + (corners[i] - center) * outer, shade, Vector2.zero);
                    mesh.AddVert(center + (corners[next] - center) * outer, shade, Vector2.zero);
                    mesh.AddVert(center + (corners[next] - center) * inner, shade, Vector2.zero);
                    mesh.AddVert(center + (corners[i] - center) * inner, shade, Vector2.zero);
                    mesh.AddTriangle(first, first + 1, first + 2); mesh.AddTriangle(first, first + 2, first + 3);
                }
            }
            for (int glow = 5; glow >= 1; glow--)
                Ring(1 + glow * .045f, .66f - glow * .035f, new Color(0, .55f, 1, .07f));
            Ring(.81f, 0, new Color(.015f, .07f, .11f, 1));
            Ring(1, .77f, color);
            Ring(.94f, .87f, new Color(.76f, 1, 1, 1));
        }
    }
}
