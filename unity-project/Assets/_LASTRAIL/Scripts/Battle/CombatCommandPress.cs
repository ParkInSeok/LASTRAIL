using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace LastRail.Battle {
// Tactile feedback only. Button.onClick remains the sole command submission path.
public sealed class CombatCommandPress:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler,ISubmitHandler {
 Button button;Image well;RectTransform[] contents;Vector2[] homes;Func<double> clock;
 bool held;int pointer;double release=-1;
 Coroutine returning;
 public bool Pressed=>held;
 public void Configure(Image face,Func<double> now){button=GetComponent<Button>();well=face;clock=now;contents=transform.Cast<Transform>().Where(t=>t!=face.transform).Select(t=>t as RectTransform).Where(t=>t!=null).ToArray();homes=contents.Select(t=>t.anchoredPosition).ToArray();}
 public void OnPointerDown(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left||button==null||!button.IsInteractable()||held)return;StopReturn();pointer=e.pointerId;held=true;release=-1;Evaluate();}
 public void OnPointerUp(PointerEventData e){if(!held||e.pointerId!=pointer)return;held=false;BeginReturn();}
 public void OnPointerExit(PointerEventData e){if(held&&e.pointerId==pointer)ResetFeedback();}
 public void OnSubmit(BaseEventData e){if(button==null||!button.IsInteractable())return;BeginReturn();}
 void BeginReturn(){StopReturn();release=clock();Evaluate();if(Application.isPlaying&&isActiveAndEnabled)returning=StartCoroutine(ReturnToRest());}
 System.Collections.IEnumerator ReturnToRest(){
  // This .14s interpolation has no animation asset; it exists only between release and rest.
  while(release>=0&&clock()-release<.14){yield return null;Evaluate();}
  returning=null;release=-1;Evaluate();
 }
 void StopReturn(){if(returning!=null)StopCoroutine(returning);returning=null;}
 public void Evaluate(){
  if(button==null||clock==null)return;
  if(held&&!button.IsInteractable()){ResetFeedback();return;}
  float amount=held?1:release<0?0:Mathf.Clamp01(1-(float)(clock()-release)/.14f);amount=amount*amount;
  for(int i=0;i<contents.Length;i++)if(contents[i]!=null)contents[i].anchoredPosition=homes[i]+new Vector2(0,-3*amount);
  if(well!=null)well.color=new Color(.015f,.04f,.055f,.05f+.62f*amount);
 }
 void ResetFeedback(){StopReturn();held=false;release=-1;if(contents!=null)for(int i=0;i<contents.Length;i++)if(contents[i]!=null)contents[i].anchoredPosition=homes[i];if(well!=null)well.color=new Color(.015f,.04f,.055f,.05f);}
 public void CancelFeedback()=>ResetFeedback();
 void OnDisable()=>ResetFeedback();
}
}
