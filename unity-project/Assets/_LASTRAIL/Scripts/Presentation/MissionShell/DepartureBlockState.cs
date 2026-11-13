using System;
using System.Collections.Generic;

namespace LastRail.Presentation.MissionShell
{
    [Flags] public enum DepartureBlockFlags
    {
        None=0, Outcome=1, Authentication=2, Storage=4, Unsupported=8,
        Formation=16, Progress=32, Assets=64, Train=128, Ultimate=256,
        Account=512, Quote=1024, Fuel=2048, Support=4096
    }
    public enum DepartureBlockAction { None, Outcome, Account, Storage, Formation, Operations, Assets, Train, Preview, Refresh, Support }
    public sealed class DepartureBlockReason
    {
        public string Code {get;} public string Message {get;} public string ActionLabel {get;}
        public DepartureBlockAction Action {get;}
        internal DepartureBlockReason(string code,string message,string label,DepartureBlockAction action)
        {Code=code;Message=message;ActionLabel=label;Action=action;}
    }
    // Immutable display projection. Flags are observations of existing owners, not permission to start.
    public sealed class DepartureBlockState
    {
        public int Flags {get;} public IReadOnlyList<DepartureBlockReason> Reasons {get;}
        public string PreparationNotice {get;}
        public string SelectionNotice {get;}
        public bool HasBlocks=>Reasons.Count!=0;
        public string PrimaryNotice=>HasBlocks?Reasons[0].Message:null;
        DepartureBlockState(int flags,List<DepartureBlockReason> reasons,string detail,string selection){Flags=flags;Reasons=reasons.AsReadOnly();PreparationNotice=detail;SelectionNotice=selection;}
        public static DepartureBlockState FromFlags(int value)=>FromObserved(value,null);
        public static DepartureBlockState FromObserved(int value,string preparationNotice,string selectionNotice=null)
        {
            if(value<0||(value&~8191)!=0)throw new ArgumentOutOfRangeException(nameof(value));
            var flags=(DepartureBlockFlags)value;var reasons=new List<DepartureBlockReason>();
            void Add(DepartureBlockFlags bit,string code,string message,string label,DepartureBlockAction action){if((flags&bit)!=0)reasons.Add(new DepartureBlockReason(code,message,label,action));}
            Add(DepartureBlockFlags.Outcome,"outcome","진행 중인 요청이나 작전 기록을 먼저 확인해 주세요.","원래 요청·기록 확인",DepartureBlockAction.Outcome);
            Add(DepartureBlockFlags.Authentication,"authentication","로그인 상태를 확인해 주세요.","계정 확인",DepartureBlockAction.Account);
            Add(DepartureBlockFlags.Storage,"storage","확정된 출격 기록을 저장하지 못했습니다. 원래 기록은 보존됩니다.","저장 다시 시도",DepartureBlockAction.Storage);
            Add(DepartureBlockFlags.Unsupported,"unsupported","현재 버전에서는 출격할 수 없습니다.","지원 버전 필요",DepartureBlockAction.None);
            Add(DepartureBlockFlags.Formation,"formation","대원 5명의 편성을 저장해 주세요.","대원 편성",DepartureBlockAction.Formation);
            Add(DepartureBlockFlags.Progress,"progress","작전 진행 조건을 확인해 주세요.","작전 확인",DepartureBlockAction.Operations);
            Add(DepartureBlockFlags.Train,"train","열차 배치 또는 편성 조건을 확인해 주세요.","열차 편성",DepartureBlockAction.Train);
            Add(DepartureBlockFlags.Ultimate,"ultimate","현재 편성의 궁극기 선택 정보를 확인해 주세요.","대원 편성",DepartureBlockAction.Formation);
            Add(DepartureBlockFlags.Support,"support",preparationNotice??"지휘관 스킬 3개를 선택하고 저장해 주세요.","지원 스킬 변경",DepartureBlockAction.Support);
            Add(DepartureBlockFlags.Account,"account","계정 정보를 확인해야 합니다.","정보 다시 확인",DepartureBlockAction.Refresh);
            Add(DepartureBlockFlags.Quote,"quote",selectionNotice??"현재 선택의 출격 비용을 확인해 주세요.","출격 준비 확인",DepartureBlockAction.Preview);
            Add(DepartureBlockFlags.Fuel,"fuel","출격에 필요한 연료가 부족합니다.","재화 확인",DepartureBlockAction.Refresh);
            Add(DepartureBlockFlags.Assets,"assets",preparationNotice??"전투 자료 준비 또는 전투 연결 확인이 필요합니다.","전투 자료 다시 준비",DepartureBlockAction.Assets);
            return new DepartureBlockState(value,reasons,preparationNotice,selectionNotice);
        }
    }
}
