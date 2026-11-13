namespace LastRail.OfflineRewards
{
    public sealed class OfflineRewardPreviewRequest
    {
        private OfflineRewardPreviewRequest()
        {
        }

        public string Method => "GET";
        public string EndpointPath => "/v1/offline-rewards/preview";
        public string BodyJson => null;
        public bool HasQueryParameters => false;
        public bool HasAccountShortcut => false;

        public static OfflineRewardPreviewRequest Create()
        {
            return new OfflineRewardPreviewRequest();
        }
    }
}
