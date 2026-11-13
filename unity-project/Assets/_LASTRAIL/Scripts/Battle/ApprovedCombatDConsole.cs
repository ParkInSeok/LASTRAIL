using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Battle
{
    public sealed partial class ApprovedCombatView
    {
        // The approved console is authored on a 1004 × 565 canvas.
        const float DUnit = 1600f / 1004f;
        TMP_Text dShieldAmount;
        readonly List<RawImage> dRailLights = new List<RawImage>();
        TMP_Text[] dThrottleStops;
        Image[] dThrottleMarks;
        bool hasDConsole;

        static void DPosition(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        RectTransform DPlace(Transform target, float x, float y, float width, float height)
        {
            var r = (RectTransform)target;
            DPosition(r, x * DUnit, y * DUnit, width, height);
            r.localScale = Vector3.one * DUnit;
            return r;
        }

        RawImage DArt(string key, Transform parent, float x, float y, float w, float h)
        {
            var texture = LastRail.Resources.PreparedContent.Load<Texture2D>("CombatSecondFinal/DConsole/" + key);
            if (texture == null) throw new System.InvalidOperationException("Missing D console component: " + key);
            return Raw(key, parent, texture, x, y, w, h);
        }

        static void HideChild(Transform parent, params string[] names)
        {
            foreach (var name in names) parent.Find(name)?.gameObject.SetActive(false);
        }

        static void DText(TMP_Text label, float width, float height, int pixels)
        {
            label.fontSize = pixels;
            label.enableWordWrapping = false;
            label.rectTransform.sizeDelta = new Vector2(width, height);
        }

        void ApplyApprovedDConsole()
        {
            if (hasDConsole) return;
            hasDConsole = true;
            ApplyDTrain();
            ApplyDRail();
            ApplyDSupports();
            for (int i = 0; i < operatorCards.Count; i++) ApplyDCard(operatorCards[i], i);
        }

        void ApplyDTrain()
        {
            var plate = DPlace(root.Find("TrainPanel"), 7, 391, 268, 66);
            plate.GetComponent<Image>().color = Color.clear;
            DArt("train-face", plate, 0, 0, 268, 66);
            DArt("train-frame", plate, 0, 0, 268, 66);
            DArt("train-icon-v19", plate, 20, 8, 29, 25);
            DArt("system-icon-v19", plate, 19, 34, 31, 26);
            // One local coordinate space fixes both outside gauge edges at every aspect ratio.
            var gauges = Rect("DurabilityGaugeBounds", plate, 61, 10, 188, 47);
            var label = root.Find("TrainLabel").GetComponent<TMP_Text>();
            label.transform.SetParent(gauges, false);
            DPosition(label.rectTransform, 0, 0, 80, 12);
            DText(label, 80, 12, 10);
            label.text = "열차 HP";
            label.alignment = TextAlignmentOptions.Left;
            trainHp.transform.SetParent(gauges, false);
            DPosition(trainHp.rectTransform, 0, 14, 188, 8);
            trainHp.RoundedEnds = false;
            trainHp.HullNeon = false;
            trainHp.CyanBevel = true;
            trainHp.color = new Color(0, .94f, 1);
            trainText.transform.SetParent(gauges, false);
            DPosition(trainText.rectTransform, 80, 0, 108, 12);
            DText(trainText, 108, 12, 10);
            trainText.enableAutoSizing = false;
            trainText.alignment = TextAlignmentOptions.Right;
            var shieldLabel = Text("ShieldLabel", plate, "방어막", 62, -13, 62, 13, 9);
            shieldLabel.alignment = TextAlignmentOptions.Left;
            dShieldAmount = Text("ShieldAmount", plate, "", 135, -13, 104, 13, 9);
            dShieldAmount.alignment = TextAlignmentOptions.Right;
            // Role labels describe the approved display, not installed equipment or its condition.
            // No authoritative equipment selection/durability contract yet: never infer percentages.
            var labels = new[] { "화력", "요격", "회복" };
            for (int i = 0; i < 3; i++)
            {
                float x = i * 65;
                var symbol = Rect("SystemDurabilityIcon" + i, gauges, x + 1, 27, 10, 10).gameObject.AddComponent<CombatSystemSymbol>();
                symbol.Kind = (CombatSystemSymbol.Role)i;
                symbol.color = new Color(.55f, .66f, .68f);
                symbol.raycastTarget = false;
                Text("SystemDurabilityLabel" + i, gauges, labels[i], x + 13, 26, 19, 12, 8).alignment = TextAlignmentOptions.Left;
                var value = Text("SystemDurabilityValue" + i, gauges, "미연결", x + 32, 26, 26, 12, 7);
                value.alignment = TextAlignmentOptions.Right;
                value.color = new Color(.55f, .66f, .68f);
                var meter = Meter("SystemDurability" + i, gauges, x, 39, 58, 6, trainHp.color);
                meter.CyanBevel = true;
                meter.SetValue(0);
            }
        }
        void ApplyDRail()
        {
            var branch = DPlace(root.Find("BranchHousing"), 5, 454, 271, 94);
            branch.GetComponent<CombatConceptPanel>().enabled = false;
            HideChild(branch, "MetalPlate");
            DArt("rail-slim-fixed", branch, 1, 1, 269, 90);
            for (int i = 0; i < 3; i++)
            {
                var glow = DArt("rail-slim-glow-"+i, branch, 1, 1, 269, 90);
                dRailLights.Add(glow);
            }
            var routeTitle = root.Find("BranchTitle").GetComponent<TMP_Text>();
            DPlace(routeTitle.transform, 12, 457, 256, 14); DText(routeTitle, 256, 14, 10); routeTitle.text = "선로 선택";
            DPlace(branchLever.transform, 20, 474, 245, 70);
            SetDSlider(branchLever, 79.5f, 47.8f, 82, 0, "rail-slim-grip", 77, 30.4f);
            HideChild(root, "LeverHint");
            DPlace(routeDescription.transform, 30, 538, 220, 14); DText(routeDescription, 220, 14, 9);
            // The existing lever Commit callback remains the single route command.


            var throttle = DPlace(root.Find("ThrottleHousing"), 276, 390, 128, 166);
            throttle.GetComponent<CombatConceptPanel>().enabled = false;
            HideChild(throttle, "MetalPlate");
            DArt("throttle-frame", throttle, 0, 0, 128, 166);
            DArt("throttle-guide", throttle, 10, 29, 62, 126);
            DPlace(trainThrottle.transform, 290, 419, 47, 124);
            SetDSlider(trainThrottle, 2, 21, 42, 82, "throttle-grip", 44, 34);
            var speedTitle = root.Find("ThrottleTitle").GetComponent<TMP_Text>();
            DPlace(speedTitle.transform, 282, 395, 115, 18); DText(speedTitle, 115, 18, 11);
            for(int i=0;i<7;i++) HideChild(root, "ThrottleTick"+i);
            dThrottleStops = new TMP_Text[3];
            var stopNames = new[] {"고속", "순항", "저속"};
            dThrottleMarks = new Image[3];
            for(int i=0;i<3;i++){
                dThrottleStops[i]=Text("ThrottleStop"+i,throttle,stopNames[i],68,42+41*i,51,18,11);
                dThrottleMarks[i]=Box("ThrottleStopMark"+i,throttle,62,50+41*i,11,2,Color.gray);
            }
            DPlace(throttleDescription.transform, 281, 537, 116, 17); DText(throttleDescription,116,17,9);
        }

        void SetDSlider(Slider slider, float x, float y, float w, float h, string grip, float gripW, float gripH)
        {
            HideChild(slider.transform, "Track");
            if (!slider.TryGetComponent<Image>(out var hit)) hit=slider.gameObject.AddComponent<Image>();
            hit.color=Color.clear; hit.raycastTarget=true;
            DPosition((RectTransform)slider.handleRect.parent,x,y,w,h);
            slider.handleRect.sizeDelta=Vector2.zero;
            slider.handleRect.pivot=Vector2.one*.5f;
            HideChild(slider.handleRect, "MechanicalGrip");
            var art = DArt(grip,slider.handleRect,0,0,gripW,gripH);
            art.rectTransform.anchorMin=art.rectTransform.anchorMax=art.rectTransform.pivot=Vector2.one*.5f;
            art.rectTransform.anchoredPosition=Vector2.zero;
            slider.targetGraphic=art;
            // Slider updates handle anchors when the underlying value changes.
            if(slider.direction==Slider.Direction.BottomToTop){
                slider.handleRect.anchorMin=new Vector2(0,slider.normalizedValue);
                slider.handleRect.anchorMax=new Vector2(1,slider.normalizedValue);
            }else{
                slider.handleRect.anchorMin=new Vector2(slider.normalizedValue,0);
                slider.handleRect.anchorMax=new Vector2(slider.normalizedValue,1);
            }
        }

        static int DSupportSlot(string ability)
        {
            if (ability=="barrier") return 0;
            if (ability=="support_fire") return 1;
            return 2;
        }
        void ApplyDSupports()
        {
            var rack=Rect("DSupportRack",root,0,0,181,164);
            DPlace(rack,815,391,181,164);
            DArt("support-rack",rack,0,0,181,164);
            var commands=controls.Where(c=>c.Group=="manuals").ToArray();
            var rows=(JArray)driver.ReadDisplayState()["manuals"];
            if(commands.Length==0){
                var ids=new[]{"barrier","support_fire","emergency_repair"};
                var labels=new[]{"방어막","지원 사격","긴급 수리"};
                for(int i=0;i<3;i++){
                    var slot=Rect("UnavailableSupport"+i,rack,23,6+49*i,143,47);
                    DrawDSupport(slot,ids[i]);
                    Text("UnavailableLabel",slot,labels[i],56,5,80,22,12);
                    Text("Availability",slot,"미공급",56,24,80,16,9).color=new Color32(166,176,181,255);
                    slot.gameObject.AddComponent<CanvasGroup>().alpha=.72f;
                }
                HideChild(root,"SupportUnavailable");return;
            }
            rack.SetSiblingIndex(commands.Min(c=>c.Button.transform.GetSiblingIndex()));
            foreach(var c in commands){
                string id=(string)rows[c.Index]["ability"];
                var b=DPlace(c.Button.transform,838,397+49*DSupportSlot(id),143,47);
                foreach(Transform child in b)child.gameObject.SetActive(false);
                DrawDSupport(b,id);
                var well=b.Find("PressWell").GetComponent<Image>();
                well.gameObject.SetActive(true);DPosition(well.rectTransform,7,5,129,37);
                c.Text.gameObject.SetActive(true);c.Text.transform.SetAsLastSibling();
                DPosition(c.Text.rectTransform,52,3,83,39);DText(c.Text,83,39,12);
                c.Wipe.gameObject.SetActive(true);DPosition(c.Wipe.rectTransform,9,6,41,34);c.Wipe.transform.SetAsLastSibling();
                c.CooldownText.gameObject.SetActive(true);DPosition(c.CooldownText.rectTransform,9,7,41,31);c.CooldownText.fontSize=12;c.CooldownText.transform.SetAsLastSibling();
                c.Button.GetComponent<CombatCommandPress>().Configure(well,()=>presentationClock());
            }
        }
        void DrawDSupport(RectTransform cell,string ability)
        {
            foreach(var role in new[]{"face","frame","icon"}) DArt(ability+"-"+role,cell,0,0,143,47);
        }
        static string DRole(string id)
        {
            switch(id){
                case "op_reina": case "op_eve": case "op_mira": return "attack";
                case "op_kyle": return "defense";
                case "op_sera": return "buff_heal";
                case "op_nyx": return "debuff";
                default: return null;
            }
        }
        static Color DRoleColor(string role)
        {
            switch(role){
                case "attack": return new Color32(255,96,100,255);
                case "defense": return new Color32(77,190,255,255);
                case "buff_heal": return new Color32(98,219,172,255);
                case "debuff": return new Color32(189,138,255,255);
                default: return new Color32(185,193,198,255);
            }
        }

        void ApplyDCard(OperatorCard c, int index)
        {
            var actor=((JArray)driver.ReadDisplayState()["actors"]).Single(a=>(string)a["id"]==c.Actor);
            string visual=(string)actor["visual"];
            string role=DRole(visual);
            var card=DPlace(c.Button.transform.parent,402+77*index,405,77,138);
            DPosition((RectTransform)card.Find("CardPlate"),3,1,71,133);
            DPosition(c.Portrait.rectTransform,5,4,68,72);
            if(visual=="op_reina" || visual=="op_kyle" || visual=="op_eve" || visual=="op_sera" || visual=="op_nyx"){
                c.Portrait.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("CombatSecondFinal/DConsole/portrait-"+visual.Substring(3));
                c.Portrait.uvRect=new Rect(0,0,1,1);
                c.Portrait.name="ApprovedBattleCardPortrait_"+visual;
                c.Portrait.enabled=true;
            }
            var back=card.Find("OperatorNameplate");
            DPosition((RectTransform)back,6,72,65,16);
            var name=card.Find("OperatorName").GetComponent<TMP_Text>();
            DPosition(name.rectTransform,6,72,65,16);DText(name,65,16,11);
            DPosition((RectTransform)card.Find("SelectOperator_"+c.Actor),0,0,77,99);
            DPosition(c.Selection.rectTransform,5,4,66,12);DText(c.Selection,66,12,8);
            DPosition(c.Hp.rectTransform,20,90,49,9);
            Text("HpLabel",card,"HP",5,89,16,11,8);
            HideChild(card,"ChargeNumberBackplate");
            c.Charge.enabled=false;
            DPosition(c.Percent.rectTransform,11,119,53,11);DText(c.Percent,53,11,8);
            c.Percent.alignment=TextAlignmentOptions.Center;
            var ultimate=(RectTransform)c.Button.transform;
            DPosition(ultimate,3,100,71,33);
            HideChild(ultimate,"UltimateSocket","UltimateIcon","UltimateIllustration");
            if(role!=null)DArt("role-"+role,ultimate,0,4,24,24);
            else {c.Icon.gameObject.SetActive(true);DPosition(c.Icon.rectTransform,3,5,19,19);}
            DArt("charge-track",ultimate,22,8,45,16);
            c.DCharge=DArt("charge-fill",ultimate,22,8,45,16);
            c.DTint=DRoleColor(role);
            c.DCharge.color=c.DTint;
            c.DWidth=45;
            // A legacy manual skill keeps a separate hit target and label.
            if(legacyGeneralButtons.TryGetValue(c.Actor,out var general)){
                DPlace(general.transform,406+77*index,531,69,11);
                var text=general.GetComponentInChildren<TMP_Text>();DPosition(text.rectTransform,0,0,69,11);DText(text,69,11,8);
                DPosition(ultimate,3,100,71,25);
            }
            var frame=(RectTransform)card.Find("CardFrame");
            DPosition(frame,3,-1,73,137);frame.SetAsLastSibling();
            if(c.ReadyWash==null)c.ReadyWash=Box("ReadyWash",card,5,4,67,126,Color.clear);
            DPosition(c.ReadyWash.rectTransform,5,4,67,126);
            c.ReadyWash.transform.SetAsLastSibling();
            c.ReadyWash.enabled=false;
            c.Rim.transform.SetParent(card,false);DPosition(c.Rim.rectTransform,3,0,71,135);c.Rim.transform.SetAsLastSibling();
            c.DWash=DArt("ready-wash",card,-1,-1,80,140);
            c.DFlash=DArt("ready-flash",card,6,-7,66,20);
            c.DSelected=DArt("selected-corners",card,-4,-6,86,148);
            c.Selection.enabled=false;
        }

        void RefreshDCardLight(OperatorCard card, int? charge, bool down, bool selected)
        {
            const float riseSeconds=.20f, flashSeconds=.15f;
            double now=presentationClock();
            // An already-ready card opens at steady light. Only a new observed ready transition flashes.
            bool ready=!down && charge==100;
            if(ready && !card.DReady) card.DReadyAt=now-(card.DReadySeen?0:riseSeconds+flashSeconds);
            card.DReadySeen=true; card.DReady=ready;
            card.DSelected.enabled=selected;RenderDCardLight(card);
        }

        void RenderDCardLight(OperatorCard card)
        {
            const float riseSeconds=.20f, flashSeconds=.15f;
            bool ready=card.DReady;
            float age=(float)(presentationClock()-card.DReadyAt);
            float rise=ready?Mathf.Clamp01(age/riseSeconds):0;
            float flash=ready && age>=riseSeconds?Mathf.Clamp01(1-(age-riseSeconds)/flashSeconds):0;
            var wash=card.DTint; wash.a=rise*(.82f+.18f*flash);
            card.DWash.color=wash;
            var sparkle=Color.Lerp(card.DTint,Color.white,.5f); sparkle.a=flash;
            card.DFlash.color=sparkle;
            card.Rim.SetLight(rise,1); card.Rim.color=card.DTint;
        }

        // Without a supplied reading, park the display at center without issuing a slider command.
        static void PositionDGrip(Slider slider,bool supplied)
        {
            float position=supplied?slider.normalizedValue:.5f;
            bool vertical=slider.direction==Slider.Direction.BottomToTop;
            slider.handleRect.anchorMin=vertical?new Vector2(0,position):new Vector2(position,0);
            slider.handleRect.anchorMax=vertical?new Vector2(1,position):new Vector2(position,1);
            slider.targetGraphic.color=supplied?Color.white:new Color(.65f,.65f,.65f,.62f);
        }

        void RefreshApprovedDConsole(JObject state)
        {
            if(!hasDConsole)return;
            var train=((JArray)state["actors"]).FirstOrDefault(a=>(bool)a["train"]);
            int shield=(int?)train?["shield"]??0;
            int? shieldMaximum=(int?)train?["shieldMaximum"];
            bool hasCapacity=shieldMaximum.GetValueOrDefault()>0;
            dShieldAmount.text=hasCapacity?shield+" / "+shieldMaximum:shield.ToString();
            var rail=(state["railJourney"]??state["ordinaryRail"]) as JObject;
            var offer=rail?["offer"] as JObject;
            int active=offer==null?-1:(branchLever.normalizedValue<.25f?0:branchLever.normalizedValue>.75f?2:1);
            for(int i=0;i<dRailLights.Count;i++)dRailLights[i].enabled=(i==active);
            int throttle=(int?)rail?["throttleBps"]??0;
            int stop=rail==null?-1:throttle>10000?0:throttle<10000?2:1;
            for(int i=0;i<dThrottleStops.Length;i++){
                var tint=i==stop?new Color32(135,237,255,255):new Color32(164,171,174,255);
                dThrottleStops[i].color=tint;dThrottleMarks[i].color=tint;
            }
            PositionDGrip(branchLever,offer!=null);
            PositionDGrip(trainThrottle,rail!=null);
            if(rail==null){
                throttleDescription.text="속도 정보 없음";
                routeDescription.text="선로 정보 없음";
            }
        }
    }
}
