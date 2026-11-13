using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Battle {
 // Procedural diagnostic presentation, not approved final weapon art/palette/timing.
 // One-authority-step flashes are explicitly a QA display convention, not game policy.
 // Parent owns Controller binding; this View only owns created render objects/material.
 public sealed class MiraFirstFeedbackView:MonoBehaviour {
  readonly Dictionary<string,GameObject> bodies=new Dictionary<string,GameObject>(StringComparer.Ordinal);
  readonly Dictionary<string,string> kinds=new Dictionary<string,string>(StringComparer.Ordinal);
  readonly Dictionary<string,GameObject> pulses=new Dictionary<string,GameObject>(StringComparer.Ordinal);
  Material material;Transform basis;long tick=-1;bool visible=true,decorationReduced;
  [SerializeField] Color mainTint=Color.white,shieldTint=Color.cyan;
  Material OwnedMaterial {get{if(material==null){var shader=Shader.Find("Sprites/Default");if(shader==null)throw new InvalidOperationException("Diagnostic feedback shader unavailable");material=new Material(shader){name="MiraFirstFeedback_OwnedDiagnostic"};}return material;}}
  static Vector3 Point(JToken value)=>new Vector3((float)(long)value[0],(float)(long)value[1],(float)(long)value[2])/1000f;
  GameObject Node(string id){var go=new GameObject(id);go.transform.SetParent(transform,false);return go;}
  static void Release(UnityEngine.Object value){if(value==null)return;if(value is GameObject go)go.SetActive(false);if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
  GameObject Sphere(string id,Vector3 point,float radius,Color tint){
   var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=id;go.transform.SetParent(transform,false);go.transform.localPosition=point;go.transform.localScale=Vector3.one*(radius*2);
   var collider=go.GetComponent<Collider>();collider.enabled=false;Release(collider);var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=OwnedMaterial;var block=new MaterialPropertyBlock();block.SetColor("_Color",tint);renderer.SetPropertyBlock(block);renderer.enabled=visible;return go;
  }
  LineRenderer Line(GameObject parent,string id,Vector3[] points,float width,Color tint){
   var go=new GameObject(id);go.transform.SetParent(parent.transform,false);var line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;line.sharedMaterial=OwnedMaterial;line.positionCount=points.Length;line.SetPositions(points);line.startWidth=line.endWidth=width;line.startColor=line.endColor=tint;line.enabled=visible&&!(decorationReduced&&id=="ShortTrail");return line;
  }
  public void BindBasis(Transform geometryBasis){
   if(geometryBasis==null||geometryBasis==transform||geometryBasis.IsChildOf(transform))throw new ArgumentException("mira_feedback_geometry_basis");Clear();basis=geometryBasis;transform.SetParent(basis,false);transform.localPosition=Vector3.zero;transform.localRotation=Quaternion.identity;transform.localScale=Vector3.one;
  }
  void RequireBasis(JObject frame){
   if(basis==null||transform.parent!=basis||transform.localPosition!=Vector3.zero||transform.localRotation!=Quaternion.identity||transform.localScale!=Vector3.one)throw new InvalidOperationException("mira_feedback_basis_binding_changed");
   var q=basis.rotation;var p=basis.position;long Mm(float v)=>checked((long)Math.Round((double)v*1000,MidpointRounding.AwayFromZero));
   var actual=new JObject{["positionMm"]=new JArray(Mm(p.x),Mm(p.y),Mm(p.z)),["rotation"]=new JArray(q.x,q.y,q.z,q.w)};
   if(!JToken.DeepEquals(MiraJudgementWire.Capture(actual),MiraJudgementWire.Capture(frame["basis"] as JObject)))throw new ArgumentException("mira_feedback_authority_basis_conflict");
  }
  public void ApplyFrame(JObject frame){
   if(!isActiveAndEnabled)return;
   if(frame==null||(string)frame["version"]!="mira.first-feedback-frame.v1.20261015"||frame["flights"] is not JArray flights||frame["pulses"] is not JArray events)throw new ArgumentException("mira_feedback_view_frame");
   RequireBasis(frame);foreach(var flight in flights){string id=(string)flight["id"],kind=(string)flight["kind"];if(kind!="MAIN"&&kind!="SECONDARY"&&kind!="PIERCE"||kinds.TryGetValue(id,out var oldKind)&&oldKind!=kind)throw new ArgumentException("mira_feedback_branch_contract");}long current=(long)frame["tick"];
   if(current!=tick){foreach(var go in pulses.Values)Release(go);pulses.Clear();tick=current;}
   var ids=new HashSet<string>(flights.Select(f=>(string)f["id"]),StringComparer.Ordinal);
   foreach(string id in bodies.Keys.Where(k=>!ids.Contains(k)).ToArray()){Release(bodies[id]);bodies.Remove(id);kinds.Remove(id);}
   foreach(var flight in flights){string id=(string)flight["id"],kind=(string)flight["kind"];var point=Point(flight["positionMm"]);var direction=(Point(flight["anchorMm"])-Point(flight["originMm"])).normalized;
    if(!bodies.TryGetValue(id,out var go)){go=Node(kind+"Body:"+id);bodies.Add(id,go);kinds.Add(id,kind);var body=Sphere("Body",Vector3.zero,kind=="SECONDARY"?.03f:.06f,mainTint);body.transform.SetParent(go.transform,false);Line(go,"ShortTrail",new[]{Vector3.zero,-direction*.24f},kind=="SECONDARY"?.02f:.04f,mainTint);}
    go.transform.localPosition=point;
   }
   foreach(var pulse in events){string id=(string)pulse["id"],kind=(string)pulse["kind"];if(pulses.ContainsKey(id))continue;var point=Point(pulse["pointMm"]);GameObject go;
    if(kind=="SHIELD"||kind=="HP"){
     go=Node(kind+":"+id);go.transform.localPosition=point;float radius=kind=="SHIELD"?.45f:.20f;var ring=Enumerable.Range(0,25).Select(n=>new Vector3(Mathf.Cos(n*Mathf.PI/12)*radius,Mathf.Sin(n*Mathf.PI/12)*radius,0)).ToArray();Line(go,kind+"Response",ring,.035f,kind=="SHIELD"?shieldTint:mainTint);
    }else if(kind=="CONTACT"){
     // The physical core owns its radius; text is a sibling at the original contact anchor.
     go=Node(kind+":"+id);go.transform.localPosition=point;var core=Sphere("ContactCore",Vector3.zero,.09f,mainTint);core.transform.SetParent(go.transform,false);
    }else if(kind=="FIRE"||kind=="BLOCKED")go=Sphere(kind+":"+id,point,.09f,mainTint);
    else throw new ArgumentException("Unsupported feedback pulse; never synthesize an HP hit");
    if(kind=="CONTACT"&&pulse["labels"] is JObject labels&&pulse["targetGeneration"]?.Type==JTokenType.Integer)AddLabels(go,labels);
    pulses.Add(id,go);
   }
  }
  // Temporary diagnostic typography; final font/scale/duration require Direction and Player QA.
  void AddLabels(GameObject parent,JObject labels){
   var font=UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");if(font==null)throw new InvalidOperationException("Diagnostic label font unavailable");
   void Label(string name,string text,Vector3 offset){if(string.IsNullOrEmpty(text))return;var node=new GameObject(name);node.transform.SetParent(parent.transform,false);node.transform.localPosition=offset;var mesh=node.AddComponent<TextMesh>();mesh.font=font;mesh.fontSize=64;mesh.characterSize=.04f;mesh.anchor=TextAnchor.MiddleLeft;mesh.text=text;mesh.color=Color.white;node.GetComponent<MeshRenderer>().sharedMaterial=font.material;node.GetComponent<MeshRenderer>().enabled=visible;}
   string hp=(string)labels["hpText"],shield=(string)labels["shieldText"];bool critical=(bool)labels["critical"];
   Label("ActualHpLabel",string.IsNullOrEmpty(hp)?"":(critical?"*":"")+hp,new Vector3(.16f,.22f,0));
   Label("ActualShieldLabel",string.IsNullOrEmpty(shield)?"":(critical&&string.IsNullOrEmpty(hp)?"*":"")+"["+shield+"]",new Vector3(.16f,-.22f,0));
  }
  void RefreshVisibility(){foreach(var go in bodies.Values.Concat(pulses.Values))foreach(var renderer in go.GetComponentsInChildren<Renderer>())renderer.enabled=visible&&!(decorationReduced&&renderer.name=="ShortTrail");}
  public void SetVisible(bool value){visible=value;RefreshVisibility();}
  // Upper display owner supplies overlap/warning choice; no gameplay or world-position change.
  public void SetDecorationReduced(bool value){decorationReduced=value;RefreshVisibility();}
  public void Clear(){foreach(var go in bodies.Values.Concat(pulses.Values))Release(go);bodies.Clear();kinds.Clear();pulses.Clear();Release(material);material=null;tick=-1;}
  void OnDisable()=>Clear();void OnDestroy()=>Clear();
 }
}