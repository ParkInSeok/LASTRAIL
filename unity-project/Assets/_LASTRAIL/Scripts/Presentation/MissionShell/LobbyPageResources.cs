using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LastRail.Resources;
using UnityEngine;

namespace LastRail.Presentation.MissionShell
{
    public sealed partial class LobbyDestinationPages
    {
        CancellationTokenSource pageResourceRequest;
        PreparedContent.Scope pageResources;
        void CancelPageResources(){var request=pageResourceRequest;pageResourceRequest=null;request?.Cancel();}
        void ReleasePageResources()
        {
            CancelPageResources();
            ReleaseDetailInput();ReleaseEquipmentMultishot();
            if(content!=null){content.gameObject.SetActive(false);if(Application.isPlaying)Destroy(content.gameObject);else DestroyImmediate(content.gameObject);content=null;}
            ReleaseSkins();art=null;if(backdrop!=null)backdrop.texture=null;
            pageResources?.Dispose();pageResources=null;
        }
        async Task OpenPreparedPageAsync(LobbyPage page,bool editorFixture)
        {
            CancelPageResources();
            var request=pageResourceRequest=new CancellationTokenSource();var owner=accountState;
            PreparedContent.Scope acquired=null;
            try
            {
                string key="DestinationPages/"+Keys[(int)page];
                var selectors=page==LobbyPage.Formation&&!editorFixture?new[]{"EntryUi/primary","FormationPlan/","NoncombatCodex/codex-dark-steel-surface-v01"}:new[]{key+"-atlas",key+"-background","EntryUi/primary"};
                if(page==LobbyPage.Formation&&!editorFixture&&owner?.Account?.Operators.Any(o=>o.Text("operatorId")=="op_mira")==true)selectors=selectors.Concat(new[]{"OperatorPortraits/mira"}).ToArray();
                acquired=await PreparedContent.PrepareAsync(selectors,request.Token);
                if(this==null||request.IsCancellationRequested||pageResourceRequest!=request||owner!=accountState||backOwner!=null&&!backOwner.isActiveAndEnabled)return;
                pageResourceRequest=null;ReleasePageResources();pageResources=acquired;acquired=null;
                OpenPreparedPage(page,editorFixture);
            }
            catch(OperationCanceledException){}
            catch(Exception error){Debug.LogException(error);if(this!=null&&!request.IsCancellationRequested)Close();}
            finally{if(pageResourceRequest==request)pageResourceRequest=null;acquired?.Dispose();request.Dispose();}
        }
    }
}
