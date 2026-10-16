using UnityEngine;
using UnityEngine.UI;
namespace SecretsReborn
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ConsentMarkGraphic : MaskableGraphic
    {
        [SerializeField] private bool accepted;
        public bool Accepted { get=>accepted; set { if(accepted==value) return; accepted=value; SetVerticesDirty(); } }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); Vector2 center=rectTransform.rect.center;
            if(accepted)
            {
                Line(mesh,center+new Vector2(-9,0),center+new Vector2(-2,-7),3,new Color(.35f,.92f,.48f));
                Line(mesh,center+new Vector2(-2,-7),center+new Vector2(11,9),3,new Color(.35f,.92f,.48f));
            }
            else for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;
                Line(mesh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*9,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*9,1.3f,new Color(.65f,.64f,.52f,.65f));
            }
        }
        static void Line(VertexHelper mesh,Vector2 a,Vector2 b,float width,Color tint)
        {
            Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f; int i=mesh.currentVertCount;
            mesh.AddVert(a-n,tint,Vector2.zero); mesh.AddVert(a+n,tint,Vector2.zero); mesh.AddVert(b+n,tint,Vector2.zero); mesh.AddVert(b-n,tint,Vector2.zero);
            mesh.AddTriangle(i,i+1,i+2); mesh.AddTriangle(i+2,i+3,i);
        }
    }
}
