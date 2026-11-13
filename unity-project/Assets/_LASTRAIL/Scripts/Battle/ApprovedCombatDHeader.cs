using LastRail.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Battle {
public sealed partial class ApprovedCombatView {
    // Direction authored the HUD in a 1004 x 565 reference coordinate system.
    const float HeaderScale = 1600f / 1004f;

    void BuildApprovedDHeader(string operationId) {
        foreach (string name in new[] { "Header", "HeaderUnderlight", "TitleHousing", "TitleEmblem", "WaveHousing" }) {
            var previous = root.Find(name);
            if (previous != null) previous.gameObject.SetActive(false);
        }

        var title = root.Find("Title").GetComponent<TMP_Text>();
        HeaderLayer(root, "title-face", 0, 2, 260, 53);
        HeaderLayer(root, "title-rim", 0, 2, 260, 53);
        HeaderLayer(root, "title-emblem", 21, 10, 40, 40);
        title.color = new Color32(243, 237, 223, 255);
        title.text = UnityDisplayLabels.ForActOneStoryProgress(operationId, "작전");
        HeaderLabel(title, 68, 7, 172, 25, 19, TextAlignmentOptions.MidlineLeft);
        HeaderLabel(wave, 69, 32, 162, 12, 10, TextAlignmentOptions.MidlineLeft);
        HeaderPlace(waveTrack.rectTransform, 69, 45, 122, 2);
        waveTrack.transform.SetAsLastSibling();

        HeaderButton(speed, 856, 10, 81, 31, "rate");
        var speedLabel = speed.transform.Find("Text").GetComponent<TMP_Text>();
        HeaderLabel(speedLabel, 8, 0, 46, 31, 16, TextAlignmentOptions.Center);
        speed.transform.Find("SpeedChevrons")?.gameObject.SetActive(false);
        speed.transform.Find("SpeedGlyph")?.gameObject.SetActive(false);
        HeaderLayer(speed.transform, "rate-arrow", 49, 7, 16, 16);

        HeaderButton(pause, 942, 7, 49, 37, "pause");
        var idleBars = HeaderLayer(pause.transform, "playback-bars", 16, 8, 17, 20);
        dPauseBars = idleBars.gameObject;
        HeaderPlace(pauseGlyph.rectTransform, 16, 8, 17, 20);
        pauseGlyph.transform.SetAsLastSibling();
    }

    GameObject dPauseBars;

    void HeaderButton(Button button, float x, float y, float w, float h, string prefix) {
        HeaderPlace((RectTransform)button.transform, x,y,w,h);
        button.transform.SetAsLastSibling();
        button.transform.Find("ControlHousing")?.gameObject.SetActive(false);
        HeaderLayer(button.transform, prefix+"-face", 0,0,w,h);
        HeaderLayer(button.transform, prefix+"-rim", 0,0,w,h);
    }

    RawImage HeaderLayer(Transform parent, string id, float x, float y, float w, float h) {
        var texture = LastRail.Resources.PreparedContent.Load<Texture2D>("CombatSecondFinal/DHeader/"+id);
        if (texture == null) throw new System.InvalidOperationException("Missing approved D header layer: "+id);
        return Raw(id, parent, texture, x*HeaderScale,y*HeaderScale,w*HeaderScale,h*HeaderScale);
    }

    static void HeaderPlace(RectTransform rect, float x, float y, float w, float h) {
        rect.anchoredPosition = new Vector2(x,-y)*HeaderScale;
        rect.sizeDelta = new Vector2(w,h)*HeaderScale;
    }

    static void HeaderLabel(TMP_Text label, float x, float y, float w, float h, float size, TextAlignmentOptions alignment) {
        HeaderPlace(label.rectTransform, x,y,w,h);
        label.fontSize = size*HeaderScale;
        label.alignment = alignment;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.transform.SetAsLastSibling();
    }
}
}
