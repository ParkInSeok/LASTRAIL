using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LastRail.Presentation.MissionShell
{
    public enum RecoveryConfirmationStatus { Unavailable, Refreshing, Ready, Submitting, Error, Complete }

    // Presentation input only, not a wallet or recovery HTTP DTO. An approved
    // server adapter must supply authoritative display values and eligibility.
    public sealed class CrystalPolicyPresentation
    {
        private long generation;
        private string selection;
        private bool eligible;
        public bool WalletAvailable { get; private set; }
        public CrystalWalletReadStatus WalletStatus { get; private set; }
        public string CurrencyName { get; private set; }
        public string Total { get; private set; }
        public string Free { get; private set; }
        public string Paid { get; private set; }
        public IReadOnlyList<string> History { get; private set; } = Array.Empty<string>();
        public string Cost { get; private set; }
        public string Reward { get; private set; }
        public string Balance { get; private set; }
        public RecoveryConfirmationStatus RecoveryStatus { get; private set; }
        public bool CanConfirm => RecoveryStatus == RecoveryConfirmationStatus.Ready && eligible && ConfirmRequested != null;
        public bool CanRefresh => RefreshRequested != null && RecoveryStatus != RecoveryConfirmationStatus.Submitting
            && RecoveryStatus != RecoveryConfirmationStatus.Refreshing;
        public event Action Changed;
        public event Action<long, string> ConfirmRequested;
        public event Action<long> RefreshRequested;

        public void SetWallet(string name, string total, string free, string paid, string[] history)
        {
            var entries = (history ?? Array.Empty<string>()).Select(Require).ToArray();
            var title = Require(name); var sum = Require(total); var earned = Require(free); var purchased = Require(paid);
            CurrencyName = title; Total = sum; Free = earned; Paid = purchased;
            History = Array.AsReadOnly(entries); WalletAvailable = true; WalletStatus = CrystalWalletReadStatus.Ready; Changed?.Invoke();
        }

        public void ApplyWalletRead(CrystalWalletRead read)
        {
            if (read == null) throw new ArgumentNullException(nameof(read));
            ResetState();
            if (read.Status == CrystalWalletReadStatus.Ready)
            {
                SetWallet(read.CurrencyId, read.TotalBalance.Value.ToString(CultureInfo.InvariantCulture),
                    read.FreeBalance.Value.ToString(CultureInfo.InvariantCulture), read.PaidBalance.Value.ToString(CultureInfo.InvariantCulture),
                    Array.Empty<string>());
                return;
            }
            WalletStatus = read.Status; Changed?.Invoke();
        }

        public static string RewardSourceLabel(string origin)
        {
            switch (origin)
            {
                case "ATTENDANCE": case "FREE_PASS": case "PREMIUM_PASS": return "FREE";
                default: throw new ArgumentException("An approved reward origin is required.", nameof(origin));
            }
        }

        public void SetRecoveryQuote(string selectionKey, string cost, string reward, string balance, bool serverEligible)
        {
            if (RecoveryStatus == RecoveryConfirmationStatus.Submitting)
                throw new InvalidOperationException("Resolve the pending request before replacing its selection.");
            var key = Require(selectionKey); var debit = Require(cost); var grant = Require(reward); var available = Require(balance);
            ++generation; selection = key; Cost = debit; Reward = grant; Balance = available;
            eligible = serverEligible; RecoveryStatus = RecoveryConfirmationStatus.Ready; Changed?.Invoke();
        }

        public long Confirm()
        {
            if (!CanConfirm) return 0;
            var attempt = ++generation; var key = selection; var callback = ConfirmRequested;
            RecoveryStatus = RecoveryConfirmationStatus.Submitting; Changed?.Invoke();
            // Observers may close/reset the view while it enters its loading state.
            if (generation != attempt || RecoveryStatus != RecoveryConfirmationStatus.Submitting) return 0;
            try { callback(attempt, key); }
            catch { Resolve(attempt, false); }
            return attempt;
        }

        public void Resolve(long attempt, bool success)
        {
            if (attempt != generation || RecoveryStatus != RecoveryConfirmationStatus.Submitting) return;
            eligible = false;
            RecoveryStatus = success ? RecoveryConfirmationStatus.Complete : RecoveryConfirmationStatus.Error;
            // No optimistic balance mutation or inferred reward grant.
            Changed?.Invoke();
        }

        public void RequestRefresh()
        {
            if (!CanRefresh) return;
            var attempt = ++generation; var callback = RefreshRequested; selection = null; eligible = false;
            RecoveryStatus = RecoveryConfirmationStatus.Refreshing;
            Cost = Reward = Balance = null; Changed?.Invoke();
            if (generation != attempt || RecoveryStatus != RecoveryConfirmationStatus.Refreshing) return;
            try { callback(attempt); }
            catch { FailRefresh(attempt); }
        }

        public void FailRefresh(long attempt)
        {
            if (attempt != generation || RecoveryStatus != RecoveryConfirmationStatus.Refreshing) return;
            RecoveryStatus = RecoveryConfirmationStatus.Error; Changed?.Invoke();
        }

        public bool ApplyRefreshedQuote(long attempt, string selectionKey, string cost, string reward, string balance, bool serverEligible)
        {
            if (attempt != generation || RecoveryStatus != RecoveryConfirmationStatus.Refreshing) return false;
            SetRecoveryQuote(selectionKey, cost, reward, balance, serverEligible);
            return true;
        }

        public void Reset()
        {
            ResetState(); Changed?.Invoke();
        }

        private void ResetState()
        {
            ++generation; selection = null; eligible = false; WalletAvailable = false;
            WalletStatus = CrystalWalletReadStatus.Unavailable;
            CurrencyName = Total = Free = Paid = Cost = Reward = Balance = null;
            History = Array.Empty<string>(); RecoveryStatus = RecoveryConfirmationStatus.Unavailable;
        }

        private static string Require(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Explicit server display value required.");
            return text;
        }
    }
}
