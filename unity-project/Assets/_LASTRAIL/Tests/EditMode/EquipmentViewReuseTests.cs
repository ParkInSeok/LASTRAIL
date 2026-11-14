using System.Linq;
using LastRail.Presentation.MissionShell;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class EquipmentViewReuseTests
{
    [Test]
    public void SelectionAndRefreshKeepRowsAndDetailAndRetireRemovedInputs()
    {
        var packet=JObject.Parse(System.IO.File.ReadAllText("../QA/ResourceIntake-20261110/connection/equipment-inventory/server-read-fixture.json"));
        var run=(string)packet["runId"];
        var model=EquipmentInventoryModel.Read("a",run,packet.ToString());
        var state=new EquipmentInventoryController("a",run);state.Apply(model);
        var root=new GameObject("EquipmentReuse",typeof(RectTransform));
        try
        {
            var view=EquipmentInventoryView.Open(root.transform,null,state,null);
            var row=view.transform.Find("List/Rows/Row0");
            var input=row.GetComponentInChildren<Button>();
            state.Select(model.Items[0].Id);
            Assert.AreSame(row,view.transform.Find("List/Rows/Row0"),"Selection must not recreate the list");
            var detail=view.transform.Find("Detail").GetChild(0);
            state.Back();
            Assert.AreSame(detail,view.transform.Find("Detail").GetChild(0),"Detail is a reusable view");
            view.Bind(state);input.onClick.Invoke();Assert.AreEqual(model.Items[0].Id,state.SelectedId);
            state.Invalidate();input.onClick.Invoke();Assert.IsNull(state.SelectedId);
            Assert.IsFalse(row.gameObject.activeSelf,"Unavailable rows must not accept input");
            state.Apply(model);Assert.AreSame(row,view.transform.Find("List/Rows/Row0"));
            view.Unbind();input.onClick.Invoke();Assert.IsNull(state.SelectedId);
        }
        finally{Object.DestroyImmediate(root);}
    }
}
