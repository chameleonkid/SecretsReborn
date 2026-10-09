using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SecretsReborn
{
    public sealed class InventoryCanvasSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] private int index;
        [SerializeField] private RawImage icon;
        [SerializeField] private Image ghost, selection, quality;
        [SerializeField] private Text amount;
        private InventoryCanvasView view;
        private Vector2 iconSize;
        private void Awake() => iconSize = icon.rectTransform.sizeDelta;
        public int Index => index;
        internal void Bind(InventoryCanvasView owner) => view = owner;
        internal void Refresh(ItemDefinition item,int count,bool selected)
        {
            selection.gameObject.SetActive(selected); quality.gameObject.SetActive(item != null);
            if (item != null) quality.color = item.QualityColor;
            ghost.gameObject.SetActive(item == null); icon.gameObject.SetActive(item?.Icon != null);
            if (item?.Icon != null) InventoryCanvasView.Sprite(icon,item.Icon,item.IconTint,item.IconContent,iconSize);
            amount.text = item != null && count != 1 ? count.ToString() : "";
        }
        public void OnPointerEnter(PointerEventData e) => view?.Hover(index);
        public void OnPointerExit(PointerEventData e) => view?.Hover(-1);
        public void OnPointerClick(PointerEventData e) { if (e.dragging) return; if (e.button == PointerEventData.InputButton.Right) view?.Activate(index); else if (e.button == PointerEventData.InputButton.Left) view?.Select(index); }
        public void OnBeginDrag(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) view?.BeginDrag(this,e.position); }
        public void OnDrag(PointerEventData e) => view?.Drag(e.position);
        public void OnEndDrag(PointerEventData e) => view?.EndDrag();
        public void OnDrop(PointerEventData e) => view?.Drop(index);
    }
}
