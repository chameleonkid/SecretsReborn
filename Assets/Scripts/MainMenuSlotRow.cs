using UnityEngine;
using UnityEngine.UI;

namespace SecretsReborn
{
    public sealed class MainMenuSlotRow : MonoBehaviour
    {
        [SerializeField] private int index;
        [SerializeField] private Text details;
        [SerializeField] private Button load,remove;
        public void Configure(int row,Text text,Button loadButton,Button deleteButton) { index=row; details=text; load=loadButton; remove=deleteButton; }
        public void Render(MainMenu menu)
        { details.text=menu.SlotDetails(index); load.interactable=menu.CanLoadSlot(index); remove.interactable=menu.SlotExists(index); }
    }
}
