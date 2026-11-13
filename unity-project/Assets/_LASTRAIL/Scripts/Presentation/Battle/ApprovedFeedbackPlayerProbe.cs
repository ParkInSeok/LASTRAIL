#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using LastRail.Battle;
using LastRail.Presentation.RoofBattle;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace LastRail.Presentation.Battle {
 // Explicit development-only inspection of the existing approved first Mira LOCAL source.
 // Not an operational scene, ultimate trial, full roof composition or final art.
 public sealed class ApprovedFeedbackPlayerProbe:MonoBehaviour {
  [SerializeField] TextAsset original;[SerializeField] string sourceHash;
  public string SourceHash=>sourceHash;
  RoofMiraLocalSource.State native;MiraJudgementSceneAdapter geometry;MiraFirstFeedbackController feedback;MiraFirstFeedbackView view;
  GameObject viewObject,cameraObject;JObject frame;readonly JArray checks=new JArray();string output;int exit=1;long input;bool closed;
  public void Initialize(TextAsset source,string hash){if(source==null)throw new ArgumentNullException(nameof(source));RoofMiraLocalSource.Read(JObject.Parse(source.text),hash);original=source;sourceHash=hash;}
  static void Need(bool value,string message){if(!value)throw new InvalidOperationException(message);}
  void Check(bool value,string message){Need(value,message);checks.Add(message);}
  void Awake(){
   // Explicit standalone QA must keep advancing its finite input probe when Codex regains focus.
   // This scene never enters an operating run and exits after the probe; native time remains authoritative.
   if(Application.isPlaying&&Environment.GetCommandLineArgs().Any(a=>a.StartsWith("--approved-feedback-qa-dir=",StringComparison.Ordinal))){
    Debug.Log("ApprovedFeedback QA startup focused="+Application.isFocused+" priorRunInBackground="+Application.runInBackground);
    Application.runInBackground=true;
   }
  }
  IEnumerator Start(){
   var arg=Environment.GetCommandLineArgs().FirstOrDefault(a=>a.StartsWith("--approved-feedback-qa-dir=",StringComparison.Ordinal));
   if(arg==null){Debug.LogError("Explicit D QA argument required.");yield break;}
   output=Path.GetFullPath(arg.Substring("--approved-feedback-qa-dir=".Length)).Replace('\\','/');
   if(!output.StartsWith("D:/Project/LASTRAIL/QA/",StringComparison.OrdinalIgnoreCase)){Debug.LogError("D QA output required.");Application.Quit(2);yield break;}
   Directory.CreateDirectory(output);
   if(Environment.GetCommandLineArgs().Contains("--approved-feedback-exit-only")){
    File.WriteAllText(output+"/empty-exit.json",new JObject{["scope"]="EMPTY_PLAYER_EXIT_CONTROL_NO_NATIVE_OR_FEEDBACK_OBJECTS",["focused"]=Application.isFocused,["runInBackground"]=Application.runInBackground,["nativeCreated"]=false,["quitRequested"]=true}.ToString());
    yield return null;Application.Quit(0);yield break;
   }
   var work=Run();bool failed=false;
   while(true){bool more=false;try{more=work.MoveNext();}catch(Exception error){failed=true;File.WriteAllText(output+"/failure.txt",error.ToString());Debug.LogException(error);}if(failed||!more)break;yield return work.Current;}
   (work as IDisposable)?.Dispose();Close();Application.Quit(exit);
  }
  IEnumerator Run(){
   var source=RoofMiraLocalSource.Read(JObject.Parse(original.text),sourceHash);geometry=source.CreateGeometry();native=source.CreateState(geometry,10000);
   viewObject=new GameObject("ApprovedNativeFeedback");view=viewObject.AddComponent<MiraFirstFeedbackView>();view.BindBasis(geometry.transform);
   Debug.Log("ApprovedFeedback QA original state created; focused="+Application.isFocused+" runInBackground="+Application.runInBackground);feedback=new MiraFirstFeedbackController();feedback.Bind(p=>{frame=(JObject)p.DeepClone();view.ApplyFrame(p);},view.Clear,native.Capture());
   cameraObject=new GameObject("ExplicitNativeFeedbackQaCamera");var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=4.6f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.035f,.055f);
   camera.transform.position=geometry.transform.TransformPoint(new Vector3(19,8,-5));camera.transform.LookAt(geometry.transform.TransformPoint(new Vector3(12.9f,3,3)));
   JArray Shots()=>(JArray)native.Capture()["runtime"]["miraFirstTrial"]["shots"];
   void Advance(){native.Advance("approved-feedback-player-"+(++input),source.Binding.Composition.Attack.StepTimeSpanTicks,geometry);feedback.Observe(native.Capture());if(input%25==0)Debug.Log("ApprovedFeedback QA frame="+input+" nativeTick="+native.Tick+" focused="+Application.isFocused);}
   int n=0;while(Shots().Count==0&&n++<220){Advance();yield return null;}
   Check(Shots().Count==1,"one_original_native_Mira_MAIN_fired");var shot=(JObject)Shots()[0];string target=(string)shot["targetId"];int hp=native.Hp(target);
   Check((string)shot["status"]=="FLYING","original_projectile_is_still_flying");
   var flight=native.Capture();view.SetDecorationReduced(false);yield return Capture("native-flight-full");
   view.SetDecorationReduced(true);Check(view.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Body").enabled,"core_visible_while_trail_reduced");Check(!view.GetComponentsInChildren<Renderer>().Single(r=>r.name=="ShortTrail").enabled,"decoration_reduced_separately");Check(JToken.DeepEquals(flight,native.Capture()),"decoration_does_not_change_original_native_state");yield return Capture("native-flight-reduced");view.SetDecorationReduced(false);
   n=0;while((string)Shots()[0]["status"]=="FLYING"&&n++<70){Advance();yield return null;}
   Check((string)Shots()[0]["status"]=="HIT","actual_native_contact_committed");
   var contact=((JArray)frame["pulses"]).Single(p=>(string)p["kind"]=="CONTACT");var hpReceipt=contact["receipt"]["hp"];var labels=contact["labels"];
   Check((string)labels["hpText"]==((long)hpReceipt["hpLoss"]).ToString(System.Globalization.CultureInfo.InvariantCulture),"label_equals_original_hpLoss");
   Check(hp-native.Hp(target)==(int)hpReceipt["hpLoss"],"native_hp_matches_original_contact_receipt");
   Check((string)labels["criticalState"]=="NOT_APPLICABLE","original_first_profile_critical_NA_preserved");
   Check((long)contact["targetGeneration"]==(long)Shots()[0]["targetGeneration"],"label_uses_original_target_generation");
   var text=view.GetComponentsInChildren<TextMesh>();Check(text.Length>0&&text.Any(t=>t.text==(string)labels["hpText"]),"actual_native_label_render_object_exists");
   var labelBounds=new JArray(text.Select(t=>new JObject{["text"]=t.text,["size"]=new JArray(t.GetComponent<Renderer>().bounds.size.x,t.GetComponent<Renderer>().bounds.size.y,t.GetComponent<Renderer>().bounds.size.z)}));
   native.SetPaused("approved-feedback-hold",true);feedback.Observe(native.Capture());long tick=native.Tick;var frozen=native.Capture();yield return Capture("native-contact-label");Check(native.Tick==tick&&JToken.DeepEquals(frozen,native.Capture()),"capture_hold_has_no_native_advance");
   feedback.Bind(p=>{frame=(JObject)p.DeepClone();view.ApplyFrame(p);},view.Clear,native.Capture());Check(((JArray)frame["pulses"]).Count==0&&view.GetComponentsInChildren<TextMesh>().Length==0,"rebind_does_not_replay_historical_label");
   File.WriteAllText(output+"/approved-feedback-player.json",new JObject{["status"]="Passed",["scope"]="ACTUAL_APPROVED_FIRST_MIRA_LOCAL_NATIVE_MAIN_NOT_ULT_OR_FULL_ROOF",["sourceHash"]=sourceHash,["bindingHash"]=source.Binding.OriginalHash,["screen"]=new JArray(Screen.width,Screen.height),["graphics"]=SystemInfo.graphicsDeviceType.ToString(),["checks"]=checks,["contact"]=contact.DeepClone(),["labelBounds"]=labelBounds,["sourceFactory"]="RoofMiraLocalOwnerTests.SourceEnvelope Editor-only frozen original source",["hardwarePointerInput"]=false,["ultimateHealthReadinessPlayerBound"]=false,["checkpointFileStoreExercised"]=false}.ToString());exit=0;
  }
  IEnumerator Capture(string name){
   yield return new WaitForEndOfFrame();string path=output+"/"+name+".png";ScreenCapture.CaptureScreenshot(path);
   for(int n=0;n<90&&!File.Exists(path);n++)yield return null;
   Check(File.Exists(path),"native_framebuffer_capture_"+name);
   yield return null;
  }
  void Close(){if(closed)return;closed=true;feedback?.Unbind();native?.Dispose();native=null;if(viewObject!=null)Destroy(viewObject);if(cameraObject!=null)Destroy(cameraObject);if(geometry!=null)Destroy(geometry.gameObject);}
  void OnDestroy()=>Close();
 }
}
#endif
