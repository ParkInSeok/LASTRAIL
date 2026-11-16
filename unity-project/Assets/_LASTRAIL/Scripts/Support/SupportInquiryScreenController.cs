using System;
using System.Threading.Tasks;
using LastRail.Submissions;

namespace LastRail.Support
{
    // Borrows the account inquiry/recovery owners; owns only this screen's confirmation and lifetime.
    public sealed class SupportInquiryScreenController : IDisposable
    {
        readonly SupportInquiryController model;
        Action render;
        SupportReceiptRecovery recovery;
        bool active,disposed;
        int generation;
        public PendingReceipt DeleteTarget {get;private set;}
        public bool RecoveryActionFailed {get;private set;}
        public bool RecoveryBusy=>recovery!=null&&(recovery.Busy||model.State==InquiryState.Sending);
        public bool CapacityBlocksSend=>recovery!=null&&recovery.Records.Count>=64&&model.Pending==null;
        bool Current(int stamp)=>active&&!disposed&&stamp==generation;
        bool Available=>active&&!disposed;
        bool RecoveryBlocked=>recovery!=null&&(RecoveryBusy||recovery.StorageFailed||(recovery.Selected?.Kind==PendingReceiptKind.ClientError&&!recovery.ManagingDraft));
        public SupportInquiryScreenController(SupportInquiryController model,Action render)
        {this.model=model??throw new ArgumentNullException(nameof(model));this.render=render;}
        public void ConfigureRecovery(SupportReceiptRecovery value)
        {
            if(disposed)return;if(value==null)throw new ArgumentNullException(nameof(value));
            if(active&&recovery!=null)recovery.Changed-=Changed;
            generation++;recovery=value;DeleteTarget=null;RecoveryActionFailed=false;
            if(active)recovery.Changed+=Changed;
        }
        public void Activate(){if(active||disposed)return;active=true;generation++;model.Changed+=Changed;if(recovery!=null)recovery.Changed+=Changed;Changed();}
        public void Deactivate(){if(!active)return;active=false;generation++;DeleteTarget=null;model.Changed-=Changed;if(recovery!=null)recovery.Changed-=Changed;}
        void Changed()
        {
            if(!Available)return;
            if(recovery!=null&&(RecoveryBusy||recovery.StorageFailed||!SameRecord(DeleteTarget,recovery.Selected)))DeleteTarget=null;
            render?.Invoke();
        }
        public void SelectReceipt(PendingReceipt record)
        {if(!Available||recovery==null)return;if(RecoveryBusy||recovery.StorageFailed){Changed();return;}DeleteTarget=null;RecoveryActionFailed=false;recovery.Select(record);Changed();}
        public async Task RefreshReceiptsAsync()
        {
            if(!Available||recovery==null||RecoveryBusy)return;int stamp=generation;var owner=recovery;DeleteTarget=null;RecoveryActionFailed=false;
            try{await owner.RefreshAsync();}catch{if(Current(stamp))RecoveryActionFailed=true;}if(Current(stamp))Changed();
        }
        public void StartIndependentInquiry()
        {if(!Available||recovery==null||RecoveryBusy||recovery.StorageFailed)return;DeleteTarget=null;RecoveryActionFailed=false;recovery.StartIndependentInquiry();Changed();}
        public void BeginForget(){if(!Available||recovery==null||RecoveryBusy||recovery.StorageFailed||recovery.Selected==null)return;DeleteTarget=recovery.Selected;Changed();}
        public void CancelForget(){if(!Available)return;DeleteTarget=null;Changed();}
        public async Task ConfirmForgetAsync()
        {
            if(!Available||recovery==null||RecoveryBusy||recovery.StorageFailed||DeleteTarget==null||!SameRecord(DeleteTarget,recovery.Selected))return;
            int stamp=generation;var owner=recovery;DeleteTarget=null;RecoveryActionFailed=false;
            try{await owner.ForgetSelectedAsync(true);}catch{if(Current(stamp))RecoveryActionFailed=true;}if(Current(stamp))Changed();
        }
        public void Edit(int index,string content){if(!Available||RecoveryBlocked||index<0||index>=model.Categories.Count)return;model.Edit(model.Categories[index],content);}
        public async Task SubmitAsync()
        {
            if(!Available||RecoveryBlocked||CapacityBlocksSend)return;if(model.State==InquiryState.Received){model.StartAnother();return;}
            int stamp=generation;try{await model.SubmitAsync();}catch{if(Current(stamp))Changed();}
        }
        public async Task ReconcileAsync()
        {
            if(!Available||recovery!=null&&(RecoveryBusy||recovery.StorageFailed||recovery.ManagingDraft||recovery.Selected?.Kind==PendingReceiptKind.ClientError))return;
            int stamp=generation;try{await model.ReconcileAsync();}catch{if(Current(stamp))Changed();}
        }
        public static bool SameRecord(PendingReceipt first,PendingReceipt second)=>first!=null&&second!=null&&first.Kind==second.Kind&&first.AccountId==second.AccountId&&first.IdempotencyKey==second.IdempotencyKey;
        public void Dispose(){if(disposed)return;Deactivate();disposed=true;render=null;recovery=null;}
    }
}
