using System;
using System.IO;
using System.Linq;
using LastRail.Battle;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class OrdinaryAllyPoseReview
{
    const string Qa = "D:/Project/LASTRAIL/QA/CommanderS1-20260922/";
    static JArray V(Vector3 p) => new JArray(p.x, p.y, p.z);

    public static void Run()
    {
        var qaSource = JObject.Parse(File.ReadAllText(Qa + "StaticReference811/source.json"));
        var start = OrdinaryFiveStartInput.Read((JObject)qaSource["originalStart"], (string)qaSource["accountId"], (string)qaSource["runId"]);
        var factory = OrdinaryFiveCombatFactory.FromInitialization(start, qaSource["initializationResponse"].ToString(), 500000);
        var driver = new CombatSceneDriver(factory.Create, () => null, (state, revision) => revision + 1, () => true);
        var binding = Resources.Load<CombatPlayerAssetBinding>("CombatPresentation/OrdinaryBinding");
        using var lease = binding.Acquire(binding.ManifestHash, start.Actors.Select(a=>a.OperatorId).ToArray());
        var owner = new GameObject("OriginalOrdinaryBindingProbe", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var view = owner.AddComponent<ApprovedCombatView>();
        try
        {
            driver.Open();
            view.Bind(driver, lease.ReadAssets());
            view.UseLandscapeLayout();
            view.Refresh();
            Canvas.ForceUpdateCanvases();
            var roofs = owner.GetComponentsInChildren<RawImage>(true).Single(x=>x.name=="OrdinaryPhysicalRoof");
            var transforms = owner.GetComponentsInChildren<RectTransform>(true);
            var roles = new JArray();
            foreach (var a in start.Actors)
            {
                var actor = transforms.Single(t=>t.name==a.OperatorId && t.Find("Unit")!=null);
                var foot = actor.TransformPoint(new Vector2(75,-160));
                roles.Add(new JObject{
                    ["id"]=a.OperatorId, ["footInRoofRect"]=V(roofs.rectTransform.InverseTransformPoint(foot)),
                    ["rigCount"]=actor.GetComponentsInChildren<CombatMultipartRig>(true).Length,
                    ["colliders"]=actor.GetComponentsInChildren<Collider>(true).Length,
                    ["colliders2D"]=actor.GetComponentsInChildren<Collider2D>(true).Length});
            }
            var rig = owner.GetComponentsInChildren<CombatMultipartRig>(true).Single();
            var body = (RectTransform)rig.transform.parent;
            if(body.name!="op_mira") throw new InvalidOperationException("Unexpected active rig.");
            var sole = body.TransformPoint(new Vector2(75,-160));
            var poses = new JArray();
            foreach (float phase in new[]{0f,.25f,.5f,.75f,1f})
            {
                rig.SetExtension(phase);
                if(!rig.TrySocket("muzzle",out var muzzle,out var direction)||!rig.TrySocket("barrelBase",out var barrel,out _))
                    throw new InvalidOperationException("Product muzzle or barrel missing.");
                var offset = roofs.rectTransform.InverseTransformPoint(muzzle)-roofs.rectTransform.InverseTransformPoint(sole);
                poses.Add(new JObject{["extension"]=phase,["muzzleOffsetInRoofUI"]=V(offset),["directionInRoofUI"]=V(roofs.rectTransform.InverseTransformDirection(direction)),
                    ["muzzleInViewWorld"]=V(muzzle),["barrelInViewWorld"]=V(barrel)});
            }
            var target=Qa+"AllyGeometry846/mira-real-view-measurement.json";
            var result = new JObject{
                ["scope"]="ACTUAL_PRODUCT_VIEW_AND_SOURCE_RIG_QA_TRANSPORT_ARTICULATION_ONLY",
                ["bindingManifestHash"]=binding.ManifestHash,["unityVersion"]=Application.unityVersion,["roles"]=roles,["poses"]=poses,
                ["facing"]="product authored right-facing pose; no product facing reversal found in this branch",
                ["nativeTickDriven"]=false,["nativeWeaponGeometryRegistered"]=false,["worldDepthCalibrated"]=false
            };
            File.WriteAllText(target,result.ToString());
            Debug.Log("MIRA_REAL_VIEW_POSES="+poses.Count);
        }
        finally
        {
            view.ReleaseOwnedPresentationResources();
            Object.DestroyImmediate(owner);
            driver.Retire();
        }
    }
}
