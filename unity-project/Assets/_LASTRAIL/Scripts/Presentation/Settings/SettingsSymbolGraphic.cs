using UnityEngine;
using UnityEngine.UI;

namespace LastRail.Presentation.Settings
{
    // Small independent symbols; no font glyph availability or baked labels required.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SettingsSymbolGraphic : MaskableGraphic
    {
        public enum Symbol { Back, Game, Sound, Display, Account, Music, Effects, Voice, Knob, Check, Down, Person }
        public Symbol Kind;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); var rect = GetPixelAdjustedRect();
            Vector2 Point(float x, float y) => new Vector2(rect.x + x * rect.width, rect.y + y * rect.height);
            void Line(float x1, float y1, float x2, float y2, float width = .065f)
            {
                var a = Point(x1, y1); var b = Point(x2, y2); var n = new Vector2(-(b - a).y, (b - a).x).normalized * Mathf.Min(rect.width, rect.height) * width / 2;
                int i = mesh.currentVertCount; mesh.AddVert(a + n, color, Vector2.zero); mesh.AddVert(b + n, color, Vector2.zero); mesh.AddVert(b - n, color, Vector2.zero); mesh.AddVert(a - n, color, Vector2.zero); mesh.AddTriangle(i, i + 1, i + 2); mesh.AddTriangle(i, i + 2, i + 3);
            }
            void Ring(float x, float y, float radius)
            { for (int i = 0; i < 32; i++) { float a = i * Mathf.PI / 16, b = (i + 1) * Mathf.PI / 16; Line(x + Mathf.Cos(a) * radius, y + Mathf.Sin(a) * radius, x + Mathf.Cos(b) * radius, y + Mathf.Sin(b) * radius); } }
            switch (Kind)
            {
                case Symbol.Down: Line(.1f,.8f,.5f,.2f,.3f);Line(.5f,.2f,.9f,.8f,.3f);break;
                case Symbol.Person:
                    // Filled head and a continuous rounded shoulder silhouette, independent of the small tab glyph.
                    mesh.AddVert(Point(.5f,.78f),color,Vector2.zero);
                    for(int i=0;i<=48;i++){float a=i*Mathf.PI/24;mesh.AddVert(Point(.5f+Mathf.Cos(a)*.21f,.78f+Mathf.Sin(a)*.19f),color,Vector2.zero);if(i>0)mesh.AddTriangle(0,i,i+1);}
                    int center=mesh.currentVertCount;mesh.AddVert(Point(.5f,.05f),color,Vector2.zero);
                    for(int i=0;i<=48;i++){float a=i*Mathf.PI/48;mesh.AddVert(Point(.5f+Mathf.Cos(a)*.5f,.05f+Mathf.Sin(a)*.5f),color,Vector2.zero);if(i>0)mesh.AddTriangle(center,center+i,center+i+1);}break;
                case Symbol.Knob:
                    mesh.AddVert(Point(.5f,.5f),color,Vector2.zero);
                    for(int i=0;i<48;i++){float a=i*Mathf.PI/24;mesh.AddVert(Point(.5f+Mathf.Cos(a)*.4f,.5f+Mathf.Sin(a)*.4f),color,Vector2.zero);}
                    for(int i=0;i<48;i++)mesh.AddTriangle(0,1+i,1+(i+1)%48);
                    Ring(.5f,.5f,.48f);break;
                case Symbol.Check: Ring(.5f,.5f,.46f);Line(.23f,.48f,.43f,.27f,.1f);Line(.43f,.27f,.78f,.72f,.1f);break;
                case Symbol.Music: Line(.36f,.25f,.36f,.87f,.1f);Line(.36f,.87f,.84f,.95f,.12f);Line(.84f,.95f,.84f,.34f,.1f);Ring(.23f,.23f,.12f);Ring(.71f,.31f,.12f);break;
                case Symbol.Effects: for(int i=0;i<9;i++){float a=i*Mathf.PI*2/9;Line(.5f+Mathf.Cos(a)*.18f,.5f+Mathf.Sin(a)*.18f,.5f+Mathf.Cos(a)*.46f,.5f+Mathf.Sin(a)*.46f,i%2==0?.09f:.05f);}Ring(.5f,.5f,.12f);break;
                case Symbol.Voice: Line(.15f,.3f,.15f,.8f);Line(.15f,.8f,.88f,.8f);Line(.88f,.8f,.88f,.3f);Line(.88f,.3f,.42f,.3f);Line(.42f,.3f,.25f,.1f);Line(.25f,.1f,.25f,.3f);Line(.3f,.56f,.72f,.56f,.1f);break;
                case Symbol.Back: Line(.65f,.83f,.3f,.5f,.12f); Line(.3f,.5f,.65f,.17f,.12f); break;
                case Symbol.Display: Line(.1f,.3f,.9f,.3f); Line(.9f,.3f,.9f,.85f); Line(.9f,.85f,.1f,.85f); Line(.1f,.85f,.1f,.3f); Line(.5f,.3f,.5f,.1f); Line(.3f,.1f,.7f,.1f); break;
                case Symbol.Account: Ring(.5f,.72f,.16f); for(int i=0;i<12;i++){float y=.15f+i*.021f;float half=Mathf.Sqrt(Mathf.Max(0,.09f-(y-.15f)*(y-.15f)));Line(.5f-half,y,.5f+half,y,.025f);} break;
                case Symbol.Sound: Line(.15f,.4f,.15f,.6f,.15f); Line(.2f,.6f,.5f,.85f); Line(.5f,.85f,.5f,.15f); Line(.5f,.15f,.2f,.4f); Line(.68f,.32f,.8f,.5f); Line(.8f,.5f,.68f,.68f); break;
                default: Ring(.5f,.5f,.23f); Ring(.5f,.5f,.075f); for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Line(.5f+Mathf.Cos(a)*.23f,.5f+Mathf.Sin(a)*.23f,.5f+Mathf.Cos(a)*.37f,.5f+Mathf.Sin(a)*.37f,.1f);} break;
            }
        }
    }
}
