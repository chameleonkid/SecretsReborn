using UnityEngine;
using UnityEngine.UI;

namespace SecretsReborn
{
    public sealed class MainMenuTrashRow : MonoBehaviour
    {
        [SerializeField] private int index;
        [SerializeField] private Text details;
        [SerializeField] private Button restore;
        public void Configure(int row,Text text,Button button) { index=row; details=text; restore=button; }
        public void Render(MainMenu menu)
        {
            string data=menu.TrashDetails(index); gameObject.SetActive(data!=null);
            if(data==null) return; details.text=data; restore.interactable=menu.CanRestore(index);
        }
    }
}
