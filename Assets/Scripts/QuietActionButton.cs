using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace SecretsReborn
{
    [RequireComponent(typeof(Button))]
    public sealed class QuietActionButton : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler
    {
        Vector3 baseline; Color tint; Button button; bool hovered; float scale=1,pulse=-10;
        void Awake() { baseline=transform.localScale; button=GetComponent<Button>(); tint=button.image.color; }
        public void OnPointerEnter(PointerEventData data) => hovered=true;
        public void OnPointerExit(PointerEventData data) => hovered=false;
        public void OnPointerDown(PointerEventData data) { if(button.IsInteractable()) pulse=Time.unscaledTime; }
        void LateUpdate()
        {
            bool enabled=button.IsInteractable(); float target=enabled && (hovered || EventSystem.current?.currentSelectedGameObject==gameObject) ? 1.035f : 1;
            scale=Mathf.Lerp(scale,target,1-Mathf.Exp(-Time.unscaledDeltaTime*14));
            float t=(Time.unscaledTime-pulse)/.22f; transform.localScale=baseline*scale*(1+(t>=0 && t<=1 ? Mathf.Sin(t*Mathf.PI)*.035f : 0));
            button.image.color=tint*(enabled ? Color.white : new Color(.55f,.58f,.55f,.65f));
        }
        void OnDisable() { hovered=false; scale=1; pulse=-10; if(button!=null) { transform.localScale=baseline; button.image.color=tint; } }
    }
}
