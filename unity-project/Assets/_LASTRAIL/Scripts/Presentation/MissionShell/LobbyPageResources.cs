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
            ReleasePageBinding();renderBody=null;
            CancelPageResources();
            ReleaseDetailInput();ReleaseEquipmentMultishot();
            if(content!=null){content.gameObject.SetActive(false);if(Application.isPlaying)Destroy(content.gameObject);else DestroyImmediate(content.gameObject);content=null;}
            ReleaseSkins();art=null;if(backdrop!=null)backdrop.texture=null;
            pageResources?.Dispose();pageResources=null;
        }
        async Task OpenPreparedPageAsync(LobbyPage page,bool editorFixture)
        {
            CancelPageResources();
            var request=pageResourceRequest=new CancellationTokenSource();var owner=accountState;var session=owner?.CaptureScreenSession()??(()=>true);
            PreparedContent.Scope acquired=null;
            try
            {
                string key="DestinationPages/"+Keys[(int)page];
                var selectors=page==LobbyPage.Train?new[]{"TrainFortress/","EntryUi/primary"}:page==LobbyPage.Formation&&!editorFixture?new[]{"EntryUi/primary","FormationPlan/","NoncombatCodex/codex-dark-steel-surface-v01"}:new[]{key+"-atlas",key+"-background","EntryUi/primary"};
                if(page==LobbyPage.Operators&&!editorFixture)selectors=selectors.Concat(new[]{"FormationPlan/portrait-reina","FormationPlan/portrait-kyle","FormationPlan/portrait-eve","FormationPlan/portrait-sera","FormationPlan/portrait-nyx"}).ToArray();
                if((page==LobbyPage.Formation||page==LobbyPage.Operators)&&!editorFixture&&owner?.Account?.Operators.Any(o=>o.Text("operatorId")=="op_mira")==true)selectors=selectors.Concat(new[]{"OperatorPortraits/mira"}).ToArray();
                acquired=await PreparedContent.PrepareAsync(selectors,request.Token);
                if(this==null||request.IsCancellationRequested||pageResourceRequest!=request||owner!=accountState||!session()||hostActive!=null&&!hostActive())return;
                pageResourceRequest=null;ReleasePageResources();pageResources=acquired;acquired=null;
                OpenPreparedPage(page,editorFixture);
            }
            catch(OperationCanceledException){}
            catch(Exception error){Debug.LogException(error);if(this!=null&&!request.IsCancellationRequested&&pageResourceRequest==request&&owner==accountState&&session())Close();}
            finally{if(pageResourceRequest==request)pageResourceRequest=null;acquired?.Dispose();request.Dispose();}
        }
    }
}
