namespace LastRail.Auth
{
    public static class GameApiGoogleAuthResponseJsonAdapter
    {
        public static GameApiGoogleAuthResponseDto FromJson(string json)
        {
            return new GameApiGoogleAuthResponseDto(GameApiGuestAuthResponseJsonAdapter.FromJson(json));
        }
    }
}
