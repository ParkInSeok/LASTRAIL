using UnityEngine;
using UnityEngine.UI;
using LastRail.Resources;

namespace LastRail.Presentation.Startup
{
    // Static approved artwork; bounded arrival choreography drives Evaluate explicitly.
    public sealed class StartupMotionBackdrop : MonoBehaviour
    {
        public bool ReducedMotion { get; set; }
        public bool IsLogin { get; private set; }
        public bool Authenticated { get; private set; }
        public int LayerCount { get; private set; }
        RectTransform art;
        RawImage background;
        float mapFocus,mapDive;
        bool resourceLoading;
        public void SetMapDive(float value){mapDive=Mathf.Clamp01(value);Evaluate(0);}
        public void SetMapFocus(float value){mapFocus=Mathf.Clamp01(value);Evaluate(0);}
        public void SetResourceLoading(bool value){resourceLoading=value;Evaluate(0);}
        public static StartupMotionBackdrop Attach(Transform parent,bool login)
        {
            var existing=parent.GetComponentInChildren<StartupMotionBackdrop>(true);
            if(existing!=null){existing.Show(login,false);return existing;}
            var root=new GameObject("StartupMotion",typeof(RectTransform));root.transform.SetParent(parent,false);root.transform.SetAsFirstSibling();
            var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
            var view=root.AddComponent<StartupMotionBackdrop>();view.Show(login,false);return view;
        }
        public void Show(bool login,bool authenticated)
        {
            if(art==null||IsLogin!=login)
            {
                if(art!=null){art.gameObject.SetActive(false);if(Application.isPlaying)Destroy(art.gameObject);else DestroyImmediate(art.gameObject);}
                IsLogin=login;LayerCount=1;mapFocus=mapDive=0;
                art=new GameObject("Artboard",typeof(RectTransform)).GetComponent<RectTransform>();art.SetParent(transform,false);
                art.anchorMin=Vector2.zero;art.anchorMax=Vector2.one;art.pivot=new Vector2(.5f,.5f);art.offsetMin=art.offsetMax=Vector2.zero;
                background=new GameObject(login?"LoginBackdrop":"FlowFirstFrame",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();background.transform.SetParent(art,false);
                var rect=background.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;background.raycastTarget=false;
                var target=background;
                if(login)target.gameObject.AddComponent<PublicEntryTexture>().Bind("login-background",texture=>{if(target!=null){target.texture=texture;Evaluate(0);}});
                else background.texture=UnityEngine.Resources.Load<Texture2D>("StartupVideo/landscape-first-frame-v01");
                var fit=background.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=1672f/941;
            }
            Authenticated=authenticated;Evaluate(0);
        }
        public void CopyPhaseFrom(StartupMotionBackdrop previous){if(previous!=null){ReducedMotion=previous.ReducedMotion;resourceLoading=previous.resourceLoading;}Evaluate(0);}
        void OnRectTransformDimensionsChange()=>Evaluate(0);
        public void Evaluate(float delta)
        {
            if(art==null)return;
            float focus=ReducedMotion?0:mapFocus,dive=ReducedMotion?0:mapDive;
            art.localScale=Vector3.one*Mathf.Pow(1.25f,focus)*Mathf.Pow(2,dive);
            art.anchoredPosition=Vector2.zero;
            if(background!=null)background.color=background.texture==null?new Color(.03f,.04f,.05f):resourceLoading?new Color(.96f,.96f,.96f):Color.white;
        }
    }
}
