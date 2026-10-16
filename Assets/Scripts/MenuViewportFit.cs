using UnityEngine;

namespace SecretsReborn
{
    [ExecuteAlways] public sealed class MenuViewportFit : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private Vector2 padding=new Vector2(24,24);
        public void Configure(RectTransform target) => panel=target;
        private void LateUpdate()
        {
            if(panel==null || !(transform.parent is RectTransform parent)) return;
            Rect safe=Screen.safeArea;
            var rect=(RectTransform)transform;
            if(Screen.width>0 && Screen.height>0)
            {
                rect.anchorMin=new Vector2(safe.x/Screen.width,safe.y/Screen.height);
                rect.anchorMax=new Vector2(safe.xMax/Screen.width,safe.yMax/Screen.height);
                rect.offsetMin=rect.offsetMax=Vector2.zero;
            }
            Vector2 available=Vector2.Scale(parent.rect.size,rect.anchorMax-rect.anchorMin)-padding*2;
            Vector2 size=Vector2.Scale(panel.rect.size,new Vector2(Mathf.Abs(panel.localScale.x),Mathf.Abs(panel.localScale.y)));
            float scale=Mathf.Clamp01(Mathf.Min(available.x/Mathf.Max(1,size.x),available.y/Mathf.Max(1,size.y)));
            rect.localScale=Vector3.one*scale;
        }
    }
}
