using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace LastRail.Battle {
// Display owner only. Child renderers never dispatch input or calculate domain state.
public abstract class CombatLayeredGraphic:MaskableGraphic {
 readonly Dictionary<string,CombatUiLayerGraphic> layers=new Dictionary<string,CombatUiLayerGraphic>();
 protected abstract string[] LayerNames {get;}
 protected virtual Texture LayerTexture=>Texture2D.whiteTexture;
 internal Texture SharedLayerTexture=>LayerTexture;
 protected abstract void DrawLayer(string layer,VertexHelper mesh);
 protected virtual void ConstrainReplacement(string layer,VertexHelper mesh){}
 internal void LimitReplacement(string layer,VertexHelper mesh)=>ConstrainReplacement(layer,mesh);
 public Graphic GetLayer(string name){EnsureLayers();if(!layers.TryGetValue(name,out var layer))throw new ArgumentException("Unknown display layer: "+name,nameof(name));return layer;}
 public void SetLayerVisible(string name,bool visible)=>GetLayer(name).enabled=visible;
 // Caller owns captured external data. null restores native rendering without replacing state.
 public void SetLayerRenderer(string name,Action<VertexHelper,Rect,Color> renderer)=>((CombatUiLayerGraphic)GetLayer(name)).SetReplacement(renderer);
 void EnsureLayers(){if(layers.Count>0)return;foreach(var name in LayerNames){var go=new GameObject("Layer_"+name,typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(transform,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;r.pivot=rectTransform.pivot;var layer=go.AddComponent<CombatUiLayerGraphic>();layer.raycastTarget=false;layer.Bind(this,name,DrawLayer);layers.Add(name,layer);go.SetActive(isActiveAndEnabled);}}
 protected override void OnEnable(){base.OnEnable();EnsureLayers();foreach(var layer in layers.Values)if(layer!=null)layer.gameObject.SetActive(true);SetVerticesDirty();}
 protected override void OnDisable(){foreach(var layer in layers.Values)if(layer!=null)layer.gameObject.SetActive(false);base.OnDisable();}
 protected override void OnDestroy(){foreach(var layer in layers.Values)if(layer!=null)layer.Unbind();layers.Clear();base.OnDestroy();}
 public override void SetVerticesDirty(){base.SetVerticesDirty();foreach(var layer in layers.Values)if(layer!=null)layer.SetVerticesDirty();}
 protected override void OnRectTransformDimensionsChange(){base.OnRectTransformDimensionsChange();foreach(var layer in layers.Values)if(layer!=null){layer.rectTransform.pivot=rectTransform.pivot;layer.SetVerticesDirty();}}
 protected override void OnPopulateMesh(VertexHelper mesh)=>mesh.Clear();
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CombatUiLayerGraphic:MaskableGraphic {
 public override Texture mainTexture=>owner!=null?owner.SharedLayerTexture:Texture2D.whiteTexture;
 CombatLayeredGraphic owner;string role;Action<string,VertexHelper> draw;Action<VertexHelper,Rect,Color> replacement;
 internal void Bind(CombatLayeredGraphic value,string name,Action<string,VertexHelper> native){owner=value;role=name;draw=native;SetVerticesDirty();}
 internal void Unbind(){owner=null;draw=null;replacement=null;}
 internal void SetReplacement(Action<VertexHelper,Rect,Color> value){replacement=value;SetVerticesDirty();}
 protected override void OnDestroy(){Unbind();base.OnDestroy();}
 protected override void OnPopulateMesh(VertexHelper mesh){mesh.Clear();if(owner==null||!owner.isActiveAndEnabled)return;if(replacement!=null){replacement(mesh,rectTransform.rect,color);owner.LimitReplacement(role,mesh);}else{draw?.Invoke(role,mesh);if(color!=Color.white){var v=new UIVertex();for(int i=0;i<mesh.currentVertCount;i++){mesh.PopulateUIVertex(ref v,i);v.color=(Color)v.color*color;mesh.SetUIVertex(v,i);}}}}
}
}
