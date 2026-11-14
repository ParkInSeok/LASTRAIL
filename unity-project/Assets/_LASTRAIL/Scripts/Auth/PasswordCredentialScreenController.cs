using System;
using System.Threading.Tasks;

namespace LastRail.Auth
{
    // Screen-only consent/confirmation flow; PasswordCredentialFlow keeps account/session authority.
    public sealed class PasswordCredentialScreenController:IDisposable
    {
        readonly PasswordCredentialFlow model;
        Action render,clearFields,close;
        char[] registrationPassword;
        bool active,disposed,closing;
        int generation;
        public string ValidationMessage {get;private set;}
        public bool ActionFailed {get;private set;}
        public bool HasRegistrationDraft=>registrationPassword!=null;
        public PasswordCredentialScreenController(PasswordCredentialFlow model,Action render,Action clearFields,Action close)
        {this.model=model??throw new ArgumentNullException(nameof(model));this.render=render;this.clearFields=clearFields;this.close=close;}
        bool Available=>active&&!disposed;
        bool Current(int stamp)=>Available&&stamp==generation;
        public void Activate(){if(active||disposed)return;active=true;generation++;model.Changed+=Refresh;Refresh();}
        public void Deactivate(){if(!active)return;active=false;generation++;model.Changed-=Refresh;ClearSecretDraft();}
        public void Refresh(){if(!Available)return;if(model.ProofExpired)ClearSecretDraft();render?.Invoke();}
        public void ShowNotice(string message){if(!Available)return;ValidationMessage=message;Refresh();}
        public void ClearSecretDraft(){if(registrationPassword!=null)Array.Clear(registrationPassword,0,registrationPassword.Length);registrationPassword=null;}
        public async Task SendCodeAsync(string id,string email,string password,string confirmation,bool terms,bool privacy)
        {
            if(!Available||model.Busy||!model.CanStart)return;
            if(model.Mode=="Register"&&model.Stage==PasswordCredentialStage.Editing){
                if(password.Length<8||password.Length>128||password!=confirmation){ShowNotice("비밀번호 8~128자와 비밀번호 확인을 확인해 주세요.");return;}
                if(!terms||!privacy){ShowNotice("이용약관과 개인정보 처리방침을 확인해 주세요.");return;}
                ClearSecretDraft();registrationPassword=password.ToCharArray();
            }
            int stamp=generation;clearFields?.Invoke();ActionFailed=false;ValidationMessage=null;
            try{await model.StartAsync(id,email);}catch{if(Current(stamp))ActionFailed=true;}
            if(!Current(stamp))return;if(ActionFailed||!string.IsNullOrEmpty(model.ErrorCode))ClearSecretDraft();Refresh();
        }
        public async Task ConfirmCodeAsync(string code)
        {
            if(!Available||model.Busy||!model.CanConfirm)return;int stamp=generation;clearFields?.Invoke();ActionFailed=false;ValidationMessage=null;
            try{await model.ConfirmAsync(code);}catch{if(Current(stamp))ActionFailed=true;}finally{code=null;}
            if(!Current(stamp))return;if(ActionFailed||model.Stage!=PasswordCredentialStage.Verified)ClearSecretDraft();Refresh();
        }
        public async Task CompleteAsync(string password,string confirmation,string code,bool terms,bool privacy)
        {
            if(!Available||model.Busy||!model.CanComplete)return;
            if(password!=confirmation){ShowNotice("비밀번호와 비밀번호 확인이 일치하지 않습니다.");return;}
            if(model.Mode!="Reset"&&(!terms||!privacy)){ShowNotice("이용약관과 개인정보 처리방침을 확인해 주세요.");return;}
            var secret=password.Length==0&&registrationPassword!=null?new string(registrationPassword):password;
            int stamp=generation;clearFields?.Invoke();ClearSecretDraft();ActionFailed=false;ValidationMessage=null;
            try{await model.CompleteAsync(secret,model.Mode=="Reset"?code:null);}catch{if(Current(stamp))ActionFailed=true;}finally{secret=null;code=null;}
            if(Current(stamp))Refresh();
        }
        public async Task ReconcileAsync(string secret)
        {
            if(!Available||model.Busy||!model.CanReconcile)return;int stamp=generation;clearFields?.Invoke();ClearSecretDraft();ActionFailed=false;ValidationMessage=null;
            try{await model.ReconcileAsync(secret);}catch{if(Current(stamp))ActionFailed=true;}finally{secret=null;}if(Current(stamp))Refresh();
        }
        public void Cancel()
        {
            if(!Available||closing)return;closing=true;clearFields?.Invoke();ClearSecretDraft();
            try{model.Cancel();}catch{ActionFailed=true;}finally{try{close?.Invoke();}finally{closing=false;}}
        }
        public void Dispose(){if(disposed)return;Deactivate();ClearSecretDraft();disposed=true;render=null;clearFields=null;close=null;}
    }
}
