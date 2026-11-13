using System;
using System.Collections.Generic;

namespace LastRail.Presentation.MissionShell
{
    public enum OperatorCodexTab { Basic,Skills,Story }
    public sealed class OperatorCodexEntry
    {
        public const string Unknown="아직 확인하지 못한 정보";
        public string Id {get;}
        public string Name {get;}
        public string Active {get;}
        public string Ultimate {get;}
        public string ActiveDescription=>Id=="op_kyle"?"보호막과 도발":Unknown;
        public string UltimateDescription=>Id=="op_kyle"?"보호막":Unknown;
        public OperatorCodexEntry(string id,string name,string active,string ultimate)
        {Id=id;Name=name;Active=active;Ultimate=ultimate;}
    }
    public static class OperatorCodexCatalog
    {
        // Display-only canonical catalogs. No account ownership/discovery/biography inference.
        public static IReadOnlyList<OperatorCodexEntry> Load()
        {
            var rows=new List<OperatorCodexEntry>();
            foreach(var id in OperatorVisualCatalog.Ids)
                rows.Add(new OperatorCodexEntry(id,OperatorVisualCatalog.KoreanLabel(id),LastRail.Battle.CombatSkillNames.Get(id+".active"),LastRail.Battle.CombatSkillNames.Get(id+".ultimate")));
            return rows.AsReadOnly();
        }
    }
    public sealed class OperatorCodexViewState
    {
        public IReadOnlyList<OperatorCodexEntry> Entries {get;}
        public OperatorCodexEntry Selected {get;}
        public OperatorCodexTab Tab {get;}
        public bool IsList {get;}
        // Selected profile snippets have not been approved/exported as runtime biographies.
        public string Body=>Tab==OperatorCodexTab.Basic&&Selected!=null?Selected.Name+"\n\n확인된 정보만 표시됩니다.":OperatorCodexEntry.Unknown;
        public OperatorCodexViewState(IReadOnlyList<OperatorCodexEntry> entries,int index,OperatorCodexTab tab,bool list)
        {Entries=entries;Selected=index>=0&&index<entries.Count?entries[index]:null;Tab=tab;IsList=list;}
    }
    public interface IOperatorCodexView
    {
        event Action<int> SelectionRequested,TabRequested;
        event Action BackRequested;
        void Render(OperatorCodexViewState state);
    }
    public sealed class OperatorCodexController:IDisposable
    {
        readonly IReadOnlyList<OperatorCodexEntry> entries;
        readonly IOperatorCodexView view;readonly Action close;
        int selected;OperatorCodexTab tab=OperatorCodexTab.Skills;bool list,disposed;
        public OperatorCodexController(IReadOnlyList<OperatorCodexEntry> entries,IOperatorCodexView view,Action close)
        {
            if(entries==null)throw new ArgumentNullException(nameof(entries));
            this.entries=new List<OperatorCodexEntry>(entries).AsReadOnly();this.view=view??throw new ArgumentNullException(nameof(view));this.close=close;
            view.SelectionRequested+=Select;view.TabRequested+=SelectTab;view.BackRequested+=Back;Render();
        }
        void Select(int index){if(disposed||index<0||index>=entries.Count)return;selected=index;list=false;Render();}
        void SelectTab(int value){if(disposed||value<0||value>2)return;tab=(OperatorCodexTab)value;Render();}
        void Back(){if(disposed)return;if(!list){list=true;Render();}else close?.Invoke();}
        void Render()=>view.Render(new OperatorCodexViewState(entries,selected,tab,list));
        public void Dispose(){if(disposed)return;disposed=true;view.SelectionRequested-=Select;view.TabRequested-=SelectTab;view.BackRequested-=Back;}
    }
}
