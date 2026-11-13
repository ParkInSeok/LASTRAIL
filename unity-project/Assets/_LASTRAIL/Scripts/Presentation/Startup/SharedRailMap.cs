using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.Startup
{
    // One texture, railway and landmark coordinate system for cabin and lobby.
    public sealed class SharedRailMap : MonoBehaviour
    {
        public const float ArrivalScale=14.2f;
        public static readonly Vector2 Landmark=new Vector2(.51f,.52f);
        public RawImage Map {get;private set;}
        public RawImage Route {get;private set;}
        RawImage node;
        Material routeMaterial,nodeMaterial;
        bool lobby;
        float settle;
        public static SharedRailMap Create(Transform parent)
        {
            var go=new GameObject("SharedRailMap",typeof(RectTransform)); go.transform.SetParent(parent,false);
            var view=go.AddComponent<SharedRailMap>();
            view.Map=view.Image("login-map-base-v01",LastRail.Resources.PreparedContent.Load<Texture2D>("StartupMotion/login-map-detail-v02"));
            view.Route=view.Image("login-route-emissive-mask-v01",LastRail.Resources.PreparedContent.Load<Texture2D>("StartupMotion/login-route-emissive-mask-v01"));
            view.routeMaterial=new Material(LastRail.Resources.PreparedContent.Load<Shader>("StartupMotion/SharedRailwayPath")); view.Route.material=view.routeMaterial;
            view.node=view.Image("RailwayJunctionLight",Texture2D.whiteTexture);
            view.nodeMaterial=new Material(LastRail.Resources.PreparedContent.Load<Shader>("StartupMotion/RailwayNode")); view.node.material=view.nodeMaterial;
            var nr=view.node.rectTransform; var center=new Vector2(Landmark.x,1-Landmark.y); var half=new Vector2(10f/130f,10f/98f);nr.anchorMin=center-half;nr.anchorMax=center+half;nr.sizeDelta=Vector2.zero;
            view.SetRoute(0); view.SetTint(0);
            return view;
        }
        RawImage Image(string name,Texture texture)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(RawImage));go.transform.SetParent(transform,false);
            var r=(RectTransform)go.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.sizeDelta=Vector2.zero;
            var image=go.GetComponent<RawImage>();image.texture=texture;image.raycastTarget=false;return image;
        }
        public void SetRoute(float value)
        {
            var p=Mathf.Clamp01(value);
            // Crop geometry and UV together; do not stretch the illuminated line.
            Route.rectTransform.anchorMax=new Vector2(p,1); Route.uvRect=new Rect(0,0,p,1);
            Route.color=new Color(.18f,.72f,1,p>0 ? .85f : 0);
            node.color=new Color(.3f,.8f,1,Mathf.SmoothStep(0,.85f,(p-.4f)/.2f));
        }
        public void SetTint(float value)
        {
            settle=Mathf.Clamp01(value);
            Map.color=Color.Lerp(new Color(.82f,.87f,.9f),new Color(.43f,.51f,.55f),settle);
        }
        public void ConfigureLobby()
        {
            lobby=true; SetRoute(1); EvaluateLayout();
        }
        public void EvaluateLayout()
        {
            if(!lobby)return;
            var area=((RectTransform)transform.parent).rect;
            var fit=Mathf.Max(area.width/720f,area.height/1280f);
            var size=new Vector2(130,98)*ArrivalScale*fit;
            var r=(RectTransform)transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);
            r.sizeDelta=size;
            r.anchoredPosition=new Vector2((.5f-Landmark.x)*size.x,(Landmark.y-.5f)*size.y);
        }
        void LateUpdate(){EvaluateLayout();}
        void OnDestroy(){Release(routeMaterial);Release(nodeMaterial);}
        static void Release(Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
    }
}
