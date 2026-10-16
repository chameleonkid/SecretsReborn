using UnityEngine;
using UnityEngine.UI;

namespace SecretsReborn
{
    // Geometry rather than a stretched texture: line widths and corner motifs stay proportional.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class QuietMenuFrameGraphic : MaskableGraphic
    {
        [SerializeField,Min(.5f)] private float lineWidth=1.5f;
        [SerializeField,Min(8)] private float cornerLength=24;
        [SerializeField] private bool ornaments=true;
        [SerializeField,Min(0)] private float inset=5;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); Rect r=GetPixelAdjustedRect();
            Border(mesh,r,1,new Color(.34f,.27f,.12f,.9f));
            r=new Rect(r.x+inset,r.y+inset,r.width-2*inset,r.height-2*inset); Border(mesh,r,lineWidth,color);
            if(!ornaments) return;
            float c=Mathf.Min(cornerLength,Mathf.Min(r.width,r.height)/4);
            foreach(float x in new[]{r.x,r.xMax}) foreach(float y in new[]{r.y,r.yMax})
            {
                float dx=x==r.x ? 1 : -1,dy=y==r.y ? 1 : -1;
                Line(mesh,new Vector2(x,y),new Vector2(x+dx*c,y),3,color);
                Line(mesh,new Vector2(x,y),new Vector2(x,y+dy*c),3,color);
                Diamond(mesh,new Vector2(x+dx*8,y+dy*8),3,color);
            }
            foreach(float y in new[]{r.y,r.yMax}) Diamond(mesh,new Vector2(r.center.x,y),5,color);
        }
        static void Border(VertexHelper mesh,Rect r,float thickness,Color tint)
        {
            Line(mesh,new Vector2(r.x,r.y),new Vector2(r.xMax,r.y),thickness,tint);
            Line(mesh,new Vector2(r.x,r.yMax),new Vector2(r.xMax,r.yMax),thickness,tint);
            Line(mesh,new Vector2(r.x,r.y),new Vector2(r.x,r.yMax),thickness,tint);
            Line(mesh,new Vector2(r.xMax,r.y),new Vector2(r.xMax,r.yMax),thickness,tint);
        }
        static void Diamond(VertexHelper mesh,Vector2 p,float size,Color tint)
        { Quad(mesh,p+Vector2.left*size,p+Vector2.up*size,p+Vector2.right*size,p+Vector2.down*size,tint); }
        static void Line(VertexHelper mesh,Vector2 a,Vector2 b,float thickness,Color tint)
        { Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*thickness*.5f; Quad(mesh,a-n,a+n,b+n,b-n,tint); }
        static void Quad(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tint)
        {
            int i=mesh.currentVertCount;
            mesh.AddVert(a,tint,Vector2.zero); mesh.AddVert(b,tint,Vector2.zero); mesh.AddVert(c,tint,Vector2.zero); mesh.AddVert(d,tint,Vector2.zero);
            mesh.AddTriangle(i,i+1,i+2); mesh.AddTriangle(i+2,i+3,i);
        }
    }
}
