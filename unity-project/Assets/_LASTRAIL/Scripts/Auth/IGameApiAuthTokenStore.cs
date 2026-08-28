using System.Threading.Tasks;

namespace LastRail.Auth
{
    public interface IGameApiAuthTokenStore
    {
        Task<GameApiAuthTokenStoreResult> SaveAsync(GameApiAuthSessionSnapshot value);
        Task<GameApiAuthSessionSnapshot> LoadAsync();
    }
}
