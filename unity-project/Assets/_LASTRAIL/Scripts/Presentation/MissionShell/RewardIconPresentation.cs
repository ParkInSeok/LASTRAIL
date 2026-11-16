using LastRail.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    // Shared display mapping. Textures are borrowed; generated children belong to the parent view.
    public static class RewardIconPresentation
    {
        public static RawImage Create(Transform parent,TMP_FontAsset font,string id,Rect area)
        {
            int index=id=="cur_credits"?0:id=="cur_machine_parts"?1:id=="cur_operator_exp"?2:id=="cur_tactical_data"?3:-1;
            var texture=index<0?null:LastRail.Resources.PreparedContent.Load<Texture2D>("EntryPopup/Rewards/"+id);
            bool exp=id=="cur_operator_exp"&&texture!=null;
            var uv=new Rect(0,0,1,1);
            if(texture==null&&index>=0)
            {
                texture=LastRail.Resources.PreparedContent.Load<Texture2D>("EntryPopup/roulette-reward-icons");
                uv=new Rect(index%2*.5f,index<2?.5f:0,.5f,.5f);
            }
            TMP_Text Label(string name,Transform owner,string text,Rect box,float size)
            {
                var label=owner.Find(name)?.GetComponent<TMP_Text>()??AuthFormVisuals.Text(owner,name,text,font,size,false,TextAlignmentOptions.Center);label.text=text;label.gameObject.SetActive(true);
                AuthFormVisuals.Place(label.rectTransform,box.x,box.y,box.width,box.height);
                label.enableAutoSizing=true;label.fontSizeMin=Mathf.Min(16,size);label.fontSizeMax=size;
                label.color=new Color(.94f,.96f,.97f);return label;
            }
            var existing=parent.Find("RewardIcon")?.GetComponent<RawImage>();
            var placeholder=parent.Find("MissingRewardArt");if(placeholder!=null)placeholder.gameObject.SetActive(false);
            if(existing!=null){existing.texture=null;existing.gameObject.SetActive(false);var oldCaption=existing.transform.Find("ExperienceChipLabel");if(oldCaption!=null)oldCaption.gameObject.SetActive(false);}
            if(texture==null)
            {
                var missing=Label("MissingRewardArt",parent,"이미지 준비 중",area,17);
                missing.color=new Color(.61f,.64f,.65f);return null;
            }
            float sourceWidth=texture.width*uv.width,sourceHeight=texture.height*uv.height;
            float scale=Mathf.Min(area.width/sourceWidth,area.height/sourceHeight);
            float width=sourceWidth*scale,height=sourceHeight*scale;
            var rect=existing!=null?existing.rectTransform:AuthFormVisuals.Rect("RewardIcon",parent);rect.gameObject.SetActive(true);
            AuthFormVisuals.Place(rect,area.x+(area.width-width)/2,area.y+(area.height-height)/2,width,height);
            var image=existing??rect.gameObject.AddComponent<RawImage>();image.texture=texture;image.uvRect=uv;image.raycastTarget=false;
            if(exp)
            {
                var caption=Label("ExperienceChipLabel",rect,"EXP",new Rect(width*.36f,height*.305f,width*.35f,height*.18f),Mathf.Min(22,height*.14f));
                caption.fontStyle=FontStyles.Bold;caption.color=new Color(.97f,.72f,.56f);
                caption.rectTransform.localRotation=Quaternion.Euler(0,0,-8);
            }
            return image;
        }
    }
}
