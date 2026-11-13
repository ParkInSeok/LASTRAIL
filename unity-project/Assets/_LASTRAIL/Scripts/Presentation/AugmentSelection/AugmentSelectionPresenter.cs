using System;
using System.Threading.Tasks;
using UnityEngine;

namespace LastRail.Presentation.AugmentSelection
{
    // Owns only its subscriptions. The host owns the borrowed controller and View.
    public sealed class AugmentSelectionPresenter:IDisposable
    {
        readonly AugmentSelectionController controller;
        readonly AugmentSelectionView view;
        readonly Func<string,string,AugmentCardPresentation> presentation;
        readonly Func<bool> extraAvailable;
        readonly bool rerollsAvailable;
        bool bound;
        public AugmentSelectionPresenter(AugmentSelectionController controller,AugmentSelectionView view,Func<string,string,AugmentCardPresentation> presentation=null,Func<bool> extraAvailable=null)
            :this(controller,view,presentation,extraAvailable,true){}
        public AugmentSelectionPresenter(AugmentSelectionController controller,AugmentSelectionView view,Func<string,string,AugmentCardPresentation> presentation,Func<bool> extraAvailable,bool rerollsAvailable)
        {this.controller=controller??throw new ArgumentNullException(nameof(controller));this.view=view?view:throw new ArgumentNullException(nameof(view));this.presentation=presentation;this.extraAvailable=extraAvailable;this.rerollsAvailable=rerollsAvailable;}
        public void Bind()
        {
            Unbind();bound=true;controller.Changed+=Render;view.Unavailable+=Unbind;
            view.Bind(i=>Observe(controller.SelectAsync(i)),(i,mode)=>Observe(controller.RerollAsync(i,mode)),()=>Observe(controller.RetryAsync()));Render();
        }
        public void RefreshPresentation()=>Render();
        void Render(){if(bound&&view&&view.isActiveAndEnabled)view.RenderWithCapabilities(controller.Snapshot,controller.Busy,controller.HasPending,controller.Notice,presentation,extraAvailable?.Invoke()==true,rerollsAvailable);}
        async void Observe(Task task)
        {try{await task;}catch(Exception e){Debug.LogError("Augment selection command failed: "+e.GetType().Name);}}
        public void Unbind(){bound=false;controller.Changed-=Render;if(view){view.Unavailable-=Unbind;view.Unbind();}}
        public void Dispose()=>Unbind();
    }
}
