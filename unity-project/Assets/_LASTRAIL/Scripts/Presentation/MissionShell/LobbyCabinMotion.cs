using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.MissionShell
{
    public sealed class LobbyCabinMotion : MonoBehaviour
    {
        bool reducedMotion;
        Coroutine animation;
        public bool ReducedMotion { get => reducedMotion; set { if(reducedMotion==value)return;reducedMotion=value;RefreshAnimation();Evaluate(0); } }
        void OnEnable()=>RefreshAnimation();
        void OnDisable(){if(animation!=null)StopCoroutine(animation);animation=null;}
        void RefreshAnimation(){if(animation!=null)StopCoroutine(animation);animation=null;if(isActiveAndEnabled&&!reducedMotion&&Art!=null)animation=StartCoroutine(Animate());}
        IEnumerator Animate(){while(isActiveAndEnabled&&!reducedMotion){Evaluate(Time.captureFramerate>0?Time.captureDeltaTime:Time.unscaledDeltaTime);yield return null;}animation=null;}
        public float Clock { get; private set; }
        public float InterferenceLevel { get; private set; }
        public RectTransform Art { get; private set; }
        public int InterferenceSite { get; private set; } = -1;
        readonly System.Random interferenceRandom = new System.Random(System.Guid.NewGuid().GetHashCode());
        static readonly Vector2[] Sites = {
            new Vector2(.105f,.425f), new Vector2(.113f,.484f),
            new Vector2(.529f,.44f), new Vector2(.80f,.353f),
            new Vector2(.91f,.414f), new Vector2(.95f,.478f),
            new Vector2(.19f,.420f), new Vector2(.20f,.470f),
            new Vector2(.60f,.414f), new Vector2(.62f,.439f), new Vector2(.64f,.452f)
        };
        static readonly Vector2[] RoadEnds = {
            new Vector2(.42f,.430f), new Vector2(.43f,.448f),
            new Vector2(.76f,.365f), new Vector2(.84f,.417f), new Vector2(.87f,.471f)
        };
        readonly int[] shuffledTargets = new int[11];
        int targetCursor = 11;
        public bool InterferenceIsRoad => InterferenceSite >= 6;
        int NextTarget()
        {
            if(targetCursor >= shuffledTargets.Length)
            {
                for(int i=0;i<shuffledTargets.Length;i++)shuffledTargets[i]=i;
                for(int i=shuffledTargets.Length-1;i>0;i--){int j=interferenceRandom.Next(i+1);int swap=shuffledTargets[i];shuffledTargets[i]=shuffledTargets[j];shuffledTargets[j]=swap;}
                if(shuffledTargets[0]==InterferenceSite){int swap=shuffledTargets[0];shuffledTargets[0]=shuffledTargets[1];shuffledTargets[1]=swap;}
                targetCursor=0;
            }
            return shuffledTargets[targetCursor++];
        }
        float nextBurst = -1, burstStart = -1, burstDuration;
        float RandomInterval() => 4f + (float)interferenceRandom.NextDouble()*5f;
        Material material;
        public static LobbyCabinMotion Create(Transform parent)
        {
            var root=new GameObject("LobbyEnvironment",typeof(RectTransform));root.transform.SetParent(parent,false);
            var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
            var motion=root.AddComponent<LobbyCabinMotion>();
            var art=new GameObject("CabinAndTable",typeof(RectTransform),typeof(RawImage));art.transform.SetParent(root.transform,false);
            motion.Art=(RectTransform)art.transform;
            var raw=art.GetComponent<RawImage>();raw.texture=LastRail.Resources.PreparedContent.Load<Texture2D>("LobbyDeck/cabin-landscape-v01");raw.raycastTarget=false;
            motion.material=new Material(LastRail.Resources.PreparedContent.Load<Shader>("LobbyDeck/CabinMotion"));
            raw.material=motion.material;
            motion.Evaluate(0);motion.RefreshAnimation();return motion;
        }
        public void Evaluate(float delta)
        {
            if(!ReducedMotion) Clock+=Mathf.Max(0,delta);
            var t=ReducedMotion?0:Clock;
            var area=((RectTransform)transform).rect;
            var fit=Mathf.Max(area.width/1672f,area.height/941f)*1.012f;
            Art.sizeDelta=new Vector2(1672,941)*fit;
            Art.anchoredPosition=ReducedMotion?Vector2.zero:new Vector2(Mathf.Sin(t*.77f)*1.25f,Mathf.Sin(t*1.17f)*.8f)*fit;
            Art.localRotation=Quaternion.Euler(0,0,ReducedMotion?0:Mathf.Sin(t*.65f)*.045f);
            if(nextBurst < 0) nextBurst = Clock + RandomInterval();
            if(!ReducedMotion && Clock >= nextBurst)
            {
                int chosen = NextTarget();
                InterferenceSite = chosen;
                burstStart = Clock;
                burstDuration = .1f + (float)interferenceRandom.NextDouble()*.1f;
                nextBurst = burstStart + RandomInterval();
                var center = Sites[chosen];
                material.SetVector("_InterferenceCenter",new Vector4(center.x,center.y,0,0));
                var end=InterferenceIsRoad ? RoadEnds[chosen-6] : center;
                material.SetVector("_InterferenceEnd",new Vector4(end.x,end.y,0,0));
                material.SetFloat("_InterferenceRoad",InterferenceIsRoad ? 1 : 0);
            }
            float phase = Clock-burstStart;
            InterferenceLevel = !ReducedMotion && burstStart >= 0 && phase < burstDuration
                ? Mathf.Sin((phase/burstDuration)*Mathf.PI) : 0;
            material.SetFloat("_Interference",InterferenceLevel);
            material.SetFloat("_Clock",t);material.SetFloat("_Motion",ReducedMotion?0:1);
        }
        void OnDestroy(){if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
    }
}
