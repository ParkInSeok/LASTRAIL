using LastRail.Tactics;

namespace LastRail.Run
{
    public readonly struct TacticSelectionExport
    {
        public TacticSelectionExport(
            int waveNumber,
            string tacticId,
            TacticKind tacticKind,
            bool draftContainedUltimateTactic)
        {
            WaveNumber = waveNumber;
            TacticId = tacticId;
            TacticKind = tacticKind;
            DraftContainedUltimateTactic = draftContainedUltimateTactic;
        }

        public int WaveNumber { get; }
        public string TacticId { get; }
        public TacticKind TacticKind { get; }
        public bool DraftContainedUltimateTactic { get; }
    }
}
