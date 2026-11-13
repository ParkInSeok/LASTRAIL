using LastRail.Auth;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Owns only the clipped scene hierarchy and registration between its independent layers.
    // Resource textures are borrowed; the containing View owns/destroys the child objects.
    public sealed class DepartureScenePresentation
    {
        const float Width=941,RegistrationHeight=485;
        readonly RectTransform viewport;
        readonly RawImage environment,driver;
        readonly Texture2D backgroundTexture;

        public DepartureScenePresentation(Transform parent)
        {
            viewport=TopRect("PreparationScene",parent,0,0,Width,RegistrationHeight);
            viewport.gameObject.AddComponent<RectMask2D>();
            var art=LastRail.Resources.PreparedContent.Load<ApprovedUiConnections>("ApprovedUi/Connections20261110");
            backgroundTexture=art?.DepartureCabin;
            environment=Layer("PreparationEnvironment",viewport,backgroundTexture);
            driver=Layer("PreparationDriver",viewport,art?.Pilot);
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            float width=viewport.rect.width,height=viewport.rect.height;
            environment.rectTransform.sizeDelta=new Vector2(width,height);
            environment.uvRect=OperatorVisualCatalog.Crop(backgroundTexture,width,height);
            if(driver.texture==null)return;
            // Approved D01 composition: bottom-left, 76% height, at most 52% width.
            float scale=Mathf.Min(height*.76f/driver.texture.height,width*.52f/driver.texture.width);
            var size=new Vector2(driver.texture.width*scale,driver.texture.height*scale);
            driver.rectTransform.sizeDelta=size;
            driver.rectTransform.anchoredPosition=new Vector2(0,-height+size.y);
        }

        static RawImage Layer(string name,Transform parent,Texture2D texture)
        {
            var rect=TopRect(name,parent,0,0,Width,RegistrationHeight);
            var image=rect.gameObject.AddComponent<RawImage>();image.texture=texture;
            image.raycastTarget=false;image.enabled=texture!=null;return image;
        }

        static RectTransform TopRect(string name,Transform parent,float x,float y,float width,float height)
        {
            var rect=AuthFormVisuals.Rect(name,parent);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);return rect;
        }
    }
}
