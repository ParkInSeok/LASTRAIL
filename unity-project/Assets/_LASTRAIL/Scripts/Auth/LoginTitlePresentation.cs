using System.Collections;
using TMPro;
using UnityEngine;

namespace LastRail.Auth
{
    // Same approved lettering as the opening; owns only this label's material.
    public sealed class LoginTitlePresentation : MonoBehaviour
    {
        TMP_Text title;
        Material metal;
        float elapsed;
        Coroutine sheenRoutine;
        const float SheenDelay = 1.6f;
        const float SheenDuration = .85f;
        void Awake()
        {
            title = GetComponent<TMP_Text>();
            title.color = Color.white;
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 9; // 108px login title / 72px opening title.
            title.horizontalMapping = TextureMappingOptions.Line;
            title.verticalMapping = TextureMappingOptions.Character;
            var template = UnityEngine.Resources.Load<Material>("StartupVideo/TitleMetal");
            if (template == null) return;
            metal = new Material(title.fontSharedMaterial) { name = "LoginTitleMetalInstance", shader = template.shader };
                metal.EnableKeyword("BEVEL_ON"); metal.EnableKeyword("UNDERLAY_ON");
                metal.SetColor("_FaceColor", Color.white); metal.SetFloat("_FaceDilate", .07f);
                metal.SetColor("_OutlineColor", new Color(.53f,.30f,.09f)); metal.SetFloat("_OutlineWidth", .18f);
                metal.SetFloat("_Bevel", .65f); metal.SetFloat("_BevelWidth", .3f); metal.SetFloat("_BevelRoundness", .2f);
                metal.SetFloat("_LightAngle", .65f); metal.SetFloat("_SpecularPower", 1.2f);
                metal.SetFloat("_Diffuse", .25f); metal.SetFloat("_Ambient", .7f);
                metal.SetColor("_UnderlayColor", new Color(.015f,.025f,.03f,.95f));
                metal.SetFloat("_UnderlayOffsetX", .5f); metal.SetFloat("_UnderlayOffsetY", -.7f);
                metal.SetFloat("_UnderlayDilate", .3f); metal.SetFloat("_UnderlaySoftness", .06f);
                metal.SetFloat("_TitleSheen", -1);
            title.fontMaterial = metal;
        }
        void OnEnable()
        {
            StopSheen();
            elapsed = 0;
            if (metal == null) return;
            metal.SetFloat("_TitleSheen", -1);
            if (Application.isPlaying) sheenRoutine = StartCoroutine(PlaySheen());
        }
        IEnumerator PlaySheen()
        {
            // Preserve the capture clock used by the sunlight reveal.
            while (elapsed < SheenDelay + SheenDuration)
            {
                yield return null;
                elapsed += Time.captureFramerate > 0 ? Time.captureDeltaTime : Time.unscaledDeltaTime;
                metal.SetFloat("_TitleSheen", Mathf.Lerp(-.2f, 1.3f, Mathf.Clamp01((elapsed - SheenDelay) / SheenDuration)));
            }
            sheenRoutine = null;
        }
        void StopSheen()
        {
            if (sheenRoutine == null) return;
            StopCoroutine(sheenRoutine);
            sheenRoutine = null;
        }
        void OnDisable() { StopSheen(); }
        void OnDestroy()
        {
            StopSheen();
            if (metal != null) { if (Application.isPlaying) Destroy(metal); else DestroyImmediate(metal); }
        }
    }
}
