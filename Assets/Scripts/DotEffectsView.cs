using UnityEngine;

namespace SecretsReborn
{
    // Local presentation only. Different shapes as well as colors distinguish the types.
    [DisallowMultipleComponent] public sealed class DotEffectsView : MonoBehaviour
    {
        private readonly LineRenderer[] marks=new LineRenderer[9];
        private Material material;
        private Transform visualRoot;
        public int VisibleMask { get; private set; }
        private void Awake()
        {
            // Runtime character clones can carry already-created presentation children.
            foreach(Transform child in transform) if(child.name=="DoT visuals") { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        }
        public void Present(int mask)
        {
            VisibleMask=mask&7;
            if(mask==0) { Clear(); return; }
            if(material==null) material=new Material(Shader.Find("Sprites/Default"));
            if(visualRoot==null) { visualRoot=new GameObject("DoT visuals").transform; visualRoot.SetParent(transform,false); }
            for(int kind=0;kind<3;kind++) for(int part=0;part<3;part++)
            {
                int index=kind*3+part;
                bool active=(mask&(1<<kind))!=0;
                var mark=marks[index];
                if(!active) { if(mark!=null) mark.enabled=false; continue; }
                if(mark==null)
                {
                    var obj=new GameObject(((DotKind)kind)+" status "+part); obj.transform.SetParent(visualRoot,false);
                    mark=obj.AddComponent<LineRenderer>(); marks[index]=mark;
                    mark.sharedMaterial=material; mark.useWorldSpace=false; mark.sortingOrder=1090;
                    mark.numCapVertices=2; mark.startWidth=mark.endWidth=.035f;
                }
                mark.enabled=true;
                float phase=Mathf.Repeat(Time.time*.9f+part/3f,1);
                float x=(part-1)*.21f;
                if(kind==0)
                {
                    mark.loop=false; mark.positionCount=4;
                    mark.startColor=new Color(1,.22f,.03f,.85f); mark.endColor=new Color(1,.85f,.15f,.1f);
                    for(int p=0;p<4;p++) mark.SetPosition(p,new Vector3(x+Mathf.Sin(Time.time*7+part+p)*.045f,.05f+phase*.45f+p*.12f,-.1f));
                }
                else if(kind==1)
                {
                    mark.loop=true; mark.positionCount=10;
                    mark.startColor=mark.endColor=new Color(.35f,1,.12f,.8f);
                    float radius=.045f+phase*.035f;
                    for(int p=0;p<10;p++) { float angle=p*Mathf.PI/5; mark.SetPosition(p,new Vector3(x+Mathf.Cos(angle)*radius,.2f+phase*.7f+Mathf.Sin(angle)*radius,-.1f)); }
                }
                else
                {
                    mark.loop=false; mark.positionCount=2;
                    mark.startColor=new Color(.85f,.02f,.12f,.95f); mark.endColor=new Color(.6f,.01f,.05f,.2f);
                    mark.SetPosition(0,new Vector3(x,.65f-phase*.5f,-.1f));
                    mark.SetPosition(1,new Vector3(x-.035f,.53f-phase*.5f,-.1f));
                }
            }
        }
        public void Clear() { VisibleMask=0; foreach(var mark in marks) if(mark!=null) mark.enabled=false; }
        private void OnDisable() => Clear();
        private void OnDestroy() { if(material!=null) Destroy(material); }
    }
}
