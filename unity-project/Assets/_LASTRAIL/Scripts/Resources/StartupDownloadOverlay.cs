using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Resources
{
    public sealed class StartupDownloadOverlay : MonoBehaviour
    {
        private GameObject modal;
        private ResourcePreparationView preparation;
        public Action PauseRequested;
        private TMP_Text message;
        private TMP_Text amountUnit;
        private GameObject retry;
        private RectTransform safe;
        public bool ConsentVisible => modal != null && modal.activeSelf;

        public void Initialize(TMP_FontAsset font, Action approve, Action decline, Action retryAction)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 80;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1;
            gameObject.AddComponent<GraphicRaycaster>();
            safe = Rect("DownloadSafeArea", transform, Vector2.zero, Vector2.zero);
            preparation = ResourcePreparationView.Create(safe, font, () => PauseRequested?.Invoke());
            retry = MakeButton(safe, "RetryContent", "\uB2E4\uC6B4\uB85C\uB4DC \uB2E4\uC2DC \uC2DC\uB3C4", Vector2.zero, new Vector2(850, 144), font, retryAction).gameObject;
            var retryRect = retry.GetComponent<RectTransform>();
            retryRect.anchorMin = retryRect.anchorMax = new Vector2(0.5f, 0);
            retryRect.anchoredPosition = new Vector2(0, 100);
            modal = Rect("DownloadConsentModal", safe, Vector2.zero, Vector2.zero).gameObject;
            modal.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);
            var panel = Rect("ConsentPanel", modal.transform, Vector2.zero, new Vector2(1660, 800));
            var frame=LastRail.Auth.IndependentMetalSurface.Create(panel, false, Color.white);frame.Frame.BorderScale=.9f;frame.Frame.color=new Color(.46f,.49f,.51f);frame.Backplate.Inset=new Vector2(48,48);frame.AlignPopupLights(true);
            var eyebrow = Text(panel, "ConsentEyebrow", "RESOURCE DOWNLOAD", new Vector2(-380, 300), new Vector2(780, 50), font); eyebrow.fontSize = 26; eyebrow.alignment = TextAlignmentOptions.Left; eyebrow.color = new Color(.4f, .85f, 1);
            var accent=Rect("ConsentHeadingAccent",panel,new Vector2(-746,262),new Vector2(48,3)).gameObject.AddComponent<Image>();accent.color=new Color(.15f,.9f,1);accent.raycastTarget=false;
            var heading = Text(panel, "ConsentTitle", "리소스를 다운로드할까요?", new Vector2(-380, 210), new Vector2(780, 125), font); heading.fontSize = 52; heading.fontStyle = FontStyles.Bold; heading.alignment = TextAlignmentOptions.Left;
            var network=Text(panel, "ConsentNetwork", "현재 모바일 데이터 네트워크에 연결되어 있습니다.", new Vector2(-380, 80), new Vector2(780, 90), font);network.fontSize=30;network.alignment=TextAlignmentOptions.Left;
            var amount = Rect("ConsentAmountPanel", panel, new Vector2(400, 150), new Vector2(780, 180));
            var amountSurface = LastRail.Auth.IndependentMetalSurface.Create(amount, true, new Color(.13f, .45f, .55f)); amountSurface.Lights.gameObject.SetActive(false);
            var amountLabel=Text(amount, "ConsentAmountLabel", "남은 다운로드", new Vector2(0, 48), new Vector2(690, 44), font);amountLabel.fontSize=26;amountLabel.alignment=TextAlignmentOptions.Left;
            message = Text(amount, "ConsentMessage", "", new Vector2(0, -24), new Vector2(690, 95), font);message.fontSize=84;message.alignment=TextAlignmentOptions.TopLeft;
            amountUnit=Text(amount,"ConsentUnit","MiB",Vector2.zero,new Vector2(140,95),font);amountUnit.fontSize=42;amountUnit.alignment=TextAlignmentOptions.TopLeft;
            var warning=Text(panel, "ConsentWarning", "이동통신 요금제에 따라 데이터 요금이 발생할 수 있습니다.", new Vector2(-380, -70), new Vector2(780, 110), font);warning.fontSize=30;warning.alignment=TextAlignmentOptions.Left;
            MakeButton(panel, "ApproveDownload", "동의하고 다운로드", new Vector2(-400, -270), new Vector2(780, 144), font, approve);
            var defer = MakeButton(panel, "DeferDownload", "Wi-Fi 연결 대기", new Vector2(400, -270), new Vector2(780, 144), font, decline);
            var deferSurface = defer.GetComponentInChildren<LastRail.Auth.IndependentMetalSurface>(); deferSurface.Backplate.color = new Color(.65f, .67f, .68f); deferSurface.SetLightColor(new Color(1, .61f, .18f));
            Show(false, 0, false);
        }

        private void LateUpdate()
        {
            if (safe == null || Screen.width <= 0 || Screen.height <= 0) return;
            var area = Screen.safeArea;
            safe.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            LayoutConsent();
        }

        private void LayoutConsent()
        {
            if (modal == null || safe == null) return;
            var panel = modal.transform.Find("ConsentPanel") as RectTransform;
            float width = Mathf.Max(800, safe.rect.width - 80);
            panel.sizeDelta = new Vector2(width, 800);
            float column = (width - 160) / 2;
            void PlaceChild(string name, float x, float y, float height)
            {
                var child = panel.Find(name) as RectTransform;
                child.anchoredPosition = new Vector2(x, y);
                child.sizeDelta = new Vector2(column, height);
            }
            var title = panel.Find("ConsentTitle").GetComponent<TMP_Text>();
            title.enableAutoSizing = true; title.fontSizeMin = 36; title.fontSizeMax = 52;
            var accent = (RectTransform)panel.Find("ConsentHeadingAccent");
            accent.anchoredPosition = new Vector2(-width / 2 + 60, 262);
            float center = (column + 40) / 2;
            PlaceChild("ConsentEyebrow", -center, 300, 50);
            PlaceChild("ConsentTitle", -center, 210, 125);
            PlaceChild("ConsentNetwork", -center, 60, 120);
            PlaceChild("ConsentWarning", -center, -100, 150);
            PlaceChild("ConsentAmountPanel", center, 150, 180);
            PlaceChild("ApproveDownload", -center, -270, 144);
            PlaceChild("DeferDownload", center, -270, 144);
            foreach (string name in new[] { "ApproveDownload", "DeferDownload" })
            {
                var label = panel.Find(name + "/Label") as RectTransform;
                label.sizeDelta = new Vector2(column - 32, 132);
            }
            var amountLabel = panel.Find("ConsentAmountPanel/ConsentAmountLabel") as RectTransform;
            amountLabel.sizeDelta = new Vector2(column - 90, 44);
            LayoutAmount();
        }

        public void Show(bool consent, long bytes, bool canRetry)
        {
            if (safe == null) return;
            var area = Screen.safeArea;
            safe.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            preparation.gameObject.SetActive(false);
            modal.SetActive(consent); retry.SetActive(canRetry && !consent);
            preparation.SetConsentVisible(consent);
            message.text = (bytes / 1048576d).ToString("0.0");
            LayoutConsent();
        }

        void LayoutAmount()
        {
            message.fontSize=84;amountUnit.fontSize=42;
            float numberWidth=message.GetPreferredValues(message.text).x,unitWidth=amountUnit.GetPreferredValues("MiB").x;
            float scale=Mathf.Min(1,Mathf.Max(100,((RectTransform)message.transform.parent).rect.width-103)/Mathf.Max(1,numberWidth+unitWidth));message.fontSize*=scale;amountUnit.fontSize*=scale;
            numberWidth=message.GetPreferredValues(message.text).x;unitWidth=amountUnit.GetPreferredValues("MiB").x;
            // TMP includes the font's full line metrics, beyond the visible digit ink.
            float numberHeight=Mathf.Max(95,message.GetPreferredValues(message.text).y+1);
            LastRail.Auth.AuthFormVisuals.Place(message.rectTransform,45,66.5f,numberWidth+1,numberHeight);
            LastRail.Auth.AuthFormVisuals.Place(amountUnit.rectTransform,45+numberWidth+12,66.5f,unitWidth+1,Mathf.Max(95,amountUnit.GetPreferredValues("MiB").y+1));
            message.ForceMeshUpdate(true);amountUnit.ForceMeshUpdate(true);
            if(message.textInfo.lineCount>0&&amountUnit.textInfo.lineCount>0)
                amountUnit.rectTransform.anchoredPosition+=Vector2.up*(message.textInfo.lineInfo[0].baseline-amountUnit.textInfo.lineInfo[0].baseline);
        }

        public void BeginAuthenticatedPlan(DownloadCoordinator coordinator,DownloadAccessPolicy policy)
        {preparation.BeginAuthenticatedPlan(coordinator.TotalBytes,policy.RemainingBytes);}

        public void RenderPreparation(bool visible, DownloadCoordinator coordinator, DownloadAccessPolicy policy, bool entryFailed=false)
        {
            preparation.gameObject.SetActive(visible);
            if(visible) {preparation.Render(coordinator.Stage,coordinator.ReceivedBytes,coordinator.TotalBytes,policy); if(entryFailed) preparation.ShowEntryFailure();}
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = size == Vector2.zero ? Vector2.zero : new Vector2(0.5f, 0.5f);
            rect.anchorMax = size == Vector2.zero ? Vector2.one : new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size; rect.anchoredPosition = position;
            return rect;
        }

        private static TMP_Text Text(Transform parent, string name, string label, Vector2 position, Vector2 size, TMP_FontAsset font)
        {
            var text = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = label; text.fontSize = 38; text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; text.richText = false;
            return text;
        }

        private static Button MakeButton(Transform parent, string name, string label, Vector2 position, Vector2 size, TMP_FontAsset font, Action clicked)
        {
            var rect = Rect(name, parent, position, size);
            var image = rect.gameObject.AddComponent<Image>(); image.color = Color.clear;
            var surface = LastRail.Auth.IndependentMetalSurface.Create(rect, true, new Color(.13f, .68f, .87f));
            surface.Frame.color=new Color(.66f,.69f,.70f);
            if (name != "DeferDownload") surface.EmphasizePrimary(1.25f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = surface.Backplate;
            Text(rect, "Label", label, Vector2.zero, size - new Vector2(32, 12), font);
            button.onClick.AddListener(() => clicked()); return button;
        }
    }
}
