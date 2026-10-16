using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecretsReborn
{
    public sealed class MainMenuPagesView : MonoBehaviour
    {
        [SerializeField] private MainMenuPageView[] pages;
        [SerializeField] private InputField characterName,hostName,joinName,address;
        [SerializeField] private Dropdown[] creatorFields;
        [SerializeField] private MenuChoiceField[] choices;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Vector2 homeSize=new Vector2(640,680);
        [SerializeField] private MenuPortraitView portrait;
        [SerializeField] private MainMenuSlotRow[] slots;
        [SerializeField] private MainMenuTrashRow[] trash;
        [SerializeField] private Text deletion,trashPage,emptyTrash;
        [SerializeField] private Button create,previousTrash,nextTrash;
        private MainMenu.Page? shown;
        private MainMenu owner;
        private int previousBody=-1,lastTrashPage=-1;
        public MainMenu.Page? Shown => shown;
        public void ConfigureLayout(RectTransform root,Vector2 size,MenuChoiceField[] fields) { panel=root; homeSize=size; choices=fields; }
        public void ShowFrontend(bool visible) { if(panel!=null && panel.gameObject.activeSelf!=visible) panel.gameObject.SetActive(visible); }
        public void Configure(MainMenuPageView[] screens,InputField hero,InputField host,InputField guest,InputField ip,Dropdown[] fields,
            MenuPortraitView preview,MainMenuSlotRow[] slotRows,MainMenuTrashRow[] trashRows,Text confirm,Text page,Text empty,Button start,Button previous,Button next)
        { pages=screens; characterName=hero; hostName=host; joinName=guest; address=ip; creatorFields=fields; portrait=preview; slots=slotRows; trash=trashRows; deletion=confirm; trashPage=page; emptyTrash=empty; create=start; previousTrash=previous; nextTrash=next; }
        public void Bind(MainMenu menu)
        {
            owner=menu;
            foreach(var button in GetComponentsInChildren<MenuGraphicButton>(true)) button.Bind(menu.Home);
            characterName.onValueChanged.AddListener(value=>menu.CharacterName=value);
            hostName.onValueChanged.AddListener(value=>menu.PlayerName=value); joinName.onValueChanged.AddListener(value=>menu.PlayerName=value);
            address.onValueChanged.AddListener(value=>menu.Address=value);
            for(int i=0;i<choices.Length;i++) { int row=i; choices[i].onValueChanged.AddListener(value=>menu.Creator.Select(row,value)); }
            Render(menu);
        }
        public void Render(MainMenu menu)
        {
            if(menu==null || pages==null) return;
            bool changed=shown!=menu.Screen; shown=menu.Screen;
            if(changed && panel!=null)
            {
                panel.sizeDelta=homeSize;
                foreach(var screen in pages) if(screen.Page==menu.Screen) panel.sizeDelta=screen.PanelSize;
            }
            menu.Home.ShowHomeContent(menu.Screen==MainMenu.Page.Home);
            foreach(var page in pages) if(page.gameObject.activeSelf!=(page.Page==menu.Screen)) page.gameObject.SetActive(page.Page==menu.Screen);
            if(menu.Screen==MainMenu.Page.SoloCreate)
            {
                Set(characterName,menu.CharacterName); create.interactable=!string.IsNullOrWhiteSpace(menu.CharacterName);
                int body=menu.Creator.Value(0);
                if(previousBody!=body) { previousBody=body; choices[1].SetOption(7,body==1 ? "Gebräunt" : "Sehr hell"); }
                for(int i=0;i<choices.Length;i++) choices[i].SetValueWithoutNotify(menu.Creator.Value(i));
                portrait.Present(menu.Creator.Profile(menu.CharacterName));
            }
            if(menu.Screen==MainMenu.Page.Host) Set(hostName,menu.PlayerName);
            if(menu.Screen==MainMenu.Page.Join) { Set(joinName,menu.PlayerName); Set(address,menu.Address); }
            if(menu.Screen==MainMenu.Page.SoloLoad || menu.Screen==MainMenu.Page.HostLoad) foreach(var row in slots) row.Render(menu);
            if(menu.Screen==MainMenu.Page.ConfirmDelete) deletion.text=menu.DeletionDetails;
            if(menu.Screen==MainMenu.Page.Trash)
            {
                foreach(var row in trash) row.Render(menu);
                emptyTrash.gameObject.SetActive(menu.TrashCount==0); trashPage.text="Seite "+(menu.TrashPage+1)+" / "+menu.TrashPageCount;
                previousTrash.interactable=menu.TrashPage>0; nextTrash.interactable=menu.TrashPage+1<menu.TrashPageCount;
                changed|=lastTrashPage!=menu.TrashPage; lastTrashPage=menu.TrashPage;
            }
            var selected=EventSystem.current?.currentSelectedGameObject;
            bool focusMissing=selected==null || !selected.activeInHierarchy || selected.GetComponent<Selectable>()?.IsInteractable()==false;
            if(menu.Screen!=MainMenu.Page.Home && (changed || (!menu.Home.PointerMode && !menu.Home.Busy && focusMissing)))
                foreach(var page in pages) if(page.Page==menu.Screen) page.Focus(changed);
        }
        private static void Set(InputField field,string text) { if(field.text!=text) field.SetTextWithoutNotify(text); }
    }
}
