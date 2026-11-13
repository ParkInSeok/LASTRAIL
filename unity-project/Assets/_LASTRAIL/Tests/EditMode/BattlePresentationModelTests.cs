using System;
using LastRail.Presentation.Battle;
using NUnit.Framework;

namespace LastRail.Tests.EditMode
{
    public sealed class BattlePresentationModelTests
    {
        [Test]
        public void PortraitDefaultLayout_SeparatesExternalBattleAndCurrentTrainCarInterior()
        {
            var layout = BattleScreenLayoutModel.CreatePortraitDefault();

            Assert.That(layout.ExternalBattleHeightRatio, Is.EqualTo(0.55f));
            Assert.That(layout.CurrentTrainCarInteriorHeightRatio, Is.EqualTo(0.45f));
            Assert.That(layout.TotalHeightRatio, Is.EqualTo(1f));
            Assert.That(layout.ExternalBattleRegion.Name, Is.EqualTo("externalBattle"));
            Assert.That(layout.CurrentTrainCarInteriorRegion.Name, Is.EqualTo("currentTrainCarInterior"));
            Assert.That(layout.ExternalBattleRegion.Order, Is.EqualTo(0));
            Assert.That(layout.CurrentTrainCarInteriorRegion.Order, Is.EqualTo(1));
        }

        [Test]
        public void Layout_RejectsRatiosThatDoNotFillPortraitScreen()
        {
            Assert.Throws<ArgumentException>(() => new BattleScreenLayoutModel(0.2f, 0.2f));
            Assert.Throws<ArgumentException>(() => new BattleScreenLayoutModel(0f, 1f));
            Assert.Throws<ArgumentException>(() => new BattleScreenLayoutModel(1f, 0f));
        }

        [Test]
        public void TrainCarAndSquadViewStates_KeepStableIdsAndDisplayLabelsSeparate()
        {
            var trainCar = new TrainCarViewState(1, "car_gun", "포격칸", true);
            var squadMember = new SquadMemberViewState("op_reina", "레이나", 1);

            Assert.That(trainCar.SlotIndex, Is.EqualTo(1));
            Assert.That(trainCar.TrainCarId, Is.EqualTo("car_gun"));
            Assert.That(trainCar.DisplayLabel, Is.EqualTo("포격칸"));
            Assert.That(trainCar.IsCurrent, Is.EqualTo(true));
            Assert.That(squadMember.OperatorId, Is.EqualTo("op_reina"));
            Assert.That(squadMember.DisplayLabel, Is.EqualTo("레이나"));
            Assert.That(squadMember.CurrentSlotIndex, Is.EqualTo(1));
        }

        [Test]
        public void SquadRedeployPreview_MovesAllFiveMembersToTargetTrainCarSlot()
        {
            var preview = SquadRedeployPreview.Create(
                currentSlotIndex: 0,
                targetSlotIndex: 2,
                trainCars: TrainCars(),
                squadMembers: SquadMembers(0));

            Assert.That(preview.CurrentSlotIndex, Is.EqualTo(0));
            Assert.That(preview.TargetSlotIndex, Is.EqualTo(2));
            Assert.That(preview.DestinationTrainCarId, Is.EqualTo("car_cargo"));
            Assert.That(preview.IsNoOp, Is.EqualTo(false));
            Assert.That(preview.PreviewMembers.Count, Is.EqualTo(5));
            Assert.That(preview.PreviewMembers[0].OperatorId, Is.EqualTo("op_kyle"));
            Assert.That(preview.PreviewMembers[0].CurrentSlotIndex, Is.EqualTo(2));
            Assert.That(preview.AffectedOperatorIds.Count, Is.EqualTo(5));
            Assert.That(preview.AffectedOperatorIds[4], Is.EqualTo("op_mira"));
        }

        [Test]
        public void SquadRedeployPreview_RequiresWholeFivePersonSquadInOneCurrentSlot()
        {
            Assert.Throws<ArgumentException>(() => SquadRedeployPreview.Create(0, 1, TrainCars(), new[]
            {
                new SquadMemberViewState("op_kyle", "카일", 0)
            }));

            Assert.Throws<ArgumentException>(() => SquadRedeployPreview.Create(0, 1, TrainCars(), new[]
            {
                new SquadMemberViewState("op_kyle", "카일", 0),
                new SquadMemberViewState("op_reina", "레이나", 0),
                new SquadMemberViewState("op_eve", "이브", 0),
                new SquadMemberViewState("op_sera", "세라", 1),
                new SquadMemberViewState("op_mira", "미라", 0)
            }));
        }

        [Test]
        public void SquadRedeployPreview_RejectsUnknownTargetTrainCarSlot()
        {
            Assert.Throws<ArgumentException>(() => SquadRedeployPreview.Create(0, 4, TrainCars(), SquadMembers(0)));
        }

        private static TrainCarViewState[] TrainCars()
        {
            return new[]
            {
                new TrainCarViewState(0, "car_combat", "전투칸", true),
                new TrainCarViewState(1, "car_gun", "포격칸", false),
                new TrainCarViewState(2, "car_cargo", "화물칸", false)
            };
        }

        private static SquadMemberViewState[] SquadMembers(int slotIndex)
        {
            return new[]
            {
                new SquadMemberViewState("op_kyle", "카일", slotIndex),
                new SquadMemberViewState("op_reina", "레이나", slotIndex),
                new SquadMemberViewState("op_eve", "이브", slotIndex),
                new SquadMemberViewState("op_sera", "세라", slotIndex),
                new SquadMemberViewState("op_mira", "미라", slotIndex)
            };
        }
    }
}
