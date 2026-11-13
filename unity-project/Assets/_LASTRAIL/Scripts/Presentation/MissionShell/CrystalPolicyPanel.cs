using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Binds existing TMP/button slots. No transport, economy calculation or
    // purchase adapter is installed by this view.
    public sealed class CrystalPolicyPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text walletText;
        [SerializeField] private TMP_Text historyText;
        [SerializeField] private TMP_Text recoveryText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button refreshButton;
        private CrystalPolicyPresentation model;

        public void ApplyWalletResponse(int statusCode, string json)
        {
            if (model == null) Bind(new CrystalPolicyPresentation());
            model.ApplyWalletRead(CrystalWalletRead.Parse(statusCode, json));
        }

        public void Bind(CrystalPolicyPresentation value)
        {
            Detach(); model = value;
            if (isActiveAndEnabled) Attach();
            Render();
        }
        private void OnEnable() { Attach(); Render(); }
        private void OnDisable() => Detach();
        private void Attach()
        {
            Detach();
            if (model != null) model.Changed += Render;
            if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
            if (refreshButton != null) refreshButton.onClick.AddListener(Refresh);
        }
        private void Detach()
        {
            if (model != null) model.Changed -= Render;
            if (confirmButton != null) confirmButton.onClick.RemoveListener(Confirm);
            if (refreshButton != null) refreshButton.onClick.RemoveListener(Refresh);
        }
        private void Confirm() => model?.Confirm();
        private void Refresh() => model?.RequestRefresh();
        private void Render()
        {
            Set(walletText, model?.WalletAvailable == true
                ? model.CurrencyName + "\n\uD569\uACC4 " + model.Total + "\nFREE " + model.Free + "\nPAID " + model.Paid
                : model?.WalletStatus == CrystalWalletReadStatus.Unconfigured ? "\uC7AC\uD654 \uC124\uC815\uC744 \uC900\uBE44 \uC911\uC785\uB2C8\uB2E4."
                : model?.WalletStatus == CrystalWalletReadStatus.AuthRequired ? "\uB85C\uADF8\uC778\uC774 \uD544\uC694\uD569\uB2C8\uB2E4."
                : "\uC7AC\uD654 \uC815\uBCF4\uB97C \uC0AC\uC6A9\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.");
            Set(historyText, model?.WalletAvailable == true && model.History.Count > 0 ? string.Join("\n", model.History) : "-");
            var status = model?.RecoveryStatus ?? RecoveryConfirmationStatus.Unavailable;
            var message = status == RecoveryConfirmationStatus.Submitting || status == RecoveryConfirmationStatus.Refreshing
                ? "\uCC98\uB9AC \uC911"
                : status == RecoveryConfirmationStatus.Error ? "\uCC98\uB9AC \uACB0\uACFC\uB97C \uB2E4\uC2DC \uD655\uC778\uD574 \uC8FC\uC138\uC694."
                : status == RecoveryConfirmationStatus.Complete ? "\uCC98\uB9AC\uAC00 \uC644\uB8CC\uB418\uC5C8\uC2B5\uB2C8\uB2E4."
                : status == RecoveryConfirmationStatus.Ready ? "\uCD9C\uC11D \uBCF5\uAD6C"
                : "\uD604\uC7AC \uC0AC\uC6A9\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.";
            if (model?.Cost != null && status != RecoveryConfirmationStatus.Complete)
                message += "\n\uBE44\uC6A9 " + model.Cost + "\n\uBCF4\uC0C1 " + model.Reward
                    + "\n\uD655\uC778 \uAE30\uC900 \uC794\uC561 " + model.Balance;
            Set(recoveryText, message);
            if (confirmButton != null) confirmButton.interactable = model?.CanConfirm == true;
            if (refreshButton != null) refreshButton.interactable = model?.CanRefresh == true;
        }
        private static void Set(TMP_Text target, string value)
        {
            if (target == null) return;
            target.richText = false; target.enableAutoSizing = true;
            target.fontSizeMin = 18; target.fontSizeMax = 28; target.text = value;
        }
    }
}
