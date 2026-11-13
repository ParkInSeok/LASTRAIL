using System;
using NUnit.Framework;
using LastRail.Sortie;

namespace LastRail.Tests.EditMode
{
    public sealed class SortieProgressionTests
    {
        [Test]
        public void StartActOne_BeginsAtFirstOfFifteenWaves()
        {
            var progression = SortieProgression.StartActOne("waveplan_act1_opn03_seed9876");

            Assert.That(progression.WavePlanId, Is.EqualTo("waveplan_act1_opn03_seed9876"));
            Assert.That(progression.Stage, Is.EqualTo(SortieStage.Wave));
            Assert.That(progression.CurrentWaveNumber, Is.EqualTo(1));
            Assert.That(progression.CompletedWaveCount, Is.EqualTo(0));
            Assert.That(progression.TotalWaveCount, Is.EqualTo(15));
            Assert.That(progression.IsComplete, Is.EqualTo(false));
        }

        [Test]
        public void CompleteCurrentWave_AdvancesThroughFifteenWavesThenFinalOperation()
        {
            var progression = SortieProgression.StartActOne("waveplan_act1_opn03_seed9876");

            for (var i = 1; i <= 14; i++)
            {
                progression = progression.CompleteCurrentWave();
                Assert.That(progression.Stage, Is.EqualTo(SortieStage.Wave));
                Assert.That(progression.CurrentWaveNumber, Is.EqualTo(i + 1));
                Assert.That(progression.CompletedWaveCount, Is.EqualTo(i));
            }

            progression = progression.CompleteCurrentWave();

            Assert.That(progression.Stage, Is.EqualTo(SortieStage.FinalOperation));
            Assert.That(progression.CompletedWaveCount, Is.EqualTo(15));
            Assert.That(progression.CurrentWaveNumber, Is.EqualTo(0));
            Assert.That(progression.IsComplete, Is.EqualTo(false));
        }

        [Test]
        public void CompleteFinalOperation_MarksSortieComplete()
        {
            var progression = SortieProgression.StartActOne("waveplan_act1_opn03_seed9876");
            for (var i = 0; i < 15; i++)
            {
                progression = progression.CompleteCurrentWave();
            }

            var completed = progression.CompleteFinalOperation();

            Assert.That(completed.Stage, Is.EqualTo(SortieStage.Complete));
            Assert.That(completed.IsComplete, Is.EqualTo(true));
            Assert.That(completed.CompletedWaveCount, Is.EqualTo(15));
        }

        [Test]
        public void CompleteCurrentWave_RejectsWhenNotInWaveStage()
        {
            var progression = SortieProgression.StartActOne("waveplan_act1_opn03_seed9876");
            for (var i = 0; i < 15; i++)
            {
                progression = progression.CompleteCurrentWave();
            }

            Assert.Throws<InvalidOperationException>(() => progression.CompleteCurrentWave());
        }
    }
}
