using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace LastRail.Presentation.MissionShell
{
    // Borrows the home hierarchy; its screen host owns assets and object destruction.
    public sealed class LobbyHomeView
    {
        readonly IReadOnlyList<TMP_Text> balances;readonly TMP_Text notice;readonly LobbySquadPortraits portraits;
        public LobbyHomeView(IReadOnlyList<TMP_Text> balances,TMP_Text notice,LobbySquadPortraits portraits)
        {this.balances=balances;this.notice=notice;this.portraits=portraits;}
        public void Render(LobbyHomeState state)
        {for(int i=0;i<balances.Count;i++)if(balances[i]!=null)balances[i].text=state.Balances[i];if(notice!=null)notice.text=state.Notice;portraits?.Render(state.Operators.ToArray());}
    }
}
