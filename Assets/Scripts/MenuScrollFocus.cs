using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecretsReborn
{
    [RequireComponent(typeof(ScrollRect))] public sealed class MenuScrollFocus : MonoBehaviour
    {
        private ScrollRect scroll;
        private GameObject lastSelection;
        private readonly Vector3[] corners=new Vector3[4];
        private void Awake() => scroll=GetComponent<ScrollRect>();
        private void OnEnable() => lastSelection=null;
        private void LateUpdate()
        {
            var selected=EventSystem.current?.currentSelectedGameObject;
            if(selected==lastSelection) return;
            lastSelection=selected;
            if(scroll==null || selected==null || !selected.transform.IsChildOf(scroll.content) || !(selected.transform is RectTransform target)) return;
            var dropdown=selected.GetComponentInParent<Dropdown>(); if(dropdown!=null && dropdown.transform.Find("Dropdown List")!=null) return;
            target.GetWorldCorners(corners);
            float low=scroll.viewport.InverseTransformPoint(corners[0]).y,high=scroll.viewport.InverseTransformPoint(corners[1]).y;
            Rect area=scroll.viewport.rect; float shift=high>area.yMax-8 ? area.yMax-8-high : low<area.yMin+8 ? area.yMin+8-low : 0;
            if(Mathf.Abs(shift)<.1f) return;
            scroll.StopMovement(); scroll.content.anchoredPosition+=Vector2.up*shift;
        }
    }
}
