namespace LastRail.Narrative
{
    public sealed class StoryProgressReadRequest
    {
        private StoryProgressReadRequest()
        {
        }

        public string Method => "GET";
        public string EndpointPath => "/v1/story-progress";
        public string BodyJson => null;
        public bool HasAccountShortcut => false;

        public static StoryProgressReadRequest Create()
        {
            return new StoryProgressReadRequest();
        }
    }
}
