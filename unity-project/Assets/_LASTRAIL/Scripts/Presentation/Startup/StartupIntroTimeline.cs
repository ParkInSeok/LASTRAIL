namespace LastRail.Presentation.Startup
{
    // One launch, one journey. Version admission remains owned by DownloadCoordinator.
    public sealed class StartupIntroTimeline
    {
        public const float TravelSeconds = 8, BloomSeconds = 1.25f, RevealSeconds = 1.4f;
        public const float BloomStartSeconds = TravelSeconds - BloomSeconds;
        public float Travel { get; private set; }
        public float Transition { get; private set; }
        public bool Revealed { get; private set; }
        public bool Finished => Revealed && Transition >= BloomSeconds + RevealSeconds;
        public StartupIntroTimeline(bool alreadyPlayed = false)
        {
            Travel = alreadyPlayed ? TravelSeconds : 0;
            Transition = alreadyPlayed ? BloomSeconds : 0;
        }
        // Elapsed-time helper; the real video path below supplies decoded media time.
        public bool Advance(float delta, bool versionAccepted)
        {
            delta = System.Math.Max(0, delta);
            return ObservePlayback(Travel + delta, Travel + delta >= TravelSeconds, delta, versionAccepted);
        }
        public bool ObservePlayback(float mediaTime, bool mediaEnded, float delta, bool versionAccepted)
        {
            if (Finished) return false;
            if (Revealed)
            {
                Transition = System.Math.Min(BloomSeconds + RevealSeconds, Transition + System.Math.Max(0, delta));
                return false;
            }
            if (!float.IsNaN(mediaTime) && !float.IsInfinity(mediaTime))
                Travel = System.Math.Max(Travel, System.Math.Min(TravelSeconds, System.Math.Max(0, mediaTime)));
            if (mediaEnded) Travel = TravelSeconds;
            Transition = System.Math.Min(BloomSeconds, System.Math.Max(0, Travel - BloomStartSeconds));
            // Decoder preparation/stalls never advance the transition. A white peak
            // can wait indefinitely for version admission without restarting media.
            if (mediaEnded && versionAccepted) { Revealed = true; return true; }
            return false;
        }
    }
}
