using UnityEngine;
using UnityEngine.InputSystem;
namespace SecretsReborn
{
    public sealed class CombatTrainingControls : MonoBehaviour
    {
        private CharacterInventory actor;
        private void Awake() => actor=GetComponent<CharacterInventory>();
        private bool Available => Debug.isDebugBuild && actor!=null && actor.LocalInput && !GameSession.Instance.Busy
            && !SaveBook.IsOpen && actor.GetComponent<SpellCaster>()?.IsCasting!=true && !SpellRingMenu.Selecting(actor);
        private void Update()
        {
            if (Available && Application.isFocused && Keyboard.current?.f5Key.wasPressedThisFrame==true)
                GameSession.Instance.SetCritTestMode(actor,!actor.ForceCritsForTesting);
        }
        private void OnGUI()
        {
            if (!Debug.isDebugBuild || actor==null || !actor.LocalInput || GameUiStyle.LocalOverlayOpen) return;
            bool previous=GUI.enabled; GUI.enabled=previous && Available;
            if (GUI.Button(new Rect(12,Screen.height-42,250,28),"F5 · 100 % Crits: "+(actor.ForceCritsForTesting ? "AN" : "AUS")))
                GameSession.Instance.SetCritTestMode(actor,!actor.ForceCritsForTesting);
            GUI.enabled=previous;
        }
    }
}
