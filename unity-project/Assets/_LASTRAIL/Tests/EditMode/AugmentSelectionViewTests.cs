using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using LastRail.Presentation.AugmentSelection;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace LastRail.Tests.EditMode
{
    public sealed class AugmentSelectionViewTests
    {
        [TestCase(1)][TestCase(2)][TestCase(3)]
        public void ExplicitUnavailableSlotsBlockCommandsWithoutCompletingGrant(int unavailableCount)
        {
            var root=new GameObject("SparseCandidates",typeof(RectTransform),typeof(Canvas));
            try {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var view=AugmentSelectionView.Create(root.transform,font);int selected=0,rerolled=0;
                view.Bind(_=>selected++,(_,__)=>rerolled++,()=>{});
                view.Render(AugmentSelectionSnapshot.Parse(AugmentSelectionContractTests.Initial),false,false,"",null);
                var buttons=view.GetComponentsInChildren<Button>(true);buttons.Single(b=>b.name=="Details0").onClick.Invoke();
                var method=typeof(AugmentSelectionView).GetMethod("ShowUnavailableSlots");Assert.IsNotNull(method);
                method.Invoke(view,new object[]{Enumerable.Range(0,unavailableCount).ToArray()});
                buttons.Single(b=>b.name=="DetailSelect").onClick.Invoke();Assert.AreEqual(0,selected);
                for(int i=0;i<3;i++){buttons.Single(b=>b.name=="Select"+i).onClick.Invoke();buttons.Single(b=>b.name=="Free"+i).onClick.Invoke();}
                Assert.AreEqual(3-unavailableCount,selected);Assert.AreEqual(3-unavailableCount,rerolled);
                Assert.AreEqual(unavailableCount,view.GetComponentsInChildren<TMP_Text>().Count(t=>t.text=="선택 가능한 증강 없음"));
                Assert.IsFalse(view.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="선택 완료"||t.text=="지급 대상 없음"));
                Assert.Throws<System.Reflection.TargetInvocationException>(()=>method.Invoke(view,new object[]{new[]{2,3}}));
                Assert.AreEqual(unavailableCount,view.GetComponentsInChildren<TMP_Text>().Count(t=>t.text=="선택 가능한 증강 없음"));
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void DetailsReusePresentationAndPreserveOnCountRevisionWithoutSelecting()
        {
            var root=new GameObject("Details",typeof(RectTransform),typeof(Canvas));
            try {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var view=AugmentSelectionView.Create(root.transform,font);int selects=0,reads=0,last=-1;
                view.Bind(i=>{selects++;last=i;},(_,__)=>{},()=>{});
                Button Find(string name)=>view.GetComponentsInChildren<Button>(true).Single(b=>b.name==name);
                var initial=AugmentSelectionSnapshot.Parse(AugmentSelectionContractTests.Initial);
                view.Render(initial,false,false,"",(_,__)=>{reads++;return new AugmentCardPresentation("이름","유형","긴 설명",null);});
                Find("Details1").onClick.Invoke();Assert.That(selects,Is.Zero);
                Assert.That(Find("DetailSelect").gameObject.activeInHierarchy,Is.True);
                Find("DetailClose").onClick.Invoke();Assert.That(selects,Is.Zero);
                Find("Details1").onClick.Invoke();Assert.That(reads,Is.EqualTo(3),"Opening details must reuse the rendered catalog data");
                Find("DetailSelect").onClick.Invoke();Assert.That(selects,Is.EqualTo(1));Assert.That(last,Is.EqualTo(1));
                Find("Details1").onClick.Invoke();var changed=JObject.Parse(AugmentSelectionContractTests.Initial);changed["grantRevision"]=1;
                view.Render(AugmentSelectionSnapshot.Parse(changed.ToString()),false,false,"",null);
                Assert.That(Find("DetailSelect").gameObject.activeInHierarchy,Is.True);
                Assert.That(view.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="DetailBody").text,Is.EqualTo("긴 설명"));
                Find("DetailClose").onClick.Invoke();
                Find("Details0").onClick.Invoke();view.Unbind();Find("DetailSelect").onClick.Invoke();Assert.That(selects,Is.EqualTo(1));
            } finally {UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void HeldPointerCannotSelectReplacementOrSurviveRebind()
        {
            var root=new GameObject("Revision input",typeof(RectTransform),typeof(Canvas));var events=new GameObject("Events",typeof(EventSystem));
            try{
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var view=AugmentSelectionView.Create(root.transform,font);int selected=0;
                void Bind()=>view.Bind(_=>selected++,(_,__)=>{},()=>{});Bind();
                var json=JObject.Parse(AugmentSelectionContractTests.Initial);
                void Render()=>view.Render(AugmentSelectionSnapshot.Parse(json.ToString()),false,false,"",(_,id)=>new AugmentCardPresentation(id,"증강",id,null));Render();
                var button=view.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Select0");var pointer=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,pointerId=7};
                void Down()=>ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);
                void Up(){ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);}
                Down();json["grantRevision"]=1;json["slots"][0]["candidateId"]="replacement";Render();Up();Assert.That(selected,Is.Zero,"An old press cannot select the replacement");
                Down();Up();Assert.That(selected,Is.EqualTo(1));
                Down();view.Unbind();Bind();Render();Up();Assert.That(selected,Is.EqualTo(1),"Rebinding cancels the captured gesture");
            }finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(events);}
        }
        [Test] public void OpenDetailRefreshesOnlyChangedCandidateAndDisablesMutationWhileBusy()
        {
            var root=new GameObject("Detail revisions",typeof(RectTransform),typeof(Canvas));
            try{
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var view=AugmentSelectionView.Create(root.transform,font);int selected=0;view.Bind(_=>selected++,(_,__)=>{},()=>{});
                var json=JObject.Parse(AugmentSelectionContractTests.Initial);
                void Render(bool busy=false)=>view.Render(AugmentSelectionSnapshot.Parse(json.ToString()),busy,false,"",(_,id)=>new AugmentCardPresentation(id,"증강",id,null,string.Join("\n",Enumerable.Repeat(id,60))));
                Button Find(string name)=>view.GetComponentsInChildren<Button>(true).Single(b=>b.name==name);
                Render();Find("Details0").onClick.Invoke();var scroll=view.GetComponentInChildren<ScrollRect>();scroll.verticalNormalizedPosition=.35f;
                Render(true);Assert.IsTrue(scroll.gameObject.activeInHierarchy);Find("DetailSelect").onClick.Invoke();Assert.That(selected,Is.Zero);Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(.35f).Within(.01));
                json["grantRevision"]=1;json["slots"][1]["candidateId"]="other";Render();Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(.35f).Within(.01));
                json["grantRevision"]=2;json["slots"][0]["candidateId"]="replacement";Render();Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(1).Within(.01));
                var texts=view.GetComponentsInChildren<TMP_Text>();Assert.That(texts.Single(t=>t.name=="DetailTitle").text,Is.EqualTo("replacement"));Assert.That(texts.Single(t=>t.name=="DetailBody").text,Does.Contain("후보가 변경되었습니다."));
                Find("DetailSelect").onClick.Invoke();Assert.That(selected,Is.EqualTo(1));
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void ExtraVisibilityDoesNotConsumeOrHideFreeAndSelection()
        {
            var root=new GameObject("ExtraUnavailable",typeof(RectTransform),typeof(Canvas));
            try{
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var view=AugmentSelectionView.Create(root.transform,font);int selects=0,free=0,extra=0;
                view.Bind(_=>selects++,(_,mode)=>{if(mode=="FREE")free++;else extra++;},()=>{});
                var snapshot=AugmentSelectionSnapshot.Parse(AugmentSelectionContractTests.Initial);view.Render(snapshot,false,false,"",null);
                foreach(var button in view.GetComponentsInChildren<Button>(true)){if(button.name.StartsWith("Extra")){Assert.That(button.gameObject.activeSelf,Is.False);button.onClick.Invoke();}}
                view.GetComponentsInChildren<Button>().Single(b=>b.name=="Free0").onClick.Invoke();view.GetComponentsInChildren<Button>().Single(b=>b.name=="Select1").onClick.Invoke();
                Assert.That(free,Is.EqualTo(1));Assert.That(selects,Is.EqualTo(1));Assert.That(extra,Is.Zero);Assert.That(snapshot.Slots[0].FreeRemaining,Is.EqualTo(3));
                view.Render(snapshot,false,false,"",null,true);Assert.That(view.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Extra")),Is.EqualTo(3));
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void ConfirmedSnapshotMarksOnlySelectedCardAndLocksAllCommands()
        {
            var root=new GameObject("ConfirmedSelection",typeof(RectTransform),typeof(Canvas));
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var view=AugmentSelectionView.Create(root.transform,font);int commands=0;view.Bind(_=>commands++,(_,__)=>commands++,()=>commands++);
                var json=JObject.Parse(AugmentSelectionContractTests.Initial);json["state"]="CONFIRMED";json["selectedCandidateId"]="b";json["grantRevision"]=1;
                for(int restored=0;restored<2;restored++)
                {
                    view.Render(AugmentSelectionSnapshot.Parse(json.ToString()),false,false,"",null);
                    Assert.That(root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="Type1").text,Is.EqualTo("선택 완료"));
                    Assert.That(root.GetComponentsInChildren<TMP_Text>().Count(t=>t.text=="선택 완료"),Is.EqualTo(1));
                    foreach(var button in view.GetComponentsInChildren<Button>()){Assert.That(button.interactable,Is.False);button.onClick.Invoke();}
                    Assert.That(commands,Is.Zero);
                    view.Render(AugmentSelectionSnapshot.Parse(AugmentSelectionContractTests.Initial),false,false,"",null);
                    Assert.That(root.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="선택 완료"),Is.False);
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        sealed class Port:IAugmentSelectionPort
        {
            internal string Current=AugmentSelectionContractTests.Initial;
            internal int Writes;
            internal readonly TaskCompletionSource<string> Completion=new TaskCompletionSource<string>();
            public Task<string> ReadAsync(AugmentSelectionScope scope,CancellationToken token)=>Task.FromResult(Current);
            public Task<string> ExecuteAsync(AugmentSelectionScope scope,AugmentSelectionCommand command,CancellationToken token){Writes++;return Completion.Task;}
        }
        [UnityTest] public IEnumerator HeldSubmitAcrossRevisionRequiresReleaseBeforeNewSelection()
        {
            yield return new EnterPlayMode();
            var root=new GameObject("Submit revision",typeof(RectTransform),typeof(Canvas));
            var events=new GameObject("Submit events",typeof(EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            var originalSettings=UnityEngine.InputSystem.InputSystem.settings;var fixtureSettings=UnityEngine.Object.Instantiate(originalSettings);UnityEngine.InputSystem.InputSystem.settings=fixtureSettings;
            fixtureSettings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;fixtureSettings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            try{
                var module=events.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();module.AssignDefaultActions();yield return null;
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var view=AugmentSelectionView.Create(root.transform,font);int selected=0;view.Bind(_=>selected++,(_,__)=>{},()=>{});
                var json=JObject.Parse(AugmentSelectionContractTests.Initial);void Render()=>view.Render(AugmentSelectionSnapshot.Parse(json.ToString()),false,false,"",null);Render();
                var button=view.GetComponentsInChildren<Button>().Single(b=>b.name=="Select0");
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Enter));UnityEngine.InputSystem.InputSystem.Update();
                Assert.IsTrue(module.submit.action.IsPressed());json["grantRevision"]=1;json["slots"][0]["candidateId"]="replacement";Render();
                ExecuteEvents.Execute(button.gameObject,new BaseEventData(events.GetComponent<EventSystem>()),ExecuteEvents.submitHandler);Assert.That(selected,Is.Zero);
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());UnityEngine.InputSystem.InputSystem.Update();
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Enter));UnityEngine.InputSystem.InputSystem.Update();
                ExecuteEvents.Execute(button.gameObject,new BaseEventData(events.GetComponent<EventSystem>()),ExecuteEvents.submitHandler);Assert.That(selected,Is.EqualTo(1));
            }finally{UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(events);UnityEngine.InputSystem.InputSystem.settings=originalSettings;UnityEngine.Object.DestroyImmediate(fixtureSettings);}
            yield return new ExitPlayMode();
        }
        sealed class Journal:IAugmentSelectionJournal
        {
            string saved;
            public string Load(string key)=>saved;
            public void SaveNew(string key,string value){Assert.That(saved,Is.Null);saved=value;}
            public void Complete(string key,string expected){Assert.That(saved,Is.EqualTo(expected));saved=null;}
        }
        [UnityTest] public IEnumerator PresenterRebindUsesExistingControllerAndDisableDetaches()
        {
            yield return new EnterPlayMode();
            yield return PresenterCase();
            yield return new ExitPlayMode();
        }
        static IEnumerator PresenterCase()
        {
            var root=new GameObject("TEST_ONLY_Presenter",typeof(RectTransform),typeof(Canvas));
            var owner=new AugmentSelectionOwner("account","TEST");var port=new Port();using var controller=new AugmentSelectionController(port,new Journal(),()=>owner);
            try
            {
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");var view=AugmentSelectionView.Create(root.transform,font);
                using var presenter=new AugmentSelectionPresenter(controller,view);presenter.Bind();presenter.Bind();var open=controller.OpenAsync("run","event");while(!open.IsCompleted)yield return null;
                var free=root.GetComponentsInChildren<Button>().Single(b=>b.name=="Free0");free.onClick.Invoke();free.onClick.Invoke();Assert.That(port.Writes,Is.EqualTo(1));
                view.gameObject.SetActive(false);var next=JObject.Parse(port.Current);next["grantRevision"]=1;next["slots"][0]["candidateId"]="new";next["slots"][0]["freeRemaining"]=2;port.Current=next.ToString();port.Completion.SetResult(new JObject{{"outcome","REROLLED"},{"snapshot",next},{"approval",null}}.ToString());
                yield return null;Assert.That(controller.HasPending,Is.False);view.gameObject.SetActive(true);free.onClick.Invoke();Assert.That(port.Writes,Is.EqualTo(1));
                Assert.That(root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="FreeCount0").text,Is.EqualTo("무료 3/3"),"Detached View must not receive the late update");
                presenter.Bind();Assert.That(root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="FreeCount0").text,Is.EqualTo("무료 2/3"));
                controller.Reset();Assert.That(root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="Name0").text,Is.EqualTo("확인 필요"),"Account reset must clear the bound View immediately");
                presenter.Dispose();free.onClick.Invoke();Assert.That(port.Writes,Is.EqualTo(1));
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [UnityTest] public IEnumerator NativeRerollHitNeverSelectsAndBindingOwnsItsLifetime()
        {
            yield return new EnterPlayMode();
            var type=typeof(AugmentSelectionController).Assembly.GetType("LastRail.Presentation.AugmentSelection.AugmentSelectionView");
            Assert.That(type,Is.Not.Null,"Independent selection View missing");
            var root=new GameObject("TEST_ONLY_Selection",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
            var events=new GameObject("Events",typeof(EventSystem));var cameraObject=new GameObject("Camera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();var target=new RenderTexture(1000,1000,24);
            try
            {
                var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;((RectTransform)root.transform).sizeDelta=new Vector2(1000,1000);
                camera.orthographic=true;camera.orthographicSize=500;camera.aspect=1;camera.transform.position=new Vector3(0,0,-10);camera.targetTexture=target;
                var font=UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset");
                var view=(Component)type.GetMethod("Create").Invoke(null,new object[]{root.transform,font});
                int selections=0,rerolls=0,lastSlot=-1;string lastMode=null;Action<int> select=i=>selections++;Action<int,string> reroll=(i,m)=>{rerolls++;lastSlot=i;lastMode=m;};Action retry=()=>{};
                void Bind()=>type.GetMethod("Bind").Invoke(view,new object[]{select,reroll,retry});Bind();Bind();
                var snapshot=AugmentSelectionSnapshot.Parse(AugmentSelectionContractTests.Initial);
                type.GetMethod("Render").Invoke(view,new object[]{snapshot,false,false,"",null,false});
                void Click(string name)
                {
                    Canvas.ForceUpdateCanvases();camera.Render();var button=root.GetComponentsInChildren<Button>().Single(b=>b.name==name);var rect=(RectTransform)button.transform;
                    var pointer=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
                    var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);Assert.That(hits.Count,Is.GreaterThan(0));Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.SameAs(button));ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
                }
                Click("Free1");Assert.That(rerolls,Is.EqualTo(1));Assert.That(lastSlot,Is.EqualTo(1));Assert.That(lastMode,Is.EqualTo("FREE"));Assert.That(selections,Is.Zero);
                Click("Details2");Assert.That(selections,Is.Zero);Click("DetailClose");Assert.That(selections,Is.Zero);
                Click("Details2");Click("DetailSelect");Assert.That(selections,Is.EqualTo(1));
                type.GetMethod("Render").Invoke(view,new object[]{snapshot,true,false,"진행 중",null,false});Click("Select0");Assert.That(selections,Is.EqualTo(1));
                type.GetMethod("Unbind").Invoke(view,null);type.GetMethod("Render").Invoke(view,new object[]{snapshot,false,false,"",null,false});Click("Free1");Assert.That(rerolls,Is.EqualTo(1));
                Bind();view.gameObject.SetActive(false);view.gameObject.SetActive(true);Click("Select2");Assert.That(selections,Is.EqualTo(1));
            }
            finally{camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(events);UnityEngine.Object.DestroyImmediate(cameraObject);}
            yield return new ExitPlayMode();
        }
    }
}
