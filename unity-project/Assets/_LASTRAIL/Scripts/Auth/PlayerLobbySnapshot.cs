using LastRail.Narrative;

namespace LastRail.Auth
{
    public sealed class PlayerLobbySnapshot
    {
        public PlayerEntryProfile Profile { get; }
        public StoryProgressReadState Story { get; }
        public MissionNarrativeCatalog Catalog { get; }
        public long Generation { get; }
        public PlayerLobbySnapshot(PlayerEntryProfile profile, StoryProgressReadState story, MissionNarrativeCatalog catalog, long generation)
        { Profile = profile; Story = story; Catalog = catalog; Generation = generation; }
    }
}
