using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LastRail.Narrative;

namespace LastRail.Presentation.MissionShell
{
    public interface IStoryArchiveView
    {
        event Action<int> SelectionRequested;
        event Action OpenRequested,BackRequested,RefreshRequested;
        void Render(StoryArchiveViewState state);
    }
    public sealed class StoryArchiveRow
    {
        public string MissionId {get;}
        public string Title {get;}
        public bool CanOpen {get;}
        public StoryArchiveRow(string id,string title,bool canOpen){MissionId=id;Title=title;CanOpen=canOpen;}
    }
    public sealed class StoryArchiveViewState
    {
        public IReadOnlyList<StoryArchiveRow> Rows {get;}
        public int SelectedIndex {get;}
        public StoryArchiveReadStatus Status {get;}
        public string NarrativeVersion {get;}
        public string DetailText {get;}
        public bool IsReading {get;}
        public bool CanOpen=>Status==StoryArchiveReadStatus.Ready&&SelectedIndex>=0&&SelectedIndex<Rows.Count&&Rows[SelectedIndex].CanOpen;
        public StoryArchiveViewState(IReadOnlyList<StoryArchiveRow> rows,int selected,StoryArchiveReadStatus status,string version,string text,bool reading)
        {Rows=rows;SelectedIndex=selected;Status=status;NarrativeVersion=version;DetailText=text;IsReading=reading;}
    }
    // Navigation/selection only. The borrowed reader owns account progress;
    // CodexArchivePresentationState remains the existing text-permission policy.
    public sealed class StoryArchiveController : IDisposable
    {
        readonly StoryArchiveReadModel reader;
        readonly IStoryArchiveView view;
        readonly Action close;
        readonly MissionNarrativeCatalog catalog=MissionNarrativeCatalog.CreateActOne();
        readonly IReadOnlyDictionary<string,string> localized=new Dictionary<string,string>();
        int selected;bool reading,disposed;
        CodexArchivePresentationState archive;
        StoryProgressReadState displayedSource;
        IReadOnlyList<StoryArchiveRow> rows;
        public StoryArchiveController(StoryArchiveReadModel reader,IStoryArchiveView view,Action close)
        {
            this.reader=reader;this.view=view??throw new ArgumentNullException(nameof(view));this.close=close;
            view.SelectionRequested+=Select;view.OpenRequested+=Open;view.BackRequested+=Back;view.RefreshRequested+=Refresh;
            if(reader!=null){reader.SynchronizeSession();reader.Changed+=Changed;}Changed();
        }
        public Task RefreshAsync()=>disposed||reader==null?Task.CompletedTask:reader.RefreshAsync();
        public Task EnsureAsync()=>disposed||reader==null?Task.CompletedTask:reader.EnsureAsync();
        async void Refresh(){await RefreshAsync();}
        void Changed()
        {
            if(disposed)return;
            var source=reader?.DisplayState;
            if(rows==null||!ReferenceEquals(displayedSource,source))
            {
                displayedSource=source;
                archive=source!=null?CodexArchivePresentationState.CreateActOne(source,catalog,localized):null;
                rows=catalog.Definitions.Select((definition,index)=>new StoryArchiveRow(definition.MissionId,definition.DisplayName,archive?.Entries[index].CanOpenForDisplay==true)).ToList().AsReadOnly();
            }
            if(archive==null||!archive.Entries[selected].CanOpenForDisplay)reading=false;
            Render();
        }
        void Select(int index){if(disposed||index<0||index>=catalog.Count)return;selected=index;reading=false;Render();}
        void Open(){if(disposed||reader?.Status!=StoryArchiveReadStatus.Ready||archive==null||!archive.Entries[selected].CanOpenForDisplay)return;reading=true;Render();}
        void Back(){if(disposed)return;if(reading){reading=false;Render();}else close?.Invoke();}
        void Render()
        {
            string text=reading?archive.Entries[selected].CodexEntryText:catalog.Definitions[selected].BriefingSummary;
            view.Render(new StoryArchiveViewState(rows,selected,reader?.Status??StoryArchiveReadStatus.Unavailable,archive?.NarrativeVersion,text,reading));
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;
            if(reader!=null)reader.Changed-=Changed;
            view.SelectionRequested-=Select;view.OpenRequested-=Open;view.BackRequested-=Back;view.RefreshRequested-=Refresh;
            displayedSource=null;archive=null;rows=null;
        }
    }
}
