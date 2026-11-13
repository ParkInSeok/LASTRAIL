using System;
using System.Threading.Tasks;

namespace LastRail.Auth
{
    public enum GameApiAuthTokenReadOutcome { Loaded, Missing, Corrupt, Unavailable, ReadError }

    public sealed class GameApiAuthTokenReadResult
    {
        public GameApiAuthTokenReadOutcome Outcome { get; }
        public GameApiAuthSessionSnapshot Session { get; }
        public GameApiAuthTokenReadResult(GameApiAuthTokenReadOutcome outcome, GameApiAuthSessionSnapshot session = null)
        {
            if ((outcome == GameApiAuthTokenReadOutcome.Loaded) != (session != null))
                throw new ArgumentException("Only a loaded token read may contain a session.");
            Outcome = outcome; Session = session;
        }
    }

    public interface IGameApiAuthTokenReader
    {
        Task<GameApiAuthTokenReadResult> ReadAsync();
    }
}
