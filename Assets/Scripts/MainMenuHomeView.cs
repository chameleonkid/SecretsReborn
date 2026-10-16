using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SecretsReborn
{
    public sealed class MainMenuHomeView : MonoBehaviour
    {
        [SerializeField] private MenuGraphicButton[] buttons;
        [SerializeField] private Text status;
        [SerializeField] private GameObject homeContent;
        private Action<int> chosen;
        private int remembered;
        public bool PointerMode { get; private set; }
        public bool Busy { get; private set; }
        public MenuGraphicButton[] Buttons => buttons;
        public bool HomeContentVisible => homeContent==null || homeContent.activeInHierarchy;
        public void ConfigureContent(GameObject content) => homeContent=content;
        public void ShowHomeContent(bool visible)
        {
            if(homeContent==null || homeContent.activeSelf==visible) return;
            homeContent.SetActive(visible); if(visible) SelectRemembered();
        }
        public void Configure(MenuGraphicButton[] choices,Text message) { buttons=choices; status=message; }
        public void Bind(Action<int> action)
        {
            chosen=action;
            if(EventSystem.current==null)
            {
                Debug.LogError("EventSystem fehlt in der MainMenu-Szene.",this);
                return;
            }
            for(int i=0;i<buttons.Length;i++)
            {
                int index=i; var button=buttons[i].Button;
                buttons[i].Bind(this);
                button.onClick.AddListener(()=>Activate(index));
            }
            SelectRemembered();
        }
        public void Show(bool visible,string message)
        {
            if(status!=null) status.text=message ?? "";
            if(gameObject.activeSelf==visible) return;
            gameObject.SetActive(visible);
            if(visible) SelectRemembered();
        }
        private void SelectRemembered()
        {
            PointerMode=false; Busy=false;
            if(EventSystem.current==null) return;
            for(int offset=0;offset<buttons.Length;offset++)
            {
                var candidate=buttons[(remembered+offset)%buttons.Length];
                if(candidate.isActiveAndEnabled && candidate.Button.IsInteractable())
                { EventSystem.current.SetSelectedGameObject(candidate.gameObject); return; }
            }
            EventSystem.current.SetSelectedGameObject(null);
        }
        public void UsePointer() => PointerMode=true;
        private void Update()
        {
            var keyboard=Keyboard.current; var pad=Gamepad.current;
            if(keyboard?.upArrowKey.wasPressedThisFrame==true || keyboard?.downArrowKey.wasPressedThisFrame==true
                || keyboard?.leftArrowKey.wasPressedThisFrame==true || keyboard?.rightArrowKey.wasPressedThisFrame==true || keyboard?.escapeKey.wasPressedThisFrame==true
                || keyboard?.wKey.wasPressedThisFrame==true || keyboard?.sKey.wasPressedThisFrame==true
                || keyboard?.enterKey.wasPressedThisFrame==true || keyboard?.spaceKey.wasPressedThisFrame==true
                || keyboard?.tabKey.wasPressedThisFrame==true || pad?.dpad.ReadValue().sqrMagnitude>.1f
                || pad?.leftStick.ReadValue().sqrMagnitude>.1f || pad?.buttonSouth.wasPressedThisFrame==true || pad?.buttonEast.wasPressedThisFrame==true) PointerMode=false;
            if(HomeContentVisible && !PointerMode && !Busy && EventSystem.current!=null && EventSystem.current.currentSelectedGameObject==null) SelectRemembered();
            if(!Busy && keyboard?.tabKey.wasPressedThisFrame==true)
            {
                var choices=Array.FindAll(GetComponentsInChildren<Selectable>(),x=>x.isActiveAndEnabled && x.IsInteractable() && x.navigation.mode!=Navigation.Mode.None);
                if(choices.Length==0) return;
                int index=Array.FindIndex(choices,x=>x.gameObject==EventSystem.current.currentSelectedGameObject);
                int direction=keyboard.shiftKey.isPressed ? -1 : 1;
                EventSystem.current.SetSelectedGameObject(choices[(index+direction+choices.Length)%choices.Length].gameObject);
            }
        }
        private int SelectedIndex()
        {
            for(int i=0;i<buttons.Length;i++) if(EventSystem.current?.currentSelectedGameObject==buttons[i].gameObject) return i;
            return remembered;
        }
        private void Activate(int index)
        {
            if(Busy) return;
            remembered=index; Confirm(buttons[index],()=>chosen?.Invoke(index));
        }
        public void Confirm(MenuGraphicButton button,Action action)
        {
            if(Busy) return; Busy=true; button.Pulse(); StartCoroutine(ConfirmRoutine(button,action));
        }
        private IEnumerator ConfirmRoutine(MenuGraphicButton button,Action action)
        {
            yield return new WaitForSecondsRealtime(button.PulseDuration);
            Busy=false; action?.Invoke();
        }
        private void OnDisable()
        {
            StopAllCoroutines(); Busy=false;
            if(EventSystem.current!=null)
                foreach(var button in buttons) if(EventSystem.current.currentSelectedGameObject==button.gameObject) EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
