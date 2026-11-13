using System;
using System.Collections.Generic;
using LastRail.Presentation.Battle;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class BattleScreenInputPortTests
    {
        [Test]
        public void InputPortStoresInitialStateAndLatestResult()
        {
            var state = BattleScreenRootPresenter.CreateDefault(CreateTrainCars(), CreateSquadMembers(0));

            var port = new BattleScreenInputPort(state);

            Assert.That(port.CurrentState, Is.EqualTo(state));
            Assert.That(port.LastResult.Action, Is.EqualTo(BattleScreenInputAction.None));
            Assert.That(port.LastResult.State, Is.EqualTo(state));
            Assert.That(port.LastResult.IsBlockingRunFlow, Is.EqualTo(false));
        }

        [Test]
        public void SelectTrainCarSlotUpdatesCurrentStateToPreview()
        {
            var port = new BattleScreenInputPort(
                BattleScreenRootPresenter.CreateDefault(CreateTrainCars(), CreateSquadMembers(0)));

            var result = port.SelectTrainCarSlot(2);

            Assert.That(result.Action, Is.EqualTo(BattleScreenInputAction.PreviewRedeploy));
            Assert.That(port.CurrentState, Is.EqualTo(result.State));
            Assert.That(port.CurrentState.HasRedeployPreview, Is.EqualTo(true));
            Assert.That(port.CurrentState.RedeployPreview.TargetSlotIndex, Is.EqualTo(2));
        }

        [Test]
        public void ConfirmRedeployUpdatesCurrentStateAndClearsPreview()
        {
            var port = new BattleScreenInputPort(
                BattleScreenRootPresenter.CreateDefault(CreateTrainCars(), CreateSquadMembers(0)));

            port.SelectTrainCarSlot(3);
            var result = port.ConfirmRedeploy();

            Assert.That(result.Action, Is.EqualTo(BattleScreenInputAction.ConfirmRedeploy));
            Assert.That(port.CurrentState.CurrentSlotIndex, Is.EqualTo(3));
            Assert.That(port.CurrentState.HasRedeployPreview, Is.EqualTo(false));
            Assert.That(port.CurrentState.SquadMembers[0].CurrentSlotIndex, Is.EqualTo(3));
        }

        [Test]
        public void CancelRedeployUpdatesCurrentStateAndLeavesSquadInPlace()
        {
            var port = new BattleScreenInputPort(
                BattleScreenRootPresenter.CreateDefault(CreateTrainCars(), CreateSquadMembers(0)));

            port.SelectTrainCarSlot(1);
            var result = port.CancelRedeploy();

            Assert.That(result.Action, Is.EqualTo(BattleScreenInputAction.CancelRedeploy));
            Assert.That(port.CurrentState.CurrentSlotIndex, Is.EqualTo(0));
            Assert.That(port.CurrentState.HasRedeployPreview, Is.EqualTo(false));
            Assert.That(port.CurrentState.SquadMembers[0].CurrentSlotIndex, Is.EqualTo(0));
        }

        [Test]
        public void ReplaceStateResetsLastResultToNoOpForNewState()
        {
            var port = new BattleScreenInputPort(
                BattleScreenRootPresenter.CreateDefault(CreateTrainCars(), CreateSquadMembers(0)));
            var nextState = BattleScreenRootPresenter.CreateDefault(
                TrainCarStripPresenter.MarkCurrent(CreateTrainCars(), 1),
                CreateSquadMembers(1));

            var result = port.ReplaceState(nextState);

            Assert.That(port.CurrentState, Is.EqualTo(nextState));
            Assert.That(result.Action, Is.EqualTo(BattleScreenInputAction.None));
            Assert.That(result.State, Is.EqualTo(nextState));
        }

        [Test]
        public void InputPortRejectsNullInitialState()
        {
            Assert.Throws<ArgumentNullException>(() => new BattleScreenInputPort(null));
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
