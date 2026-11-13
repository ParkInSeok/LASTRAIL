using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LastRail.Battle
{
    /// <summary>
    /// Editable approved judgement-model authoring, not final art or operating START.
    /// Borrowed authority tick/revision/generations must be bound by the original combat owner.
    /// Geometry is observed synchronously on Unity's main thread; no independent clock/HP.
    /// Root is the explicit coordinate basis. A basis change requires a new context/rebase.
    /// </summary>
    public sealed class MiraJudgementSceneAdapter : MonoBehaviour, IMiraFirstGeometrySource
    {
        public const string ObservationVersion=MiraJudgementGeometry.ObservationVersion;
        [SerializeField] string profileJson,profileHash,layoutJson,layoutHash;
        [SerializeField] Transform feet,muzzle;
        [SerializeField] CapsuleCollider[] targets;
        [SerializeField] BoxCollider[] bodies;
        [SerializeField] bool debugOverlay=true;
        Func<string,long> generations;
        public static MiraJudgementSceneAdapter Create(JObject profile,string independentProfileHash,JObject layout,string independentLayoutHash)
        {
            if(profile==null || layout==null || BDirectAttackInput.Digest(profile)!=independentProfileHash
                || BDirectAttackInput.Digest(layout)!=independentLayoutHash
                || (string)profile["version"]!="mira.judgement-authoring.v1.20261015"
                || (string)profile["provenance"]!="APPROVED_FIRST_TEST_JUDGEMENT_MODEL_NOT_ART_MEASUREMENT"
                || (long?)profile["millimetresPerUnityUnit"]!=1000
                || (string)layout["version"]!="mira.judgement-layout.v1.20261015"
                || (string)layout["provenance"]!="EXPLICIT_ISOLATED_FIRST_TEST_LAYOUT_NOT_OWNED_PLACEMENT")
                throw new ArgumentException("Mira judgement profile/layout pin.");
            var carRows=Array(layout["cars"]);var targetRows=Array(layout["targets"]);
            if(carRows.Count==0 || targetRows.Count==0)throw new ArgumentException("Supplied carriage/target instances required.");
            var root=new GameObject("MiraApprovedJudgementV1");
            try
            {
                var adapter=root.AddComponent<MiraJudgementSceneAdapter>();
                adapter.profileJson=profile.ToString(Newtonsoft.Json.Formatting.None);adapter.profileHash=independentProfileHash;
                adapter.layoutJson=layout.ToString(Newtonsoft.Json.Formatting.None);adapter.layoutHash=independentLayoutHash;
                var cars=new Dictionary<int,Transform>();var ids=new HashSet<string>(StringComparer.Ordinal);
                var boxList=new List<BoxCollider>();var capsuleList=new List<CapsuleCollider>();
                foreach(var row in carRows)
                {
                    string id=Id(row["id"]);int slot=checked((int)Natural(row["slot"]));
                    if(!ids.Add(id) || cars.ContainsKey(slot))throw new ArgumentException("Duplicate judgement carriage.");
                    var go=Child(root.transform,id,Metres(row["centerMm"]));
                    var box=go.AddComponent<BoxCollider>();box.center=Metres(profile["carriage"]["centerMm"]);box.size=Metres(profile["carriage"]["sizeMm"]);
                    cars.Add(slot,go.transform);boxList.Add(box);
                }
                var source=Object(layout["source"]);
                if(!cars.TryGetValue(checked((int)Natural(source["slot"])),out var sourceCar))throw new ArgumentException("Source carriage missing.");
                var actor=Child(root.transform,"MiraFeet",Metres(source["feetMm"]));
                actor.transform.localRotation=Quaternion.Euler(0,-(float)source["yawDegrees"],0);
                actor.transform.SetParent(sourceCar,true);adapter.feet=actor.transform;
                adapter.muzzle=Child(actor.transform,"JudgementMuzzle_NotArtSocket",Metres(profile["muzzleMm"])).transform;
                foreach(var row in targetRows)
                {
                    string id=Id(row["id"]);
                    if(!ids.Add(id) || !cars.TryGetValue(checked((int)Natural(row["slot"])),out var car))throw new ArgumentException("Target carriage/instance invalid.");
                    var shape=Array(profile["targets"]).SingleOrDefault(s=>(string)s["definitionId"]==(string)row["definitionId"])
                        ?? throw new ArgumentException("Unapproved target shape.");
                    var go=Child(root.transform,id,Metres(row["feetMm"]));go.transform.SetParent(car,true);
                    var capsule=go.AddComponent<CapsuleCollider>();capsule.direction=1;
                    capsule.radius=(float)(long)shape["radiusMm"]/1000f;capsule.height=(float)(long)shape["heightMm"]/1000f;capsule.center=Metres(shape["centerMm"]);
                    capsuleList.Add(capsule);
                }
                adapter.targets=capsuleList.ToArray();adapter.bodies=boxList.ToArray();
                adapter.Observe(0,1);return adapter;
            }
            catch { DestroyImmediate(root);throw; }
        }
        public JObject CaptureAuthoring()=>new JObject{["profile"]=JObject.Parse(profileJson),["profileHash"]=profileHash,["layout"]=JObject.Parse(layoutJson),["layoutHash"]=layoutHash};
        public const string NativeStateVersion="mira.judgement-native-state.v2.20261015";
        public void AddApprovedTestBox(string id,long xMm,long zMm)
        {
            Id(id);
            if(GetComponentsInChildren<Transform>(true).Any(t=>t.name==id)
                || Math.Abs((decimal)xMm)>1000000000 || Math.Abs((decimal)zMm)>1000000000)
                throw new ArgumentException("Test box identity/coordinate.");
            var profile=JObject.Parse(profileJson);
            var box=Child(transform,id,new Vector3(xMm/1000f,0,zMm/1000f)).AddComponent<BoxCollider>();
            box.center=new Vector3(0,(long)profile["testBox"]["centerYmm"]/1000f,0);
            box.size=Metres(profile["testBox"]["sizeMm"]);
            bodies=bodies.Concat(new[]{box}).ToArray();
        }
        Transform[] StateNodes()=>bodies.Select(b=>b.transform).Concat(new[]{feet}).Concat(targets.Select(t=>t.transform)).ToArray();
        public JObject CaptureNativeState(long tick,long revision)
        {
            var observation=Observe(tick,revision);
            return MiraJudgementWire.Capture(new JObject {
                ["version"]=NativeStateVersion,["observation"]=observation,
                ["poses"]=new JArray(StateNodes().Select(n=>{
                    var q=n.localRotation;
                    return new JObject {["id"]=n.name,["positionNativeLocalUnits"]=new JArray(n.localPosition.x,n.localPosition.y,n.localPosition.z),["rotation"]=new JArray(q.x,q.y,q.z,q.w)};
                }))
            });
        }
        /// <summary>
        /// Native geometry only. Original authority must restore generations first.
        /// Validate in a temporary scene root before publishing poses on the live main-thread root.
        /// Root/basis, shape identities and immutable profile are not replaced by the snapshot.
        /// </summary>
        public void RestoreNativeState(JObject state,string independentlyPinnedStateHash)
        {
            if(state==null || state.Count!=3 || (string)state["version"]!=NativeStateVersion
                || state["observation"] is not JObject saved || state["poses"] is not JArray
                || BDirectAttackInput.Digest(state)!=independentlyPinnedStateHash)
                throw new ArgumentException("Native geometry state version/pin.");
            state=MiraJudgementWire.Capture(state);saved=(JObject)state["observation"];
            long tick=Natural(saved["tick"]),revision=Natural(saved["revision"]);
            var current=Observe(tick,revision);
            if((string)saved["profileHash"]!=profileHash || (string)saved["layoutHash"]!=layoutHash
                || (string)saved["basisHash"]!=(string)current["basisHash"])
                throw new ArgumentException("Native geometry original context/basis.");
            MiraJudgementSceneAdapter staging=null;
            try
            {
                staging=Create(JObject.Parse(profileJson),profileHash,JObject.Parse(layoutJson),layoutHash);
                staging.transform.SetPositionAndRotation(transform.position,transform.rotation);
                foreach(var box in bodies.Skip(((JArray)JObject.Parse(layoutJson)["cars"]).Count))
                    staging.AddApprovedTestBox(box.name,0,0);
                if(generations!=null)staging.BindGenerations(generations);
                staging.ApplyPoses((JArray)state["poses"]);
                if(!JToken.DeepEquals(staging.CaptureNativeState(tick,revision),state))
                    throw new ArgumentException("Native pose/observation replay or original generation mismatch.");
                // All identities, dimensions, generations and resulting observations were checked.
                // No callback/event or authority ownership is transferred during this publication.
                ApplyPoses((JArray)state["poses"]);
            }
            catch(InvalidOperationException e) { throw new ArgumentException("Unsupported restored geometry.",e); }
            finally {if(staging!=null)DestroyImmediate(staging.gameObject);}
        }
        void ApplyPoses(JArray poses)
        {
            var nodes=StateNodes();
            if(poses.Count!=nodes.Length || nodes.Select(n=>n.name).Distinct(StringComparer.Ordinal).Count()!=nodes.Length)
                throw new ArgumentException("Native pose set.");
            var indexed=nodes.ToDictionary(n=>n.name,StringComparer.Ordinal);
            var staged=new List<(Transform node,Vector3 position,Quaternion rotation)>();
            var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var row in poses)
            {
                if(row is not JObject p || p.Count!=3 || !indexed.TryGetValue(Id(row["id"]),out var node) || !seen.Add(node.name))
                    throw new ArgumentException("Native pose identity.");
                var point=Array(row["positionNativeLocalUnits"]);
                if(point.Count!=3 || point.Any(v=>(v.Type!=JTokenType.Float && v.Type!=JTokenType.Integer)
                    || double.IsNaN((double)v) || double.IsInfinity((double)v) || Math.Abs((double)v)>1000000))
                    throw new ArgumentException("Native local pose coordinate.");
                var a=Array(row["rotation"]);
                if(a.Count!=4 || a.Any(v=>(v.Type!=JTokenType.Float && v.Type!=JTokenType.Integer)
                    || double.IsNaN((double)v) || double.IsInfinity((double)v) || Math.Abs((double)v)>1))
                    throw new ArgumentException("Native pose rotation.");
                var q=new Quaternion((float)a[0],(float)a[1],(float)a[2],(float)a[3]);
                if(Mathf.Abs(q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w-1)>0.00001f)
                    throw new ArgumentException("Native pose quaternion.");
                staged.Add((node,new Vector3((float)point[0],(float)point[1],(float)point[2]),q));
            }
            // Preserve native local float precision; mm conversion belongs only to Observe.
            foreach(var entry in staged.OrderBy(e=>e.node.parent==transform?0:1))
                {entry.node.localPosition=entry.position;entry.node.localRotation=entry.rotation;}
        }
        public void BindGenerations(Func<string,long> authority) => generations=authority??throw new ArgumentNullException(nameof(authority));
        public void UnbindGenerations() => generations=null;
        void OnDestroy()=>UnbindGenerations();
        public void SetDebugOverlay(bool value)=>debugOverlay=value;
        public JObject Observe(long tick,long revision)
        {
            if(tick<0 || revision<1)throw new ArgumentOutOfRangeException("tick/revision");
            var profile=JObject.Parse(profileJson);var layout=JObject.Parse(layoutJson);
            if(BDirectAttackInput.Digest(profile)!=profileHash || BDirectAttackInput.Digest(layout)!=layoutHash)
                throw new InvalidOperationException("Judgement authoring source changed.");
            ValidateHierarchy(layout);
            UnitScale(transform);UnitScale(feet);UnitScale(muzzle);
            if(muzzle.parent!=feet || !muzzle.localPosition.Equals(Metres(profile["muzzleMm"]))
                || Vector3.Distance(transform.InverseTransformDirection(feet.up),Vector3.up)>0.00001f)
                throw new InvalidOperationException("Approved socket/yaw basis changed.");
            var source=Object(layout["source"]);
            var sourceRow=new JObject {
                ["id"]=source["id"].DeepClone(),["generation"]=Generation((string)source["id"],source),
                ["slot"]=source["slot"].DeepClone(),["domain"]=source["domain"].DeepClone(),
                ["feetMm"]=Point(feet.position),["muzzleMm"]=Point(muzzle.position)
            };
            var rows=new JArray();
            for(int i=0;i<targets.Length;i++)
            {
                var capsule=targets[i];var row=layout["targets"][i];
                if(capsule==null || !capsule.enabled || !capsule.gameObject.activeInHierarchy)throw new InvalidOperationException("Observed target shape missing/disabled.");
                UnitScale(capsule.transform);
                var up=transform.InverseTransformDirection(capsule.transform.up);
                if(Vector3.Distance(up,Vector3.up)>0.00001f || capsule.direction!=1)throw new InvalidOperationException("First judgement profile requires upright capsules.");
                var shape=Array(profile["targets"]).Single(s=>(string)s["definitionId"]==(string)row["definitionId"]);
                long radius=Mm(capsule.radius),height=Mm(capsule.height);
                if(radius!=(long)shape["radiusMm"] || height!=(long)shape["heightMm"] || !JToken.DeepEquals(LocalPoint(capsule.center),shape["centerMm"]))
                    throw new InvalidOperationException("Capsule dimensions changed without a new approved source.");
                rows.Add(new JObject {
                    ["id"]=row["id"].DeepClone(),["definitionId"]=row["definitionId"].DeepClone(),
                    ["generation"]=Generation((string)row["id"],Object(row)),["slot"]=row["slot"].DeepClone(),["domain"]=row["domain"].DeepClone(),
                    ["eligible"]=row["eligible"].DeepClone(),["feetMm"]=Point(capsule.transform.position),
                    ["anchorMm"]=Point(capsule.transform.TransformPoint(Metres(shape["hitAnchorMm"]))),
                    ["heightMm"]=height,["radiusMm"]=radius,["shapeVersion"]=profile["version"].DeepClone()
                });
            }
            var boxes=new JArray();
            foreach(var box in bodies)
            {
                if(box==null || !box.enabled || !box.gameObject.activeInHierarchy)throw new InvalidOperationException("Observed solid missing/disabled.");
                UnitScale(box.transform);
                bool isCar=Array(layout["cars"]).Any(c=>(string)c["id"]==box.name);
                var expectedSize=isCar?profile["carriage"]["sizeMm"]:profile["testBox"]["sizeMm"];
                var expectedCenter=isCar?profile["carriage"]["centerMm"]:new JArray(0,(long)profile["testBox"]["centerYmm"],0);
                if(!JToken.DeepEquals(LocalPoint(box.size),expectedSize) || !JToken.DeepEquals(LocalPoint(box.center),expectedCenter))
                    throw new InvalidOperationException("Approved solid dimensions changed.");
                foreach(var axis in new[]{box.transform.right,box.transform.up,box.transform.forward})
                {
                    var local=transform.InverseTransformDirection(axis);
                    if(Mathf.Max(Mathf.Abs(local.x),Mathf.Abs(local.y),Mathf.Abs(local.z))<0.999999f)
                        throw new InvalidOperationException("Unsupported oblique box; do not silently expand to a world AABB.");
                }
                var corners=new List<Vector3>();
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                    corners.Add(transform.InverseTransformPoint(box.transform.TransformPoint(box.center+Vector3.Scale(box.size*.5f,new Vector3(x,y,z)))));
                Vector3 lo=corners.Aggregate(Vector3.Min),hi=corners.Aggregate(Vector3.Max);
                boxes.Add(new JObject { ["id"]=box.name,["centerMm"]=LocalPoint((lo+hi)*.5f),["sizeMm"]=LocalPoint(hi-lo),["shapeVersion"]=profile["version"].DeepClone() });
            }
            var basis=new JObject { ["positionMm"]=LocalPoint(transform.position),["rotation"]=new JArray(transform.rotation.x,transform.rotation.y,transform.rotation.z,transform.rotation.w) };
            return new JObject {
                ["version"]=ObservationVersion,["provenance"]=profile["provenance"].DeepClone(),
                ["profileHash"]=profileHash,["layoutHash"]=layoutHash,["profile"]=profile,
                ["basis"]=basis,["basisHash"]=BDirectAttackInput.Digest(basis),["tick"]=tick,["revision"]=revision,
                ["source"]=sourceRow,["targets"]=rows,["blockers"]=boxes
            };
        }
        void ValidateHierarchy(JObject layout)
        {
            var cars=(JArray)layout["cars"];var rows=(JArray)layout["targets"];
            if(feet==null || muzzle==null || bodies==null || targets==null || bodies.Length<cars.Count || targets.Length!=rows.Count)
                throw new InvalidOperationException("Native authoring nodes missing.");
            var bySlot=new Dictionary<long,Transform>();
            for(int i=0;i<cars.Count;i++)
            {
                var node=bodies[i];
                if(node==null || node.name!=(string)cars[i]["id"] || node.transform.parent!=transform)
                    throw new InvalidOperationException("Original carriage hierarchy changed.");
                bySlot.Add((long)cars[i]["slot"],node.transform);
            }
            if(feet.name!="MiraFeet" || feet.parent!=bySlot[(long)layout["source"]["slot"]]
                || muzzle.name!="JudgementMuzzle_NotArtSocket" || muzzle.parent!=feet)
                throw new InvalidOperationException("Original source hierarchy changed.");
            for(int i=0;i<targets.Length;i++)
                if(targets[i]==null || targets[i].name!=(string)rows[i]["id"]
                    || targets[i].transform.parent!=bySlot[(long)rows[i]["slot"]])
                    throw new InvalidOperationException("Original target hierarchy changed.");
            foreach(var box in bodies.Skip(cars.Count))
                if(box==null || box.transform.parent!=transform)
                    throw new InvalidOperationException("Original test solid hierarchy changed.");
        }        long Generation(string id,JObject row)
        {
            long value=generations==null?Natural(row["generation"]):generations(id);
            if(value<0)throw new InvalidOperationException("Authority generation invalid.");return value;
        }
        static GameObject Child(Transform parent,string name,Vector3 local)
        { var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=local;return go; }
        static void UnitScale(Transform value)
        { if(Vector3.Distance(value.lossyScale,Vector3.one)>0.00001f)throw new InvalidOperationException("Explicit millimetre geometry requires unit scale."); }
        JArray Point(Vector3 world)=>LocalPoint(transform.InverseTransformPoint(world));
        static JArray LocalPoint(Vector3 value)=>new JArray(Mm(value.x),Mm(value.y),Mm(value.z));
        static long Mm(float value)
        { if(float.IsNaN(value)||float.IsInfinity(value)||Math.Abs((double)value)>1000000)throw new ArgumentException("Geometry coordinate range.");return checked((long)Math.Round((double)value*1000,MidpointRounding.AwayFromZero)); }
        static Vector3 Metres(JToken p)
        { var a=Array(p);if(a.Count!=3)throw new ArgumentException("mm point");return new Vector3((float)(long)a[0]/1000f,(float)(long)a[1]/1000f,(float)(long)a[2]/1000f); }
        static string Id(JToken p)=>p?.Type==JTokenType.String&&!string.IsNullOrWhiteSpace((string)p)?(string)p:throw new ArgumentException("Geometry instance id.");
        static long Natural(JToken p)=>p?.Type==JTokenType.Integer&&(long)p>=0?(long)p:throw new ArgumentException("Geometry integer.");
        static JObject Object(JToken p)=>p as JObject??throw new ArgumentException("Geometry object.");
        static JArray Array(JToken p)=>p as JArray??throw new ArgumentException("Geometry array.");
        void OnDrawGizmos()
        {
            if(!debugOverlay || feet==null || muzzle==null)return;
            Gizmos.color=Color.yellow;Gizmos.DrawWireSphere(muzzle.position,.06f);Gizmos.DrawLine(feet.position,muzzle.position);
            if(bodies!=null)foreach(var box in bodies){if(box==null)continue;Gizmos.color=new Color(0,1,1,.4f);Gizmos.matrix=box.transform.localToWorldMatrix;Gizmos.DrawWireCube(box.center,box.size);}
            Gizmos.matrix=Matrix4x4.identity;
        }
    }
}
