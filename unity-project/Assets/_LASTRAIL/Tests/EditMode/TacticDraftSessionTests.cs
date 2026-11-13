using System;
using NUnit.Framework;
using LastRail.Tactics;

namespace LastRail.Tests.EditMode
{
    public sealed class TacticDraftSessionTests
    {
        [Test]
        public void Start_CreatesThreeOptionsFromResolvedTacticPool()
        {
            var session = TacticDraftSession.Start(6, Pool());

            Assert.That(session.WaveNumber, Is.EqualTo(6));
            Assert.That(session.Options.Count, Is.EqualTo(3));
            Assert.That(session.Options[0].TacticId, Is.EqualTo("tac_001"));
            Assert.That(session.Options[1].TacticId, Is.EqualTo("tac_002"));
            Assert.That(session.Options[2].TacticId, Is.EqualTo("tac_003"));
            Assert.That(session.RefreshState.CanUseFreeRefresh, Is.EqualTo(true));
            Assert.That(session.RefreshState.CanUseAdRefresh, Is.EqualTo(true));
            Assert.That(session.IsSelectionConfirmed, Is.EqualTo(false));
        }

        [Test]
        public void UseFreeRefresh_ReplacesOptionsAndMarksFreeRefreshUsed()
        {
            var session = TacticDraftSession.Start(6, Pool()).UseFreeRefresh();

            Assert.That(session.Options.Count, Is.EqualTo(3));
            Assert.That(session.Options[0].TacticId, Is.EqualTo("tac_004"));
            Assert.That(session.Options[2].TacticId, Is.EqualTo("tac_006"));
            Assert.That(session.RefreshState.HasUsedFreeRefresh, Is.EqualTo(true));
            Assert.That(session.RefreshState.HasUsedAdRefresh, Is.EqualTo(false));
        }

        [Test]
        public void UseAdRefresh_AfterFreeRefresh_ReplacesOptionsAndMarksBothRefreshesUsed()
        {
            var session = TacticDraftSession.Start(6, Pool())
                .UseFreeRefresh()
                .UseAdRefresh();

            Assert.That(session.Options[0].TacticId, Is.EqualTo("tac_007"));
            Assert.That(session.Options[2].TacticId, Is.EqualTo("tac_009"));
            Assert.That(session.RefreshState.CanUseFreeRefresh, Is.EqualTo(false));
            Assert.That(session.RefreshState.CanUseAdRefresh, Is.EqualTo(false));
        }

        [Test]
        public void ConfirmSelection_AcceptsOnlyCurrentlyOfferedTactic()
        {
            var session = TacticDraftSession.Start(6, Pool()).UseFreeRefresh();

            var selected = session.ConfirmSelection("tac_005");

            Assert.That(selected.IsSelectionConfirmed, Is.EqualTo(true));
            Assert.That(selected.SelectedTacticId, Is.EqualTo("tac_005"));
            Assert.Throws<InvalidOperationException>(() => session.ConfirmSelection("tac_001"));
        }

        [Test]
        public void Refresh_RejectsAfterSelectionIsConfirmed()
        {
            var selected = TacticDraftSession.Start(6, Pool()).ConfirmSelection("tac_002");

            Assert.Throws<InvalidOperationException>(() => selected.UseFreeRefresh());
            Assert.Throws<InvalidOperationException>(() => selected.UseAdRefresh());
        }

        [Test]
        public void Start_RejectsNonDraftWaveOrPoolWithFewerThanThreeTactics()
        {
            Assert.Throws<InvalidOperationException>(() => TacticDraftSession.Start(4, Pool()));
            Assert.Throws<InvalidOperationException>(() => TacticDraftSession.Start(6, new[] { "tac_001", "tac_002" }));
        }

        private static string[] Pool()
        {
            return new[]
            {
                "tac_001",
                "tac_002",
                "tac_003",
                "tac_004",
                "tac_005",
                "tac_006",
                "tac_007",
                "tac_008",
                "tac_009"
            };
        }
    }
}
