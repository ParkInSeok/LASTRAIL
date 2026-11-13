using UnityEngine;
using UnityEngine.UI;
using LastRail.Presentation;
namespace LastRail.Battle {
public sealed partial class ApprovedCombatView {
 Texture2D SecondArt(string name)=>assets.SecondFinalResources==null?LastRail.Resources.PreparedContent.Load<Texture2D>("CombatSecondFinal/"+name):System.Array.Find(assets.SecondFinalResources,t=>t!=null&&t.name==name)??LastRail.Resources.PreparedContent.Load<Texture2D>("CombatSecondFinal/"+name);
 static void FitArt(RectTransform rect,Texture2D texture,float cx,float cy,float width,float height){float scale=Mathf.Min(width/texture.width,height/texture.height);var size=new Vector2(texture.width,texture.height)*scale;rect.anchoredPosition=new Vector2(cx-size.x/2,-(cy-size.y/2));rect.sizeDelta=size;}
 void ApplySecondFinalArt(){
  var title=SecondArt("title_emblem-v01");if(title!=null){var image=Raw("TitleEmblem",root,title,12,6,200,48);image.uvRect=new Rect(10f/2172,129f/724,2135f/2172,470f/724);root.Find("Title").GetComponent<TMPro.TMP_Text>().color=Color.clear;}
  var bezel=SecondArt("console_bezel-v01");if(bezel!=null){foreach(var area in new[]{new Rect(0,66,720,273),new Rect(0,391,720,364)}){var sprite=OwnPresentationSprite(Sprite.Create(bezel,new Rect(18f/1672*bezel.width,97f/941*bezel.height,1638f/1672*bezel.width,748f/941*bezel.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(92f/1672*bezel.width,82f/941*bezel.height,92f/1672*bezel.width,82f/941*bezel.height)));var frame=Box("MonitorBezel",root,area.x,area.y,area.width,area.height,Color.white,sprite);frame.pixelsPerUnitMultiplier=3.5f*bezel.width/1672;}}
  var plate=SecondArt("console_plate-v01");if(plate!=null){var shellSprite=OwnPresentationSprite(Sprite.Create(plate,new Rect(0,0,plate.width,plate.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,Vector4.one*(180f/1254*plate.width)));var shell=Box("SecondConsoleShell",root,0,756,720,524,new Color(.32f,.36f,.38f),shellSprite);shell.pixelsPerUnitMultiplier=3f*plate.width/1254;shell.transform.SetSiblingIndex(root.Find("CommandConsole").GetSiblingIndex());root.Find("CommandConsole").gameObject.SetActive(false);foreach(string name in new[]{"BranchHousing","ThrottleHousing"}){var target=(RectTransform)root.Find(name);var back=Box("MetalPlate",target,0,0,target.sizeDelta.x,target.sizeDelta.y,new Color(.62f,.68f,.7f),shellSprite);back.pixelsPerUnitMultiplier=4f*plate.width/1254;}}
  var handle=SecondArt("control_handle-v01");if(handle!=null){foreach(var slider in new[]{branchLever,trainThrottle}){slider.handleRect.GetComponent<Image>().color=Color.clear;var grip=Raw("MechanicalGrip",slider.handleRect,handle,0,0,slider==branchLever?110:74,slider==branchLever?34:28);grip.uvRect=new Rect(340f/2172,180f/724,1500f/2172,360f/724);grip.rectTransform.anchorMin=grip.rectTransform.anchorMax=grip.rectTransform.pivot=new Vector2(.5f,.5f);grip.rectTransform.anchoredPosition=Vector2.zero;if(slider==trainThrottle)grip.rectTransform.localRotation=Quaternion.Euler(0,0,90);slider.targetGraphic=grip;var colors=slider.colors;colors.disabledColor=new Color(.65f,.65f,.65f,1);slider.colors=colors;}}
  foreach(var c in controls){if(c.Group!="manuals")continue;string id=(string)driver.HudState["manuals"][c.Index]["ability"];
   if(CommanderSupportIconCatalog.TryGet(id,out var resource,out var uv)){
    var texture=LastRail.Resources.PreparedContent.Load<Texture2D>(resource);if(texture==null)throw new System.InvalidOperationException("Commander support icon missing: "+resource);
    c.AbilityIcon.texture=texture;c.AbilityIcon.uvRect=uv;
    float width=texture.width*uv.width,height=texture.height*uv.height,scale=Mathf.Min(60/width,60/height);
    var size=new Vector2(width*scale,height*scale);c.AbilityIcon.rectTransform.sizeDelta=size;c.AbilityIcon.rectTransform.anchoredPosition=new Vector2(4+(60-size.x)/2,-(3+(60-size.y)/2));
   }else{var tex=SecondArt("support_repair-v01");if(tex!=null){c.AbilityIcon.texture=tex;c.AbilityIcon.uvRect=new Rect(0,0,1,1);}}
  }
 }
}
}
