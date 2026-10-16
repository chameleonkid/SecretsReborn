using UnityEngine;
using UnityEngine.UI;

namespace SecretsReborn
{
    // Every quad keeps its source aspect ratio. Corners and each middle ornament appear once;
    // only a quiet piece of the straight border is repeated to fill the remaining length.
    [AddComponentMenu("UI/Menu Frame Graphic")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MenuFrameGraphic : MaskableGraphic
    {
        [SerializeField] private Sprite source;
        [SerializeField,Min(8)] private float cornerSize=27;
        public Sprite Source => source;
        public override Texture mainTexture => source!=null ? source.texture : Texture2D.whiteTexture;
        public void Configure(Sprite sprite,float corner) { source=sprite; cornerSize=corner; raycastTarget=false; SetAllDirty(); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); if(source==null) return;
            Rect src=source.textureRect,rect=GetPixelAdjustedRect(); Vector4 border=source.border;
            if(border.x<=0 || border.y<=0 || rect.width<=0 || rect.height<=0) return;
            float scale=Mathf.Min(cornerSize/border.x,rect.width/(border.x+border.z),rect.height/(border.y+border.w));
            float left=border.x*scale,right=border.z*scale,bottom=border.y*scale,top=border.w*scale;
            Quad(mesh,new Rect(rect.x,rect.y,left,bottom),new Rect(src.x,src.y,border.x,border.y));
            Quad(mesh,new Rect(rect.xMax-right,rect.y,right,bottom),new Rect(src.xMax-border.z,src.y,border.z,border.y));
            Quad(mesh,new Rect(rect.x,rect.yMax-top,left,top),new Rect(src.x,src.yMax-border.w,border.x,border.w));
            Quad(mesh,new Rect(rect.xMax-right,rect.yMax-top,right,top),new Rect(src.xMax-border.z,src.yMax-border.w,border.z,border.w));
            Horizontal(mesh,new Rect(rect.x+left,rect.y,rect.width-left-right,bottom),new Rect(src.x+border.x,src.y,src.width-border.x-border.z,border.y),scale);
            Horizontal(mesh,new Rect(rect.x+left,rect.yMax-top,rect.width-left-right,top),new Rect(src.x+border.x,src.yMax-border.w,src.width-border.x-border.z,border.w),scale);
            Vertical(mesh,new Rect(rect.x,rect.y+bottom,left,rect.height-bottom-top),new Rect(src.x,src.y+border.y,border.x,src.height-border.y-border.w),scale);
            Vertical(mesh,new Rect(rect.xMax-right,rect.y+bottom,right,rect.height-bottom-top),new Rect(src.xMax-border.z,src.y+border.y,border.z,src.height-border.y-border.w),scale);
        }
        private void Horizontal(VertexHelper mesh,Rect target,Rect src,float scale)
        {
            float ornament=Mathf.Min(100,src.width),width=Mathf.Min(target.width,ornament*scale);
            float mid=target.center.x-width*.5f;
            var strip=new Rect(src.x+Mathf.Min(45,src.width*.1f),src.y,Mathf.Min(55,src.width*.2f),src.height);
            TileH(mesh,new Rect(target.x,target.y,mid-target.x,target.height),strip,scale);
            Quad(mesh,new Rect(mid,target.y,width,target.height),new Rect(src.center.x-width/scale*.5f,src.y,width/scale,src.height));
            TileH(mesh,new Rect(mid+width,target.y,target.xMax-mid-width,target.height),strip,scale);
        }
        private void Vertical(VertexHelper mesh,Rect target,Rect src,float scale)
        {
            float ornament=Mathf.Min(100,src.height),height=Mathf.Min(target.height,ornament*scale);
            float mid=target.center.y-height*.5f;
            var strip=new Rect(src.x,src.y+Mathf.Min(45,src.height*.1f),src.width,Mathf.Min(55,src.height*.2f));
            TileV(mesh,new Rect(target.x,target.y,target.width,mid-target.y),strip,scale);
            Quad(mesh,new Rect(target.x,mid,target.width,height),new Rect(src.x,src.center.y-height/scale*.5f,src.width,height/scale));
            TileV(mesh,new Rect(target.x,mid+height,target.width,target.yMax-mid-height),strip,scale);
        }
        private void TileH(VertexHelper mesh,Rect target,Rect src,float scale)
        { for(float x=target.x;x<target.xMax-.001f;) { float width=Mathf.Min(src.width*scale,target.xMax-x); Quad(mesh,new Rect(x,target.y,width,target.height),new Rect(src.x,src.y,width/scale,src.height)); x+=width; } }
        private void TileV(VertexHelper mesh,Rect target,Rect src,float scale)
        { for(float y=target.y;y<target.yMax-.001f;) { float height=Mathf.Min(src.height*scale,target.yMax-y); Quad(mesh,new Rect(target.x,y,target.width,height),new Rect(src.x,src.y,src.width,height/scale)); y+=height; } }
        private void Quad(VertexHelper mesh,Rect rect,Rect pixels)
        {
            if(rect.width<=0 || rect.height<=0) return;
            var texture=source.texture; int start=mesh.currentVertCount;
            float x=pixels.x/texture.width,y=pixels.y/texture.height,r=pixels.xMax/texture.width,t=pixels.yMax/texture.height;
            mesh.AddVert(new Vector3(rect.x,rect.y),color,new Vector2(x,y)); mesh.AddVert(new Vector3(rect.x,rect.yMax),color,new Vector2(x,t));
            mesh.AddVert(new Vector3(rect.xMax,rect.yMax),color,new Vector2(r,t)); mesh.AddVert(new Vector3(rect.xMax,rect.y),color,new Vector2(r,y));
            mesh.AddTriangle(start,start+1,start+2); mesh.AddTriangle(start+2,start+3,start);
        }
    }
}
