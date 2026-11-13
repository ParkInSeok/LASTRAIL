using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace LastRail.Auth
{
    internal sealed class PasswordCredentialRuntime : IDisposable
    {
        private readonly PasswordEmailClient client;
        private readonly PlayerAuthOwner owner;
        private readonly IGameApiAuthTokenReader reader;
        private readonly KeystoreAuthReplayJournal journal;
        private readonly Transform parent;
        private readonly TMP_FontAsset font;
        private readonly Action hideLogin, showLogin;
        private PasswordCredentialPanel panel;
        private PasswordCredentialFlow flow;
        private bool opening, disposed, adopting;
        private long epoch;
        public bool Busy => opening || flow?.Busy == true;
        public PasswordCredentialRuntime(PasswordEmailClient client, PlayerAuthOwner owner,
            IGameApiAuthTokenReader reader, KeystoreAuthReplayJournal journal, Transform parent,
            TMP_FontAsset font, Action hideLogin, Action showLogin)
        {
            this.client = client; this.owner = owner; this.reader = reader; this.journal = journal;
            this.parent = parent; this.font = font; this.hideLogin = hideLogin; this.showLogin = showLogin;
        }
        public async void Open(string mode)
        {
            if (disposed || Busy || owner.Busy || (flow != null && flow.Stage != PasswordCredentialStage.Cancelled
                && flow.Stage != PasswordCredentialStage.Completed)) return;
            if (mode == "Link" && (!owner.Ready || owner.Session == null)) return;
            if (mode == "Register" && !owner.CanCreateGuest) return;
            opening = true;
            var stamp = ++epoch; var generation = owner.Generation;
            try
            {
                var stored = await reader.ReadAsync();
                if (disposed || stamp != epoch || generation != owner.Generation || owner.Busy || journal?.Pending != null) return;
                if (stored == null || (stored.Outcome != GameApiAuthTokenReadOutcome.Missing && stored.Outcome != GameApiAuthTokenReadOutcome.Loaded)) return;
                if (mode == "Register" && (stored.Outcome != GameApiAuthTokenReadOutcome.Missing || !owner.CanCreateGuest)) return;
                RemovePanel();
                flow = new PasswordCredentialFlow(client, mode, () => owner.Session, () => owner.Generation,
                    candidate => AdoptAsync(candidate, stamp, mode), () => DateTimeOffset.UtcNow);
                var root = new GameObject("PasswordCredentialPanel", typeof(RectTransform)); root.transform.SetParent(parent, false);
                panel = root.AddComponent<PasswordCredentialPanel>(); panel.Initialize(flow, font);
                flow.Changed += FlowChanged;
                panel.Closed += Closed;
                hideLogin(); panel.Show();
            }
            catch { if (!disposed) showLogin(); }
            finally { opening = false; }
        }
        private async Task<bool> AdoptAsync(GameApiAuthSessionSnapshot candidate, long stamp, string mode)
        {
            if (disposed || epoch != stamp || owner.Busy || flow.Stage == PasswordCredentialStage.Cancelled) return false;
            var original = owner.Session?.AccountId; var generation = owner.Generation;
            var stored = await reader.ReadAsync();
            if (disposed || epoch != stamp || owner.Busy || generation != owner.Generation || journal?.Pending != null
                || flow == null || flow.Stage == PasswordCredentialStage.Cancelled) return false;
            if (mode == "Register" ? stored?.Outcome != GameApiAuthTokenReadOutcome.Missing || original != null
                : stored?.Outcome != GameApiAuthTokenReadOutcome.Loaded || candidate.AccountId != original || stored.Session?.AccountId != original) return false;
            adopting = true;
            // Nickname entry belongs to the main auth panel, below this modal's canvas.
            panel.Hide();
            try
            {
                await owner.LoginAsync(() => Task.FromResult(!disposed && epoch == stamp ? candidate : null));
                return !disposed && epoch == stamp && ReferenceEquals(owner.Session, candidate);
            }
            finally { adopting = false; }
        }
        private void FlowChanged()
        {
            if (disposed || flow == null || flow.Busy) return;
            if (flow.Stage == PasswordCredentialStage.Completed)
            {
                panel.Hide();
                if (flow.Mode == "Reset") showLogin();
            }
            else if (flow.Stage == PasswordCredentialStage.Uncertain) panel.Show();
            else if (flow.Stage == PasswordCredentialStage.Cancelled && flow.Mode != "Link") showLogin();
        }
        private void Closed() { if (flow.Mode != "Link") showLogin(); }
        public void Cancel(bool accountInvalidated = false)
        {
            if (disposed) return;
            if (adopting && !accountInvalidated) return;
            epoch++; flow?.Cancel(); if (panel != null) panel.Hide();
        }
        private void RemovePanel()
        {
            if (flow != null) flow.Changed -= FlowChanged;
            if (panel == null) return;
            panel.Closed -= Closed;
            if (Application.isPlaying) UnityEngine.Object.Destroy(panel.gameObject);
            else UnityEngine.Object.DestroyImmediate(panel.gameObject);
            panel = null;
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; epoch++; flow?.Cancel(); RemovePanel();
        }
    }
}
