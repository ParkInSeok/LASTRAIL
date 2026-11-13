using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace LastRail.Battle {
public sealed partial class ApprovedCombatView {
 CombatHeaderGlyph pauseGlyph;
 void ApplyTitleBar(){
  // Five modules share the outer machining datum; content has its own inset.
  var titleFrame=Rect("TitleHousing",root,8,6,242,52).gameObject.AddComponent<CombatTitleBarFrame>();titleFrame.raycastTarget=false;titleFrame.transform.SetSiblingIndex(root.Find("Title").GetSiblingIndex());
  var waveFrame=Rect("WaveHousing",root,256,6,202,52).gameObject.AddComponent<CombatTitleBarFrame>();waveFrame.raycastTarget=false;waveFrame.transform.SetSiblingIndex(wave.transform.GetSiblingIndex());
  titleFrame.SetWeathering(true);waveFrame.SetWeathering(true);
  var title=(RectTransform)root.Find("Title");title.anchoredPosition=new Vector2(20,-13);title.sizeDelta=new Vector2(218,38);title.GetComponent<TMP_Text>().fontSize=24;
  var logo=root.Find("TitleEmblem") as RectTransform;if(logo!=null){logo.anchoredPosition=new Vector2(29,-10);logo.sizeDelta=new Vector2(200,44);}
  wave.rectTransform.anchoredPosition=new Vector2(266,-11);wave.rectTransform.sizeDelta=new Vector2(182,25);wave.alignment=TextAlignmentOptions.Center;wave.fontSize=18;
  waveTrack.rectTransform.anchoredPosition=new Vector2(272,-42);waveTrack.rectTransform.sizeDelta=new Vector2(170,8);
  HeaderModule(ultimateAutoButton,464,88,"AutoHousing");HeaderModule(speed,558,84,"ControlHousing");HeaderModule(pause,648,64,"ControlHousing");
  var speedLabel=speed.GetComponentInChildren<TMP_Text>();speedLabel.rectTransform.anchoredPosition=new Vector2(29,-7);speedLabel.rectTransform.sizeDelta=new Vector2(46,38);speedLabel.fontSize=18;
  var arrows=(RectTransform)speed.transform.Find("SpeedChevrons");arrows.anchoredPosition=new Vector2(9,-8);arrows.sizeDelta=new Vector2(20,36);arrows.GetComponent<TMP_Text>().enabled=false;var chevrons=Rect("SpeedGlyph",speed.transform,8,17,22,18).gameObject.AddComponent<CombatHeaderGlyph>();chevrons.raycastTarget=false;arrows.GetComponent<TMP_Text>().color=new Color(.1f,.8f,.94f);
  var pauseLabel=pause.GetComponentInChildren<TMP_Text>();pauseLabel.rectTransform.anchoredPosition=new Vector2(8,-7);pauseLabel.rectTransform.sizeDelta=new Vector2(48,38);pauseLabel.fontSize=25;pauseLabel.enabled=false;pauseGlyph=Rect("PauseGlyph",pause.transform,20,14,24,24).gameObject.AddComponent<CombatHeaderGlyph>();pauseGlyph.raycastTarget=false;pauseGlyph.SetSymbol(CombatHeaderGlyph.Symbol.Pause);
  if(autoThumb!=null){autoThumb.rectTransform.sizeDelta=new Vector2(14,14);autoText.fontSize=16;}
  root.Find("HeaderUnderlight").GetComponent<Image>().color=new Color(.18f,.23f,.24f,.7f);
 }
 void HeaderModule(Button button,float x,float width,string housingName){if(button==null)return;var rect=(RectTransform)button.transform;rect.anchoredPosition=new Vector2(x,-6);rect.sizeDelta=new Vector2(width,52);var housing=(RectTransform)rect.Find(housingName);housing.anchoredPosition=Vector2.zero;housing.sizeDelta=rect.sizeDelta;housing.GetComponent<CombatConceptPanel>().enabled=false;var frame=Rect("MachinedFrame",housing,0,0,width,52).gameObject.AddComponent<CombatTitleBarFrame>();frame.raycastTarget=false;frame.SetWeathering(true);var feedback=button.gameObject.AddComponent<CombatTitleBarPress>();feedback.Frame=frame;button.GetComponent<Image>().color=Color.clear;}
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatTitleBarFrame:CombatLayeredGraphic {
 static readonly string[] Names={"Frame","Backplate"};protected override string[] LayerNames=>Names;
 protected override void ConstrainReplacement(string layer,VertexHelper mesh){var hole=CombatUiMeshPartition.Aperture(Inset(rectTransform.rect,5),2);if(layer=="Backplate")CombatUiMeshPartition.KeepInterior(mesh,hole);else CombatUiMeshPartition.RemoveInterior(mesh,hole);}
 protected override void DrawLayer(string layer,VertexHelper mesh){using(var complete=new VertexHelper()){DrawComplete(complete);CombatUiMeshPartition.Copy(complete,mesh,CombatUiMeshPartition.Aperture(Inset(rectTransform.rect,5),2),layer=="Backplate");}}
 bool pressed;public void SetPressed(bool value){if(pressed==value)return;pressed=value;SetVerticesDirty();}
 bool weathered;public void SetWeathering(bool value){if(weathered==value)return;weathered=value;SetVerticesDirty();}
 // Stable local surface detail: no textures, shared RNG, update loop or input ownership.
 // DrawComplete is partitioned into Frame/Backplate afterwards, including these marks.
 static float Grain(int index,int salt){unchecked{uint v=(uint)(index*374761393+salt*668265263);v=(v^(v>>13))*1274126177u;return (v^(v>>16))%10000/10000f;}}
 static void Mark(VertexHelper m,float x,float y,float w,float h,float slope,Color c){int n=m.currentVertCount;m.AddVert(new Vector3(x,y),c,Vector2.zero);m.AddVert(new Vector3(x+w,y+slope),c,Vector2.zero);m.AddVert(new Vector3(x+w,y+h+slope),c,Vector2.zero);m.AddVert(new Vector3(x,y+h),c,Vector2.zero);m.AddTriangle(n,n+1,n+2);m.AddTriangle(n,n+2,n+3);}
 void SurfaceWear(VertexHelper m,Rect r){
  int seed=Mathf.RoundToInt(r.width*7);
  for(int i=0;i<Mathf.CeilToInt(r.width*2.2f);i++){
   float x=r.x+6+Grain(i,seed)*(r.width-13),y=r.y+6+Grain(i,seed+1)*(r.height-13);
   float edge=Mathf.Min(x-r.x,r.xMax-x,y-r.y,r.yMax-y);float a=edge<12?.25f:.09f;
   var c=Grain(i,seed+2)>.48f?new Color(.34f,.36f,.34f,a):new Color(.002f,.006f,.007f,a*2);
   Mark(m,x,y,.5f+Grain(i,seed+3)*2.8f,.5f+Grain(i,seed+4)*1.2f,0,c);
  }
  // Uneven exposed metal along the long rims, paired with dark recessed chips.
  for(int i=0;i<Mathf.CeilToInt(r.width/4);i++){
   float x=r.x+8+Grain(i,seed+5)*(r.width-22),y=i%2==0?r.y+1.2f:r.yMax-3.4f;
   float w=1+Grain(i,seed+6)*5;
   Mark(m,x,y,w,.6f+Grain(i,seed+7),.3f,new Color(.48f,.49f,.43f,.65f));
   Mark(m,x+.5f,y+1,w*.7f,.65f,-.2f,new Color(.005f,.01f,.012f,.8f));
  }
  // Scuffs remain near the housing corners, clear of the label field.
  for(int i=0;i<12;i++){
   bool right=i%2==0;float x=right?r.xMax-17-Grain(i,seed+8)*9:r.x+6+Grain(i,seed+8)*9;
   float y=i%3==0?r.y+7+Grain(i,seed+9)*5:r.yMax-13+Grain(i,seed+9)*4;
   Mark(m,x,y,3+Grain(i,seed+10)*6,.65f,1.3f,new Color(.39f,.40f,.35f,.38f));
  }
 }
 static Rect Inset(Rect r,float n)=>new Rect(r.x+n,r.y+n,r.width-2*n,r.height-2*n);
 static void Face(VertexHelper m,Rect r,float bevel,Color bottom,Color top){int n=m.currentVertCount;var p=new[]{new Vector2(r.x+bevel,r.y),new Vector2(r.xMax-bevel,r.y),new Vector2(r.xMax,r.y+bevel),new Vector2(r.xMax,r.yMax-bevel),new Vector2(r.xMax-bevel,r.yMax),new Vector2(r.x+bevel,r.yMax),new Vector2(r.x,r.yMax-bevel),new Vector2(r.x,r.y+bevel)};m.AddVert(r.center,Color.Lerp(bottom,top,.5f),Vector2.zero);foreach(var v in p)m.AddVert(v,Color.Lerp(bottom,top,(v.y-r.y)/r.height),Vector2.zero);for(int i=0;i<8;i++)m.AddTriangle(n,n+1+i,n+1+(i+1)%8);}
 void DrawComplete(VertexHelper m){
  m.Clear();var r=rectTransform.rect;
  Face(m,r,5,new Color(.012f,.016f,.018f),new Color(.48f,.50f,.48f));
  Face(m,Inset(r,1),4,new Color(.07f,.085f,.09f),new Color(.21f,.235f,.24f));
  Face(m,Inset(r,3),3,new Color(.32f,.35f,.35f),new Color(.004f,.009f,.012f));
  Face(m,Inset(r,5),2,new Color(.028f,.042f,.046f),pressed?new Color(.004f,.012f,.016f):new Color(.055f,.073f,.077f));
  Face(m,new Rect(r.x+8,r.yMax-5,r.width-16,4),1,new Color(.09f,.115f,.12f),new Color(.30f,.33f,.32f));
  Face(m,new Rect(r.x+8,r.y+1,r.width-16,3),1,new Color(.025f,.038f,.042f),new Color(.19f,.23f,.23f));
  // Recessed glass catches a narrow reflected edge; the label field stays matte.
  Face(m,new Rect(r.x+10,r.y+6,r.width-20,.8f),.2f,new Color(.025f,.16f,.19f),new Color(.04f,.23f,.27f));
  foreach(float x in new[]{r.x+1,r.xMax-17}){Face(m,new Rect(x,r.yMax-7,16,6),2,new Color(.09f,.12f,.13f),new Color(.32f,.35f,.34f));Face(m,new Rect(x,r.y+1,16,5),2,new Color(.03f,.045f,.05f),new Color(.20f,.24f,.24f));}
  // Local machining marks stay on the rim, outside the content inset.
  Face(m,new Rect(r.x+12,r.yMax-2,Mathf.Min(21,r.width*.16f),.7f),.2f,new Color(.31f,.32f,.31f),new Color(.43f,.44f,.42f));
  Face(m,new Rect(r.xMax-28,r.y+1.3f,11,.7f),.2f,new Color(.16f,.18f,.18f),new Color(.29f,.30f,.29f));
  if(weathered)SurfaceWear(m,r);
  foreach(float x in new[]{r.x+4,r.xMax-4})foreach(float y in new[]{r.y+5,r.yMax-5}){
   Face(m,new Rect(x-2,y-2,4,4),1,new Color(.01f,.018f,.02f),new Color(.08f,.095f,.10f));
   Face(m,new Rect(x-1.3f,y-1.3f,2.6f,2.6f),.65f,new Color(.14f,.16f,.16f),new Color(.39f,.40f,.38f));
   Face(m,new Rect(x-1,y-.3f,2,.6f),0,new Color(.025f,.035f,.038f),new Color(.025f,.035f,.038f));
  }
 }
}
public sealed class CombatTitleBarPress:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler {
 public CombatTitleBarFrame Frame;public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left&&GetComponent<Button>().IsInteractable())Frame?.SetPressed(true);}public void OnPointerUp(PointerEventData e)=>Frame?.SetPressed(false);public void OnPointerExit(PointerEventData e)=>Frame?.SetPressed(false);void OnDisable()=>Frame?.SetPressed(false);void OnApplicationFocus(bool value){if(!value)Frame?.SetPressed(false);}
}
}
