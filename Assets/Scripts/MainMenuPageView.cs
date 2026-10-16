using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecretsReborn
{
    public sealed class MainMenuPageView : MonoBehaviour
    {
        [SerializeField] private MainMenu.Page page;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Vector2 panelSize=new Vector2(720,640);
        public MainMenu.Page Page => page;
        public Vector2 PanelSize => panelSize;
        public void ConfigureLayout(Vector2 size) { panelSize=size; scroll=null; }
        public void Configure(MainMenu.Page kind,ScrollRect body) { page=kind; scroll=body; }
        public void Focus(bool resetScroll)
        {
            Canvas.ForceUpdateCanvases(); if(resetScroll && scroll!=null) scroll.verticalNormalizedPosition=1;
            foreach(var item in GetComponentsInChildren<Selectable>()) if(item.IsInteractable() && item.navigation.mode!=Navigation.Mode.None)
            { EventSystem.current?.SetSelectedGameObject(item.gameObject); return; }
        }
    }
}
