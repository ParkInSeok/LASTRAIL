using System;
using System.Collections.Generic;
using LastRail.Presentation.Battle;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class BattleScreenPresenterTests
    {
        [Test]
        public void RootPresenterCreatesDefaultPortraitStateFromCurrentTrainCar()
        {
            var state = BattleScreenRootPresenter.CreateDefault(CreateTrainCars(), CreateSquadMembers(0));

            Assert.That(state.Layout.ExternalBattleHeightRatio, Is.EqualTo(0.55f));
            Assert.That(state.Layout.CurrentTrainCarInteriorHeightRatio, Is.EqualTo(0.45f));
            Assert.That(state.CurrentSlotIndex, Is.EqualTo(0));
            Assert.That(state.TrainCars.Count, Is.EqualTo(4));
            Assert.That(state.SquadMembers.Count, Is.EqualTo(5));
            Assert.That(state.HasRedeployPreview, Is.EqualTo(false));
        }

        [Test]
        public void TrainCarStripPresenterMarksOnlyTheRequestedCurrentSlot()
        {
            var trainCars = TrainCarStripPresenter.MarkCurrent(CreateTrainCars(), 2);

            Assert.That(trainCars[0].IsCurrent, Is.EqualTo(false));
            Assert.That(trainCars[1].IsCurrent, Is.EqualTo(false));
            Assert.That(trainCars[2].IsCurrent, Is.EqualTo(true));
            Assert.That(trainCars[3].IsCurrent, Is.EqualTo(false));
            Assert.That(trainCars[2].TrainCarId, Is.EqualTo("car_cargo"));
            Assert.That(trainCars[2].DisplayLabel, Is.EqualTo("화물칸"));
        }

        [Test]
        public void SquadRedeployPresenterCreatesPreviewWithoutMutatingCurrentState()
        {
            var state = BattleScreenRootPresenter.CreateDefault(CreateTrainCars(), CreateSquadMembers(0));

            var next = SquadRedeployPresenter.PreviewRedeploy(state, 2);

            Assert.That(state.HasRedeployPreview, Is.EqualTo(false));
            Assert.That(next.HasRedeployPreview, Is.EqualTo(true));
            Assert.That(next.RedeployPreview.CurrentSlotIndex, Is.EqualTo(0));
            Assert.That(next.RedeployPreview.TargetSlotIndex, Is.EqualTo(2));
            Assert.That(next.RedeployPreview.DestinationTrainCarId, Is.EqualTo("car_cargo"));
            Assert.That(state.SquadMembers[0].CurrentSlotIndex, Is.EqualTo(0));
            Assert.That(next.RedeployPreview.PreviewMembers[0].CurrentSlotIndex, Is.EqualTo(2));
        }

        [Test]
        public void SquadRedeployPresenterConfirmMovesWholeSquadAndCurrentTrainCar()
        {
            var state = BattleScreenRootPresenter.CreateDefault(CreateTrainCars(), CreateSquadMembers(0));
            var previewed = SquadRedeployPresenter.PreviewRedeploy(state, 3);

            var confirmed = SquadRedeployPresenter.ConfirmRedeploy(previewed);

            Assert.That(confirmed.CurrentSlotIndex, Is.EqualTo(3));
            Assert.That(confirmed.HasRedeployPreview, Is.EqualTo(false));
            Assert.That(confirmed.TrainCars[3].IsCurrent, Is.EqualTo(true));
            Assert.That(confirmed.TrainCars[0].IsCurrent, Is.EqualTo(false));
            Assert.That(confirmed.SquadMembers.Count, Is.EqualTo(5));
            foreach (var member in confirmed.SquadMembers)
            {
                Assert.That(member.CurrentSlotIndex, Is.EqualTo(3));
            }
        }

        [Test]
        public void SquadRedeployPresenterRejectsConfirmWithoutPreview()
        {
            var state = BattleScreenRootPresenter.CreateDefault(CreateTrainCars(), CreateSquadMembers(0));

            Assert.Throws<InvalidOperationException>(() => SquadRedeployPresenter.ConfirmRedeploy(state));
        }

        [Test]
        public void RootPresenterRejectsMultipleCurrentTrainCars()
        {
            var trainCars = new[]
            {
                new TrainCarViewState(0, "car_combat", "전투칸", true),
                new TrainCarViewState(1, "car_gun", "포격칸", true),
            };

            Assert.Throws<ArgumentException>(() => BattleScreenRootPresenter.CreateDefault(trainCars, CreateSquadMembers(0)));
        }

        private static IReadOnlyList<TrainCarViewState> CreateTrainCars()
        {
            return new[]
            {
                new TrainCarViewState(0, "car_combat", "전투칸", true),
                new TrainCarViewState(1, "car_gun", "포격칸", false),
                new TrainCarViewState(2, "car_cargo", "화물칸", false),
                new TrainCarViewState(3, "car_defense", "방어칸", false),
            };
        }

        private static IReadOnlyList<SquadMemberViewState> CreateSquadMembers(int slotIndex)
        {
            return new[]
            {
                new SquadMemberViewState("op_kyle", "카일", slotIndex),
                new SquadMemberViewState("op_reina", "레이나", slotIndex),
                new SquadMemberViewState("op_eve", "이브", slotIndex),
                new SquadMemberViewState("op_sera", "세라", slotIndex),
                new SquadMemberViewState("op_mira", "미라", slotIndex),
            };
        }
    }
}
