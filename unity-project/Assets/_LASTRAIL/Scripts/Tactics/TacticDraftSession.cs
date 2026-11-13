using System;
using System.Collections.Generic;

namespace LastRail.Tactics
{
    public sealed class TacticDraftSession
    {
        private const int OptionCount = 3;

        private TacticDraftSession(
            int waveNumber,
            IReadOnlyList<string> resolvedTacticPool,
            IReadOnlyList<TacticDraftOption> options,
            TacticDraftRefreshState refreshState,
            int nextOptionStartIndex,
            bool isSelectionConfirmed,
            string selectedTacticId,
            bool containsUltimateTactic)
        {
            WaveNumber = waveNumber;
            ResolvedTacticPool = resolvedTacticPool;
            Options = options;
            RefreshState = refreshState;
            NextOptionStartIndex = nextOptionStartIndex;
            IsSelectionConfirmed = isSelectionConfirmed;
            SelectedTacticId = selectedTacticId;
            ContainsUltimateTactic = containsUltimateTactic;
        }

        public int WaveNumber { get; }
        public IReadOnlyList<string> ResolvedTacticPool { get; }
        public IReadOnlyList<TacticDraftOption> Options { get; }
        public TacticDraftRefreshState RefreshState { get; }
        public int NextOptionStartIndex { get; }
        public bool IsSelectionConfirmed { get; }
        public string SelectedTacticId { get; }
        public bool ContainsUltimateTactic { get; }

        public static TacticDraftSession Start(int waveNumber, IReadOnlyList<string> resolvedTacticPool)
        {
            return StartFromResolvedPool(
                waveNumber,
                new ResolvedTacticPool(resolvedTacticPool, containsUltimateTactic: false));
        }

        public static TacticDraftSession StartFromResolvedPool(int waveNumber, ResolvedTacticPool resolvedTacticPool)
        {
            if (resolvedTacticPool == null)
            {
                throw new ArgumentNullException(nameof(resolvedTacticPool));
            }

            if (!TacticDraftRules.IsCommandTacticDraftWave(waveNumber))
            {
                throw new InvalidOperationException("Command tactic selection is not available at this wave.");
            }

            var copiedPool = CopyAndValidatePool(resolvedTacticPool.TacticIds);
            return new TacticDraftSession(
                waveNumber,
                copiedPool,
                BuildOptions(copiedPool, 0),
                TacticDraftRefreshState.CreateForSelection(waveNumber),
                OptionCount,
                false,
                string.Empty,
                resolvedTacticPool.ContainsUltimateTactic);
        }

        public TacticDraftSession UseFreeRefresh()
        {
            EnsureSelectionIsOpen();
            return WithRefreshedOptions(RefreshState.UseFreeRefresh());
        }

        public TacticDraftSession UseAdRefresh()
        {
            EnsureSelectionIsOpen();
            return WithRefreshedOptions(RefreshState.UseAdRefresh());
        }

        public TacticDraftSession ConfirmSelection(string tacticId)
        {
            EnsureSelectionIsOpen();
            if (string.IsNullOrWhiteSpace(tacticId))
            {
                throw new ArgumentException("Tactic ID cannot be empty.", nameof(tacticId));
            }

            if (!ContainsCurrentOption(tacticId))
            {
                throw new InvalidOperationException("Selected tactic is not in the current draft options.");
            }

            return new TacticDraftSession(
                WaveNumber,
                ResolvedTacticPool,
                Options,
                RefreshState,
                NextOptionStartIndex,
                true,
                tacticId,
                ContainsUltimateTactic);
        }

        private TacticDraftSession WithRefreshedOptions(TacticDraftRefreshState nextRefreshState)
        {
            return new TacticDraftSession(
                WaveNumber,
                ResolvedTacticPool,
                BuildOptions(ResolvedTacticPool, NextOptionStartIndex),
                nextRefreshState,
                NextOptionStartIndex + OptionCount,
                false,
                string.Empty,
                ContainsUltimateTactic);
        }

        private void EnsureSelectionIsOpen()
        {
            if (IsSelectionConfirmed)
            {
                throw new InvalidOperationException("Tactic selection is already confirmed.");
            }
        }

        private bool ContainsCurrentOption(string tacticId)
        {
            for (var i = 0; i < Options.Count; i++)
            {
                if (string.Equals(Options[i].TacticId, tacticId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<string> CopyAndValidatePool(IReadOnlyList<string> resolvedTacticPool)
        {
            if (resolvedTacticPool == null)
            {
                throw new ArgumentNullException(nameof(resolvedTacticPool));
            }

            if (resolvedTacticPool.Count < OptionCount)
            {
                throw new InvalidOperationException("Resolved tactic pool must contain at least three tactics.");
            }

            var copied = new string[resolvedTacticPool.Count];
            for (var i = 0; i < resolvedTacticPool.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(resolvedTacticPool[i]))
                {
                    throw new ArgumentException("Resolved tactic pool cannot contain empty IDs.", nameof(resolvedTacticPool));
                }

                copied[i] = resolvedTacticPool[i];
            }

            return copied;
        }

        private static IReadOnlyList<TacticDraftOption> BuildOptions(IReadOnlyList<string> pool, int startIndex)
        {
            var options = new TacticDraftOption[OptionCount];
            for (var i = 0; i < OptionCount; i++)
            {
                options[i] = new TacticDraftOption(pool[(startIndex + i) % pool.Count]);
            }

            return options;
        }
    }
}
