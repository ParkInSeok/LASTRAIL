using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Composes the two preparation panels. Textures are borrowed from Resources;
    // the containing View owns the generated hierarchy and its lifetime.
    public sealed class DeparturePanelPresentation
    {
        readonly Transform parent;
        readonly TMP_FontAsset font;
        Func<string,Rect,Rect> imageLayout;
        readonly Dictionary<RawImage,string> resources=new Dictionary<RawImage,string>();
        readonly RawImage locomotive;
        readonly RawImage[] skills=new RawImage[3];
        readonly TMP_Text[] trainLabels=new TMP_Text[5],skillLabels=new TMP_Text[3];
        readonly TMP_Text[] numbers=new TMP_Text[4];
        readonly DeparturePanelGraphic[] badges=new DeparturePanelGraphic[4];
        readonly DeparturePanelGraphic[] plates=new DeparturePanelGraphic[4],roles=new DeparturePanelGraphic[4],locks=new DeparturePanelGraphic[4],frames=new DeparturePanelGraphic[4];
        public DeparturePanelPresentation(Transform parent,TMP_FontAsset font)
        {
            this.parent=parent;this.font=font;
            Graphic("PreparationTrainHeading",52,990,58,35,DeparturePanelGraphic.Shape.TrainHeading,new Color(.94f,.9f,.8f));
            Graphic("PreparationSupportHeading",52,1224,45,38,DeparturePanelGraphic.Shape.SupportHeading,new Color(.94f,.9f,.8f));
            var depotViewport=Rect("PreparationTrainEnvironmentViewport",30,1038,880,159);
            depotViewport.gameObject.AddComponent<RectMask2D>();
            var depot=Art("PreparationTrainEnvironment",0,0,880,159);depot.transform.SetParent(depotViewport,false);
            float depotCropHeight=159f/880*2087;
            Assign(depot,"NoncombatPreparation/Art54/train-depot-v01",new Rect(0,(754-260-depotCropHeight)/754,1,depotCropHeight/754));
            Graphic("PreparationRail",32,1145,880,10,DeparturePanelGraphic.Shape.Rail,Color.white);
            locomotive=Art("PreparationCarriage0",34,1040,211,111);
            trainLabels[0]=Text("PreparationCarriageLabel0",43,1152,192,43,20);
            for(int i=0;i<4;i++)
            {
                float x=248+i*166;
                // Preserve source aspect in each metal/chassis strip. The middle is a
                // separate state plate, not a stretched image of a complete wagon.
                var roof=Art("PreparationChassisRoof"+(i+1),x+10,1040,146,113*146f/840);
                Assign(roof,"NoncombatPreparation/Art53/wagon-chassis-v02-alpha",new Rect(208f/1254,959f/1254,840f/1254,113f/1254));
                var chassis=Art("PreparationChassis"+(i+1),x,1151-214*166f/1182,166,214*166f/1182);
                Assign(chassis,"NoncombatPreparation/Art53/wagon-chassis-v02-alpha",new Rect(36f/1254,190f/1254,1182f/1254,214f/1254));
                var left=Art("PreparationChassisSideLeft"+(i+1),x+1,1058,9.2f,69);
                Assign(left,"NoncombatPreparation/Art53/wagon-chassis-v02-alpha",new Rect(207f/1254,474f/1254,60f/1254,450f/1254));
                var right=Art("PreparationChassisSideRight"+(i+1),x+155.8f,1058,9.2f,69);
                Assign(right,"NoncombatPreparation/Art53/wagon-chassis-v02-alpha",new Rect(987f/1254,474f/1254,60f/1254,450f/1254));
                plates[i]=Graphic("PreparationCarriagePlate"+(i+1),x+10,1058,146,69,DeparturePanelGraphic.Shape.Plate,new Color(.025f,.13f,.21f));
                frames[i]=Graphic("PreparationCarriageOutline"+(i+1),x+7,1055,152,75,DeparturePanelGraphic.Shape.Frame,new Color(.12f,.65f,.9f));
                badges[i]=Graphic("PreparationCarriageBadge"+(i+1),x+16,1064,26,30,DeparturePanelGraphic.Shape.Badge,new Color(.12f,.6f,.8f));
                numbers[i]=Text("PreparationCarriageNumber"+(i+1),x+16,1064,26,30,24);numbers[i].text=(i+1).ToString();
                roles[i]=Graphic("PreparationCarriageRole"+(i+1),x+69,1065,30,27,DeparturePanelGraphic.Shape.Role,Color.white);
                locks[i]=Graphic("PreparationCarriageLock"+(i+1),x+67,1073,26,34,DeparturePanelGraphic.Shape.Lock,new Color(.6f,.65f,.67f));locks[i].enabled=false;
                trainLabels[i+1]=Text("PreparationCarriageLabel"+(i+1),x+32,1095,118,29,24);
                Graphic("PreparationCarriageLamp"+(i+1),x-4,1061,10,21,DeparturePanelGraphic.Shape.AmberLamp,Color.white);
                Graphic("PreparationCarriageCoupler"+(i+1),x-7,1135,14,6,DeparturePanelGraphic.Shape.Coupler,Color.white);
            }
            var environment=Art("PreparationSupportEnvironment",30,1274,880,159);float cropHeight=159f/880*2206/713;Assign(environment,"NoncombatPreparation/Art53/support-wall-v01",new Rect(0,1-cropHeight,1,cropHeight));
            for(int i=0;i<3;i++)
            {
                float x=213+i*181;
                Graphic("PreparationSupportPlate"+i,x+6,1284,142,138,DeparturePanelGraphic.Shape.Plate,new Color(7f/255,75f/255,117f/255));
                Graphic("PreparationSupportFrame"+i,x,1278,154,150,DeparturePanelGraphic.Shape.Frame,new Color(.02f,.73f,1));
                skills[i]=Art("PreparationSupport"+i,x+12,1288,130,105);
                skillLabels[i]=Text("PreparationSupportLabel"+i,x+8,1394,138,30,23);
            }
        }
        public void Carriage(int index,string label,string resource,bool locked,string role)
        {
            if(index<0||index>=5)return;
            trainLabels[index].text=label;
            if(index==0){Assign(locomotive,resource==null?null:"NoncombatPreparation/Art53/locomotive-side-v01-alpha",new Rect(21f/1729,113f/910,1694f/1729,743f/910));Fit(locomotive,34,1040,211,111);return;}
            int i=index-1;
            bool installed=!locked&&resource!=null;
            plates[i].Configure(DeparturePanelGraphic.Shape.Plate,installed?new Color(7f/255,53f/255,85f/255):new Color(.025f,.032f,.04f));
            frames[i].Configure(DeparturePanelGraphic.Shape.Frame,installed?new Color(.1f,.6f,.83f):new Color(.28f,.32f,.34f));
            badges[i].Configure(DeparturePanelGraphic.Shape.Badge,locked?new Color(.25f,.3f,.32f):new Color(.12f,.6f,.8f));numbers[i].color=locked?new Color(.55f,.59f,.6f):new Color(.95f,.95f,.9f);
            roles[i].Configure(DeparturePanelGraphic.Shape.Role,Color.white,role);roles[i].enabled=installed&&role!=null;locks[i].enabled=locked;
            trainLabels[index].enabled=!locked;
        }
        public void Support(int index,string id,string label)
        {
            if(index<0||index>=3)return;
            CommanderSupportIconCatalog.TryGet(id,out var resource,out var uv);
            Assign(skills[index],resource,uv);
            Fit(skills[index],225+index*181,1288,130,105);skillLabels[index].text=label;
        }
        // The containing view supplies the same section mapping used by the frames and labels.
        // Refit from the texture and UV after each identity change; never restore portrait positions.
        public void SetImageLayout(Func<string,Rect,Rect> layout)
        {
            imageLayout=layout;
            Fit(locomotive,34,1040,211,111);
            for(int i=0;i<3;i++)Fit(skills[i],225+i*181,1288,130,105);
        }
        void Fit(RawImage image,float x,float y,float width,float height)
        {
            if(image.texture==null)return;
            float sw=image.texture.width*image.uvRect.width,sh=image.texture.height*image.uvRect.height;
            float scale=Mathf.Min(width/sw,height/sh);var size=new Vector2(sw*scale,sh*scale);
            var source=new Rect(x+(width-size.x)/2,y+(height-size.y)*(y<1213?1:.5f),size.x,size.y);
            var target=imageLayout==null?source:imageLayout(image.name,source);
            image.rectTransform.sizeDelta=size;
            image.rectTransform.localScale=Vector3.one*(source.width>0?target.width/source.width:1);
            image.rectTransform.anchoredPosition=new Vector2(target.x,-target.y);
        }
        void Assign(RawImage image,string resource,Rect uv){if(!resources.TryGetValue(image,out var previous)||previous!=resource){resources[image]=resource;image.texture=resource==null?null:LastRail.Resources.PreparedContent.Load<Texture2D>(resource);}image.uvRect=uv;image.enabled=image.texture!=null;}
        RectTransform Rect(string name,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        RawImage Art(string name,float x,float y,float w,float h){var image=Rect(name,x,y,w,h).gameObject.AddComponent<RawImage>();image.raycastTarget=false;image.enabled=false;return image;}
        DeparturePanelGraphic Graphic(string name,float x,float y,float w,float h,DeparturePanelGraphic.Shape shape,Color color){var g=Rect(name,x,y,w,h).gameObject.AddComponent<DeparturePanelGraphic>();g.Configure(shape,color);return g;}
        TMP_Text Text(string name,float x,float y,float w,float h,float size){var t=Rect(name,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=size;t.enableAutoSizing=true;t.fontSizeMin=size*.7f;t.fontSizeMax=size;t.color=new Color(.95f,.95f,.9f);t.richText=false;t.raycastTarget=false;t.alignment=TextAlignmentOptions.Center;return t;}
    }
}
