using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecretsReborn
{
    [RequireComponent(typeof(Button))]
    public sealed class MenuGraphicButton : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerMoveHandler
    {
        private MainMenuHomeView owner;
        private Image artwork;
        [SerializeField,Range(1,1.25f)] private float selectedScale=1.055f;
        [SerializeField,Min(.01f)] private float selectionSmoothTime=.18f;
        [SerializeField,Range(0,.25f)] private float pulseAmount=.08f;
        [SerializeField,Range(.05f,1)] private float pulseSeconds=.32f;
        private Vector3 baseScale=Vector3.one;
        private Color baseColor=Color.white;
        private float scale=1,velocity,pulseStart=-10;
        private bool hovered;
        public Button Button => GetComponent<Button>();
        public float PulseDuration => Mathf.Max(.05f,pulseSeconds);
        private void Awake() { baseScale=transform.localScale; artwork=GetComponent<Image>(); if(artwork!=null) baseColor=artwork.color; }
        public void Bind(MainMenuHomeView view) { owner=view; artwork=GetComponent<Image>(); }
        public void OnPointerEnter(PointerEventData data)
        {
            hovered=true; owner.UsePointer();
            if(Button.interactable && EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(gameObject,data);
        }
        public void OnPointerMove(PointerEventData data) => OnPointerEnter(data);
        public void OnPointerExit(PointerEventData data) => hovered=false;
        public void Pulse() => pulseStart=Time.unscaledTime;
        public void Confirm(System.Action action) { if(owner!=null) owner.Confirm(this,action); }
        private void LateUpdate()
        {
            if(owner==null) return;
            bool selected=EventSystem.current!=null && EventSystem.current.currentSelectedGameObject==gameObject
                && (!owner.PointerMode || hovered);
            bool enabled=Button.IsInteractable(); selected &=enabled;
            scale=Mathf.SmoothDamp(scale,selected ? selectedScale : 1,ref velocity,Mathf.Max(.01f,selectionSmoothTime),Mathf.Infinity,Time.unscaledDeltaTime);
            float phase=(Time.unscaledTime-pulseStart)/PulseDuration;
            float pulse=phase>=0 && phase<=1 ? 1+pulseAmount*Mathf.Sin(phase*Mathf.PI) : 1;
            transform.localScale=baseScale*(scale*pulse);
            if(artwork!=null) artwork.color=baseColor*(enabled ? Color.Lerp(new Color(.86f,.9f,.88f),Color.white,Mathf.InverseLerp(1,selectedScale,scale)) : new Color(.5f,.56f,.52f,.65f));
        }
        private void OnDisable() { hovered=false; scale=1; velocity=0; pulseStart=-10; transform.localScale=baseScale; if(artwork!=null) artwork.color=baseColor; }
    }
}
