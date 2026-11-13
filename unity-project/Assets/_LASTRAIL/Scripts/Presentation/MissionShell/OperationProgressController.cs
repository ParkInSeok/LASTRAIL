using System;
using System.Collections.Generic;
using LastRail.Narrative;

namespace LastRail.Presentation.MissionShell
{
    // Borrows the account progress owner. Produces display rows only; START authority
    // and resource preparation remain with their existing owners.
    public sealed class OperationProgressController : IDisposable
    {
        readonly StoryArchiveReadModel owner;
        Action<IReadOnlyList<MissionShellMissionRowState>, bool> render;
        readonly MissionNarrativeCatalog catalog=MissionNarrativeCatalog.CreateActOne();
        StoryProgressReadState displayed;
        IReadOnlyList<MissionShellMissionRowState> rows;
        public OperationProgressController(StoryArchiveReadModel owner,
            Action<IReadOnlyList<MissionShellMissionRowState>, bool> render)
        {
            this.owner=owner??throw new ArgumentNullException(nameof(owner));
            this.render=render??throw new ArgumentNullException(nameof(render));
            owner.SynchronizeSession();owner.Changed+=Changed;Changed();
        }
        void Changed()
        {
            if(render==null)return;
            var source=owner.DisplayState;
            if(rows==null||!ReferenceEquals(displayed,source))
            {
                displayed=source;
                rows=MissionShellScreenPresenter.BuildMissionRows(displayed,catalog);
            }
            render(rows,owner.Status==StoryArchiveReadStatus.Ready);
        }
        public void Dispose()
        {
            owner.Changed-=Changed;render=null;displayed=null;rows=null;
        }
    }
}
