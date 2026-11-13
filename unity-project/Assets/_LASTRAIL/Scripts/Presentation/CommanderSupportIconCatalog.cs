using UnityEngine;

namespace LastRail.Presentation
{
    // One presentation identity for commander support. Operator ultimates are separate.
    public static class CommanderSupportIconCatalog
    {
        public static bool TryGet(string id,out string resource,out Rect uv)
        {
            Rect crop;
            switch(id)
            {
                case "support_fire":resource="CommanderSupportIcons/support_fire-v01";crop=new Rect(122,259,1056,804);break;
                case "barrier":resource="CommanderSupportIcons/support_barrier-v01";crop=new Rect(106,93,1041,1009);break;
                case "emergency_repair":resource="CommanderSupportIcons/support_repair-v01";crop=new Rect(77,93,1108,1092);break;
                default:resource=null;uv=new Rect(0,0,1,1);return false;
            }
            // Alpha>=8 bounds plus24source pixels preserve the meaningful glow; PNGs
            // are unmodified copies of the approved combat art, all1254px square.
            uv=new Rect(crop.x/1254,(1254-crop.y-crop.height)/1254,crop.width/1254,crop.height/1254);return true;
        }
    }
}
