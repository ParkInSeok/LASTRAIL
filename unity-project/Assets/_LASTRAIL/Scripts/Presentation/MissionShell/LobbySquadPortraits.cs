using System;
using System.Threading;
using LastRail.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // The visible lobby owns portrait leases; pages acquire their own scope.
    public sealed class LobbySquadPortraits : MonoBehaviour
    {
        RawImage[] images;
        GameObject[] empty;
        string[] ids = new string[5];
        PreparedContent.Scope scope;
        CancellationTokenSource request;
        public void Bind(RawImage[] portraits, GameObject[] plus)
        {
            images=portraits;empty=plus;
            if(isActiveAndEnabled)Load();
        }
        public void Render(string[] selection)
        {
            if(selection==null||selection.Length!=5)throw new ArgumentException("Five slots required");
            ids=(string[])selection.Clone();Apply();
        }
        void Apply()
        {
            if(images==null)return;
            for(int i=0;i<5;i++){
                var texture=scope==null?null:FormationPlanView.Portrait(ids[i]);
                images[i].texture=texture;images[i].enabled=texture!=null;images[i].color=Color.white;
                if(texture!=null)images[i].uvRect=FormationPlanView.PortraitCrop(ids[i],texture);
                empty[i].SetActive(string.IsNullOrEmpty(ids[i]));
            }
        }
        async void Load()
        {
            Release();var current=request=new CancellationTokenSource();PreparedContent.Scope acquired=null;
            try{
                acquired=await PreparedContent.PrepareAsync(new[]{"FormationPlan/portrait-reina","FormationPlan/portrait-kyle","FormationPlan/portrait-eve","FormationPlan/portrait-sera","FormationPlan/portrait-nyx","OperatorPortraits/","NumberedFormation/"},current.Token);
                if(this==null||!isActiveAndEnabled||request!=current||current.IsCancellationRequested)return;
                scope=acquired;acquired=null;Apply();
            }
            catch(OperationCanceledException){}
            catch(Exception error){Debug.LogException(error);}
            finally{acquired?.Dispose();if(request==current)request=null;current.Dispose();}
        }
        void Release()
        {
            var pending=request;request=null;pending?.Cancel();
            if(images!=null)foreach(var image in images)if(image!=null){image.texture=null;image.enabled=false;}
            scope?.Dispose();scope=null;
        }
        void OnEnable(){if(images!=null)Load();}
        void OnDisable()=>Release();
        void OnDestroy(){Release();images=null;empty=null;ids=null;}
    }
}
