using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Globalization;
using LastRail.Submissions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Support
{
    public sealed class SupportInquiryPanel : MonoBehaviour
    {
        private SupportInquiryController model;
        private TMP_Dropdown category;
        private TMP_InputField content;
        private TMP_Text status;
        private TMP_Text sendLabel;
        private Button send;
        private Button reconcile;
        private RectTransform safe;
        private RectTransform form;
        private RectTransform recoveryForm;
        private int shownWaitSeconds = -1;
        private CancellationTokenSource presentationDelay;
        private bool applyingLayout;
        private SupportReceiptRecovery recovery;
        private TMP_Dropdown pendingReceipts;
        private TMP_Text recoveryStatus, forgetWarning;
        private Button refreshReceipts, independentInquiry, forgetReceipt, confirmForget, cancelForget;
        private PendingReceipt[] displayedRecords = Array.Empty<PendingReceipt>();
        private PendingReceipt deleteTarget;
        private bool recoveryActionFailed;
        public event Action Closed;

        // The authenticated owner retains the controller across close/reopen and supplies its font.
        public void Initialize(SupportInquiryController controller, TMP_FontAsset font,
            Func<string, string> categoryLabel)
        {
            if (model != null) throw new InvalidOperationException("Panel already initialized.");
            if (font == null) throw new ArgumentNullException(nameof(font));
            if (categoryLabel == null) throw new ArgumentNullException(nameof(categoryLabel));
            model = controller ?? throw new ArgumentNullException(nameof(controller));
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 70;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1;
            gameObject.AddComponent<GraphicRaycaster>();
            var background = Rect("Background", transform);
            background.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.07f, 0.08f);
            safe = Rect("SafeArea", transform);
            var scrollRoot = Rect("InquiryScroll", safe);
            scrollRoot.anchorMax = new Vector2(.55f, 1);
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            var viewport = Rect("Viewport", scrollRoot);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.001f);
            form = Rect("Form", viewport);
            form.anchorMin = new Vector2(0, 1); form.anchorMax = Vector2.one; form.pivot = new Vector2(0.5f, 1);
            scroll.viewport = viewport; scroll.content = form; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var layout = form.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 32, 32); layout.spacing = 20;
            layout.childControlHeight = true; layout.childControlWidth = true;
            layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
            Text(form, "Title", "\uACE0\uAC1D\uBB38\uC758", font, 80, 48);
            Text(form, "CategoryLabel", "\uCE74\uD14C\uACE0\uB9AC", font, 55);
            category = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources()).GetComponent<TMP_Dropdown>();
            category.name = "Category"; category.transform.SetParent(form, false); Size(category.gameObject, 144);
            category.captionText.font = font; category.itemText.font = font;
            category.ClearOptions(); category.AddOptions(model.Categories.Select(categoryLabel).ToList());
            var arrow = category.transform.Find("Arrow"); arrow.GetComponent<Image>().enabled = false;
            var arrowText = Rect("Glyph", arrow).gameObject.AddComponent<TextMeshProUGUI>();
            arrowText.font = font; arrowText.text = "v"; arrowText.color = Color.black;
            arrowText.alignment = TextAlignmentOptions.Center; arrowText.raycastTarget = false;
            arrow.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 70);
            arrow.GetComponent<RectTransform>().anchoredPosition = new Vector2(-40, 0);
            category.captionText.rectTransform.offsetMax = new Vector2(-80, -7);
            category.template.sizeDelta = new Vector2(category.template.sizeDelta.x, 500);
            var item = category.itemText.GetComponentInParent<Toggle>(true).GetComponent<RectTransform>();
            item.sizeDelta = new Vector2(item.sizeDelta.x, 110);
            var templateContent = (RectTransform)item.parent;
            templateContent.sizeDelta = new Vector2(templateContent.sizeDelta.x, 118);
            Text(form, "ContentLabel", "\uBB38\uC758 \uB0B4\uC6A9", font, 55);
            content = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources()).GetComponent<TMP_InputField>();
            content.name = "Content"; content.transform.SetParent(form, false);
            var inputSize = Size(content.gameObject, 400); inputSize.flexibleHeight = 1;
            content.lineType = TMP_InputField.LineType.MultiLineNewline; content.characterLimit = model.MaxContentLength;
            content.richText = false;
            ((TMP_Text)content.placeholder).text = "\uBB38\uC758 \uB0B4\uC6A9";
            Text(form, "Privacy", "\uAC1C\uC778\uC815\uBCF4\u00B7\uBE44\uBC00\uBC88\uD638\u00B7\uC778\uC99D\uCF54\uB4DC\uB294 \uC785\uB825\uD558\uC9C0 \uB9C8\uC138\uC694.", font, 110);
            status = Text(form, "Status", "", font, 140);
            send = MakeButton(form, "Send", "\uBCF4\uB0B4\uAE30", font, Submit);
            sendLabel = send.GetComponentInChildren<TMP_Text>();
            reconcile = MakeButton(form, "Reconcile", "\uC811\uC218 \uC5EC\uBD80 \uD655\uC778", font, Reconcile);
            MakeButton(form, "Close", "\uB2EB\uAE30", font, Hide);
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = font; text.richText = false;
                if (text.name != "Title") text.fontSize = 38;
            }
            content.textComponent.alignment = TextAlignmentOptions.TopLeft;
            category.onValueChanged.AddListener(_ => Edit());
            content.onValueChanged.AddListener(_ => Edit());
            if (isActiveAndEnabled) model.Changed += Render;
            Render();
        }

        public void ConfigureRecovery(SupportReceiptRecovery recovery)
        {
            if (model == null) throw new InvalidOperationException("Initialize the panel first.");
            if (recovery == null) throw new ArgumentNullException(nameof(recovery));
            if (this.recovery != null) this.recovery.Changed -= Render;
            this.recovery = recovery;
            deleteTarget = null; recoveryActionFailed = false;
            if (pendingReceipts != null)
            {
                if (isActiveAndEnabled) recovery.Changed += Render;
                Render(); return;
            }
            var recoveryRoot = Rect("RecoveryScroll", safe);
            recoveryRoot.anchorMin = new Vector2(.55f, 0);
            var recoveryScroll = recoveryRoot.gameObject.AddComponent<ScrollRect>();
            var recoveryViewport = Rect("RecoveryViewport", recoveryRoot);
            recoveryViewport.gameObject.AddComponent<RectMask2D>();
            recoveryViewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .001f);
            recoveryForm = Rect("RecoveryForm", recoveryViewport);
            recoveryForm.anchorMin = new Vector2(0, 1); recoveryForm.pivot = new Vector2(.5f, 1);
            var recoveryLayout = recoveryForm.gameObject.AddComponent<VerticalLayoutGroup>();
            recoveryLayout.padding = new RectOffset(32, 32, 32, 32); recoveryLayout.spacing = 20;
            recoveryLayout.childControlHeight = recoveryLayout.childControlWidth = true;
            recoveryLayout.childForceExpandHeight = false;
            recoveryScroll.viewport = recoveryViewport; recoveryScroll.content = recoveryForm;
            recoveryScroll.horizontal = false; recoveryScroll.movementType = ScrollRect.MovementType.Clamped;
            var font = category.captionText.font;
            var label = Text(recoveryForm, "PendingReceiptsLabel", "\uBCF4\uB958 \uAE30\uB85D", font, 55);
            pendingReceipts = Instantiate(category.gameObject, recoveryForm, false).GetComponent<TMP_Dropdown>();
            pendingReceipts.name = "PendingReceipts";
            pendingReceipts.onValueChanged = new TMP_Dropdown.DropdownEvent();
            pendingReceipts.onValueChanged.AddListener(SelectReceipt);
            foreach (var text in pendingReceipts.GetComponentsInChildren<TMP_Text>(true))
            { text.richText = false; text.enableAutoSizing = true; text.fontSizeMin = 26; text.fontSizeMax = 38; }
            recoveryStatus = Text(recoveryForm, "RecoveryStatus", "", font, 180);
            recoveryStatus.enableAutoSizing = true; recoveryStatus.fontSizeMin = 28; recoveryStatus.fontSizeMax = 38;
            refreshReceipts = MakeButton(recoveryForm, "RefreshReceipts", "\uBCF4\uB958 \uAE30\uB85D \uC0C8\uB85C\uACE0\uCE68", font, RefreshReceipts);
            independentInquiry = MakeButton(recoveryForm, "StartIndependentInquiry", "\uBCC4\uAC1C\uC758 \uC0C8 \uBB38\uC758", font, StartIndependentInquiry);
            forgetReceipt = MakeButton(recoveryForm, "ForgetReceipt", "\uC774 \uAE30\uAE30 \uAE30\uB85D \uC0AD\uC81C", font, BeginForget);
            forgetWarning = Text(recoveryForm, "ForgetWarning", "\uC11C\uBC84 \uC811\uC218 \uCDE8\uC18C\uAC00 \uC544\uB2D9\uB2C8\uB2E4. \uC774 \uAE30\uAE30 \uAE30\uB85D\uC744 \uC0AD\uC81C\uD558\uBA74 \uC774\uD6C4 \uC774 \uAE30\uAE30\uC5D0\uC11C \uD574\uB2F9 \uC811\uC218 \uC5EC\uBD80\uB97C \uC870\uD68C\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.", font, 220);
            confirmForget = MakeButton(recoveryForm, "ConfirmForgetReceipt", "\uC774 \uAE30\uAE30 \uAE30\uB85D \uC0AD\uC81C \uD655\uC778", font, ConfirmForget);
            cancelForget = MakeButton(recoveryForm, "CancelForgetReceipt", "\uC0AD\uC81C \uCDE8\uC18C", font, CancelForget);
            var position = 1;
            foreach (var component in new Component[] { label, pendingReceipts, recoveryStatus, refreshReceipts,
                independentInquiry, forgetReceipt, forgetWarning, confirmForget, cancelForget })
                component.transform.SetSiblingIndex(position++);
            if (isActiveAndEnabled) recovery.Changed += Render;
            Render();
        }

        private bool RecoveryBusy => recovery != null && (recovery.Busy || model.State == InquiryState.Sending);
        private bool CapacityBlocksSend => recovery != null && recovery.Records.Count >= 64 && model.Pending == null;
        private void SelectReceipt(int index)
        {
            if (recovery == null || RecoveryBusy || recovery.StorageFailed) { Render(); return; }
            deleteTarget = null; recoveryActionFailed = false;
            if (index == 0) recovery.Select(null);
            else if (index > 0 && index <= displayedRecords.Length) recovery.Select(displayedRecords[index - 1]);
            Render();
        }
        private async void RefreshReceipts()
        {
            if (recovery == null || RecoveryBusy) return;
            deleteTarget = null; recoveryActionFailed = false; pendingReceipts.Hide();
            try { await recovery.RefreshAsync(); }
            catch { recoveryActionFailed = true; }
            Render();
        }
        private void StartIndependentInquiry()
        {
            if (recovery == null || RecoveryBusy || recovery.StorageFailed) return;
            deleteTarget = null; recoveryActionFailed = false;
            recovery.StartIndependentInquiry(); Render();
        }
        private void BeginForget()
        {
            if (recovery == null || RecoveryBusy || recovery.StorageFailed || recovery.Selected == null) return;
            deleteTarget = recovery.Selected; Render();
        }
        private void CancelForget() { deleteTarget = null; Render(); }
        private async void ConfirmForget()
        {
            if (recovery == null || RecoveryBusy || recovery.StorageFailed || deleteTarget == null
                || !SameRecord(deleteTarget, recovery.Selected)) return;
            deleteTarget = null; recoveryActionFailed = false;
            try { await recovery.ForgetSelectedAsync(true); }
            catch { recoveryActionFailed = true; }
            Render();
        }
        private static bool SameRecord(PendingReceipt first, PendingReceipt second) => first != null && second != null
            && first.Kind == second.Kind && first.AccountId == second.AccountId && first.IdempotencyKey == second.IdempotencyKey;
        private static string RecordLabel(PendingReceipt record)
        {
            var kind = record.Kind == PendingReceiptKind.Support ? "\uC9C0\uC6D0 \uBB38\uC758" : "\uC624\uB958";
            var category = record.Kind == PendingReceiptKind.Support ? SupportInquiryHttpClient.CategoryLabel(record.Category)
                : record.Category == "UNITY_EXCEPTION" ? "\uC608\uC678" : record.Category == "UNITY_ASSERT" ? "\uAC80\uC99D \uC624\uB958" : "\uC77C\uBC18 \uC624\uB958";
            return record.SubmittedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " \u00B7 " + kind + " \u00B7 " + category;
        }

        private void Edit()
        {
            if (recovery != null && (RecoveryBusy || recovery.StorageFailed
                || (recovery.Selected?.Kind == PendingReceiptKind.ClientError && !recovery.ManagingDraft))) return;
            model.Edit(model.Categories[category.value], content.text);
        }
        private async void Submit()
        {
            if (recovery != null && (RecoveryBusy || recovery.StorageFailed || CapacityBlocksSend
                || (recovery.Selected?.Kind == PendingReceiptKind.ClientError && !recovery.ManagingDraft))) return;
            if (model.State == InquiryState.Received) { model.StartAnother(); return; }
            try { await model.SubmitAsync(); }
            catch { Render(); }
        }
        public void Show() { gameObject.SetActive(true); Render(); }
        private async void Reconcile()
        {
            if (recovery != null && (RecoveryBusy || recovery.StorageFailed || recovery.ManagingDraft
                || recovery.Selected?.Kind == PendingReceiptKind.ClientError)) return;
            try { await model.ReconcileAsync(); }
            catch { Render(); }
        }
        public void Hide() { deleteTarget = null; gameObject.SetActive(false); Closed?.Invoke(); }
        private void OnEnable()
        {
            if (model != null) { model.Changed -= Render; model.Changed += Render; }
            if (recovery != null) { recovery.Changed -= Render; recovery.Changed += Render; }
            Render();
        }
        private void OnDisable()
        {
            CancelPresentationDelay(); deleteTarget = null;
            if (model != null) model.Changed -= Render;
            if (recovery != null) recovery.Changed -= Render;
        }
        private void OnApplicationFocus(bool focused) { if (focused) Render(); }
        private void OnApplicationPause(bool paused) { if (!paused) Render(); }
        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled && reconcile != null) ApplySafeArea();
        }
        private void CancelPresentationDelay()
        {
            var scheduled = presentationDelay; presentationDelay = null;
            scheduled?.Cancel();
        }
        private async void SchedulePresentationChange()
        {
            CancelPresentationDelay();
            var delay = model.NextPresentationChangeDelay;
            if (!isActiveAndEnabled || !delay.HasValue) return;
            var scheduled = new CancellationTokenSource(); presentationDelay = scheduled;
            try
            {
                await Task.Delay((int)Math.Min(int.MaxValue - 1, Math.Max(1, Math.Ceiling(delay.Value.TotalMilliseconds))), scheduled.Token);
                if (this != null && isActiveAndEnabled && ReferenceEquals(presentationDelay, scheduled))
                {
                    presentationDelay = null;
                    Render();
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (ReferenceEquals(presentationDelay, scheduled)) presentationDelay = null;
                scheduled.Dispose();
            }
        }
        private void ApplySafeArea()
        {
            if (applyingLayout) return;
            applyingLayout = true;
            try { LayoutSafeArea(); }
            finally { applyingLayout = false; }
        }
        private void LayoutSafeArea()
        {
            if (safe == null || Screen.width <= 0 || Screen.height <= 0) return;
            var area = Screen.safeArea;
            safe.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            if (form == null) return;
            ((RectTransform)form.parent.parent).anchorMax = new Vector2(recoveryForm == null ? 1 : .55f, 1);
            FitScrollContent(form);
            if (recoveryForm != null) FitScrollContent(recoveryForm);
        }
        private void FitScrollContent(RectTransform target)
        {
            var layout = target.GetComponent<VerticalLayoutGroup>();
            var children = target.Cast<Transform>().Where(t => t.gameObject.activeSelf).ToArray();
            var height = layout.padding.top + layout.padding.bottom + Mathf.Max(0, children.Length - 1) * layout.spacing;
            foreach (var child in children) height += LayoutUtility.GetPreferredHeight((RectTransform)child);
            target.sizeDelta = new Vector2(0, Mathf.Max(height, safe.rect.height));
        }
        private void Render()
        {
            if (this == null || !isActiveAndEnabled || model == null || sendLabel == null || reconcile == null) return;
            reconcile.gameObject.SetActive(model.Pending != null &&
                (model.State == InquiryState.Uncertain || model.State == InquiryState.Sending));
            reconcile.interactable = model.CanReconcile;
            ApplySafeArea();
            shownWaitSeconds = model.RetryWaitSeconds;
            content.SetTextWithoutNotify(model.State == InquiryState.Detached ? "" : model.Content);
            category.SetValueWithoutNotify(model.Categories.ToList().IndexOf(model.Category));
            content.interactable = model.CanEdit; category.interactable = model.CanEdit;
            send.interactable = model.CanSubmit || model.State == InquiryState.Received;
            sendLabel.text = model.State == InquiryState.Received ? "\uC0C8 \uBB38\uC758" :
                model.State == InquiryState.Uncertain ? "\uAC19\uC740 \uBB38\uC758 \uB2E4\uC2DC \uBCF4\uB0B4\uAE30" : "\uBCF4\uB0B4\uAE30";
            switch (model.State)
            {
                case InquiryState.Sending: status.text = "\uBCF4\uB0B4\uB294 \uC911"; break;
                case InquiryState.Uncertain: status.text = "\uC811\uC218 \uC5EC\uBD80\uB97C \uD655\uC778\uD558\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4."; break;
                case InquiryState.Rejected: status.text = "\uC811\uC218\uB418\uC9C0 \uC54A\uC558\uC2B5\uB2C8\uB2E4. \uB0B4\uC6A9\uC744 \uD655\uC778\uD574 \uC8FC\uC138\uC694."; break;
                case InquiryState.Detached: status.text = "\uB85C\uADF8\uC778 \uD655\uC778\uC774 \uD544\uC694\uD569\uB2C8\uB2E4."; break;
                case InquiryState.Received: status.text = "\uC811\uC218\uB418\uC5C8\uC2B5\uB2C8\uB2E4.\n" + model.Receipt.TicketId; break;
                default: status.text = ""; break;
            }
            if (shownWaitSeconds > 0 && model.State != InquiryState.Detached)
                status.text = "\uB2E4\uC2DC \uC2DC\uB3C4\uAE4C\uC9C0 " + shownWaitSeconds + "\uCD08";
            if (recovery != null) RenderRecovery();
            SchedulePresentationChange();
        }
        private void RenderRecovery()
        {
            var busy = RecoveryBusy;
            if (busy || recovery.StorageFailed || !SameRecord(deleteTarget, recovery.Selected)) deleteTarget = null;
            displayedRecords = recovery.Records.ToArray();
            var options = new[] { displayedRecords.Length == 0 ? "\uBCF4\uB958 \uAE30\uB85D \uC5C6\uC74C" : "\uBCF4\uB958 \uAE30\uB85D \uC120\uD0DD" }
                .Concat(displayedRecords.Select(RecordLabel)).ToList();
            if (!pendingReceipts.options.Select(o => o.text).SequenceEqual(options))
            {
                pendingReceipts.Hide(); pendingReceipts.ClearOptions(); pendingReceipts.AddOptions(options);
            }
            pendingReceipts.SetValueWithoutNotify(Array.FindIndex(displayedRecords, item => SameRecord(item, recovery.Selected)) + 1);
            pendingReceipts.interactable = !busy && !recovery.StorageFailed && displayedRecords.Length > 0;
            refreshReceipts.interactable = !busy;
            independentInquiry.interactable = !busy && !recovery.StorageFailed && (recovery.Selected != null || model.Pending != null);
            forgetReceipt.interactable = !busy && !recovery.StorageFailed && recovery.Selected != null;
            var confirming = deleteTarget != null;
            forgetWarning.gameObject.SetActive(confirming); confirmForget.gameObject.SetActive(confirming); cancelForget.gameObject.SetActive(confirming);
            confirmForget.interactable = cancelForget.interactable = confirming && !busy;
            recoveryStatus.text = recoveryActionFailed ? "\uAE30\uB85D \uC791\uC5C5\uC744 \uC644\uB8CC\uD558\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4. \uB2E4\uC2DC \uC2DC\uB3C4\uD574 \uC8FC\uC138\uC694." : recovery.Notice;
            if (busy) recoveryStatus.text = "\uBCF4\uB958 \uAE30\uB85D\uC744 \uD655\uC778\uD558\uB294 \uC911\uC785\uB2C8\uB2E4.";
            var errorSelected = recovery.Selected?.Kind == PendingReceiptKind.ClientError;
            var blocked = busy || recovery.StorageFailed || (errorSelected && !recovery.ManagingDraft);
            content.interactable &= !blocked; category.interactable &= !blocked; send.interactable &= !blocked;
            send.interactable &= !CapacityBlocksSend;
            reconcile.interactable &= !blocked && !recovery.ManagingDraft;
            if (errorSelected || recovery.ManagingDraft) reconcile.gameObject.SetActive(false);
            send.gameObject.SetActive(model.Pending?.ReceiptOnly != true && (!errorSelected || recovery.ManagingDraft));
            if (recovery.StorageFailed || recoveryActionFailed) status.text = recoveryStatus.text;
            ApplySafeArea();
        }
        private void OnDestroy()
        {
            CancelPresentationDelay();
            if (model != null) model.Changed -= Render;
            if (recovery != null) recovery.Changed -= Render;
        }
        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero; return rect;
        }
        private static LayoutElement Size(GameObject target, float height)
        {
            var size = target.AddComponent<LayoutElement>(); size.minHeight = height; size.preferredHeight = height; return size;
        }
        private static TMP_Text Text(Transform parent, string name, string label, TMP_FontAsset font, float height, float fontSize = 38)
        {
            var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = label; text.fontSize = fontSize; text.color = Color.white;
            text.richText = false; text.raycastTarget = false; Size(text.gameObject, height); return text;
        }
        private static Button MakeButton(Transform parent, string name, string label, TMP_FontAsset font, Action click)
        {
            var rect = Rect(name, parent); Size(rect.gameObject, 144);
            var graphic = rect.gameObject.AddComponent<Image>(); graphic.color = new Color(0.12f, 0.3f, 0.27f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = graphic;
            var text = Text(rect, "Label", label, font, 144); text.alignment = TextAlignmentOptions.Center;
            button.onClick.AddListener(() => click()); return button;
        }
    }
}
