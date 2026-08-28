using System.Threading.Tasks;

namespace LastRail.Auth
{
    public interface IGameApiGuestAuthClient
    {
        Task<GameApiGuestAuthResponseDto> CreateGuestAsync(GameApiGuestAuthRequest request);
    }
}
