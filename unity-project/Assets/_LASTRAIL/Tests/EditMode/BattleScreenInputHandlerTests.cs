using System;
using System.Collections.Generic;
using LastRail.Presentation.Battle;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class BattleScreenInputHandlerTests
    {
        [Test]
        public void SelectTrainCarSlotCreatesRedeployPreviewForAnotherSlot()
        {
            var state = BattleScreenRootPresenter.CreateDefault(CreateTrainCars(), CreateSquadMembers(0));

            var result = BattleScreenInputHandler.SelectTrainCarSlot(state, 2);

            Assert.That(result.Action, Is.EqualTo(BattleScreenInputAction.PreviewRedeploy));
            Assert.That(result.State.HasRedeployPreview, Is.EqualTo(true));
            Assert.That(result.State.RedeployPreview.TargetSlotIndex, Is.EqualTo(2));
            Assert.That(result.State.RedeployPreview.DestinationTrainCarId, Is.EqualTo("car_cargo"));
            Assert.That(result.IsBlockingRunFlow, Is.EqualTo(false));
        }

        [Test]
        public void SelectCurrentTrainCarSlotKeepsStateAsNoOp()
        {
            var state = BattleScreenRootPresenter.CreateDefault(CreateTrainCars(), CreateSquadMembers(0));

            var result = BattleScreenInputHandler.SelectTrainCarSlot(state, 0);

            Assert.That(result.Action, Is.EqualTo(BattleScreenInputAction.None));
            Assert.That(result.State, Is.EqualTo(state));
            Assert.That(result.State.HasRedeployPreview, Is.EqualTo(false));
            Assert.That(result.IsBlockingRunFlow, Is.EqualTo(false));
        }

        [Test]
        public void ConfirmRedeployMovesSquadAndMarksCurrentSlot()
        {
            var state = BattleScreenRootPresenter.CreateDefault(CreateTrainCars(), CreateSquadMembers(0));
            var previewResult = BattleScreenInputHandler.SelectTrainCarSlot(state, 3);

            var result = BattleScreenInputHandler.ConfirmRedeploy(previewResult.State);

            Assert.That(result.Action, Is.EqualTo(BattleScreenInputAction.ConfirmRedeploy));
            Assert.That(result.State.CurrentSlotIndex, Is.EqualTo(3));
            Assert.That(result.State.HasRedeployPreview, Is.EqualTo(false));
            Assert.That(result.State.TrainCars[3].IsCurrent, Is.EqualTo(true));
            Assert.That(result.State.SquadMembers[4].CurrentSlotIndex, Is.EqualTo(3));
            Assert.That(result.IsBlockingRunFlow, Is.EqualTo(false));
        }

        [Test]
        public void CancelRedeployClearsPreviewWithoutMovingSquad()
        {
            var state = BattleScreenRootPresenter.CreateDefault(CreateTrainCars(), CreateSquadMembers(0));
            var previewResult = BattleScreenInputHandler.SelectTrainCarSlot(state, 1);

            var result = BattleScreenInputHandler.CancelRedeploy(previewResult.State);

            Assert.That(result.Action, Is.EqualTo(BattleScreenInputAction.CancelRedeploy));
            Assert.That(result.State.CurrentSlotIndex, Is.EqualTo(0));
            Assert.That(result.State.HasRedeployPreview, Is.EqualTo(false));
            Assert.That(result.State.TrainCars[0].IsCurrent, Is.EqualTo(true));
            Assert.That(result.State.SquadMembers[0].CurrentSlotIndex, Is.EqualTo(0));
            Assert.That(result.IsBlockingRunFlow, Is.EqualTo(false));
        }

        [Test]
        public void ConfirmRedeployRejectsStateWithoutPreview()
        {
            var state = BattleScreenRootPresenter.CreateDefault(CreateTrainCars(), CreateSquadMembers(0));

            Assert.Throws<InvalidOperationException>(() => BattleScreenInputHandler.ConfirmRedeploy(state));
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
