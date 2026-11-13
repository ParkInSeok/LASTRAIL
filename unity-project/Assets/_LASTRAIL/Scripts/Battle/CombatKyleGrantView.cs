using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Battle
{
    public sealed partial class ApprovedCombatView
    {
        sealed class KyleGrant { public CombatKyleGrantGraphic Graphic; public string Target; public float Start = float.NaN; }
        readonly List<KyleGrant> kyleGrants = new List<KyleGrant>();

        bool KyleBody(JObject state, string target, out Rect bounds)
        {
            bounds = default;
            if (!ActorPoint(state, target, out _) || !units.TryGetValue(target, out var unit) || unit.Enemy) return false;
            var transforms = captureRigs.TryGetValue(unit.Visual, out var rig)
                ? rig.GetComponentsInChildren<Graphic>().Select(g => g.rectTransform).ToArray()
                : new[] { unit.Art.rectTransform };
            var corners = new Vector3[4]; var min = new Vector2(float.MaxValue, float.MaxValue); var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var transform in transforms)
            {
                transform.GetWorldCorners(corners);
                foreach (var corner in corners) { Vector2 point = root.InverseTransformPoint(corner); min = Vector2.Min(min, point); max = Vector2.Max(max, point); }
            }
            if (transforms.Length == 0 || max.x <= min.x || max.y <= min.y) return false;
            bounds = UnityEngine.Rect.MinMaxRect(min.x, min.y, max.x, max.y); return true;
        }

        void AddKyleGrant(JObject state, JObject row)
        {
            string ability = (string)row["abilityId"], target = (string)row["targetId"];
            if ((ability != "op_kyle.active" && ability != "op_kyle.ultimate") || (string)row["kind"] != "SHIELD_ISSUE"
                || !CombatEffectFeedback.Positive(row) || presentation.Mode == CombatPresentationMode.Simplified
                || ImminentEnemyWarning(state) || !KyleBody(state, target, out _)) return;
            var node = Rect("KyleGrant_" + ability + "_" + target, root, 0, 0, 1, 1);
            node.SetSiblingIndex(actorLayer.GetSiblingIndex() + 1);
            var graphic = node.gameObject.AddComponent<CombatKyleGrantGraphic>();
            graphic.Ultimate = ability == "op_kyle.ultimate"; graphic.raycastTarget = false;
            kyleGrants.Add(new KyleGrant { Graphic = graphic, Target = target });
        }

        void UpdateKyleGrants(JObject state)
        {
            foreach (var cue in kyleGrants.ToArray())
            {
                if (float.IsNaN(cue.Start)) cue.Start = resultClock;
                float age = (resultClock - cue.Start) / .58f;
                if (age >= 1 || presentation.Mode == CombatPresentationMode.Simplified || ImminentEnemyWarning(state) || !KyleBody(state, cue.Target, out var bounds))
                { DisposeVisual(cue.Graphic.gameObject); kyleGrants.Remove(cue); continue; }
                var transform = cue.Graphic.rectTransform;
                transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(.5f, .5f);
                transform.localPosition = bounds.center;
                transform.sizeDelta = bounds.size * new Vector2(cue.Graphic.Ultimate ? 1.35f : 1.25f, cue.Graphic.Ultimate ? 1.15f : 1.05f);
                cue.Graphic.SetProgress(age);
            }
        }
    }
}
