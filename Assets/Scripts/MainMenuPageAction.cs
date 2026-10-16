using UnityEngine;

namespace SecretsReborn
{
    public enum FrontendAction { Navigate,Back,CreateSolo,HostNew,Connect,LoadSlot,AskDelete,ConfirmDelete,CancelDelete,OpenTrash,RestoreTrash,PreviousTrash,NextTrash }
    public sealed class MainMenuPageAction : MonoBehaviour
    {
        [SerializeField] private MainMenu menu;
        [SerializeField] private FrontendAction action;
        [SerializeField] private MainMenu.Page target;
        [SerializeField] private int index;
        public FrontendAction Action => action;
        public MainMenu.Page Target => target;
        public int Index => index;
        public void Configure(MainMenu owner,FrontendAction kind,MainMenu.Page page=MainMenu.Page.Home,int row=0)
        { menu=owner; action=kind; target=page; index=row; }
        public void Click()
        {
            if(menu==null || !menu.Available) return;
            GetComponent<MenuGraphicButton>().Confirm(Perform);
        }
        private void Perform()
        {
            switch(action)
            {
                case FrontendAction.Navigate: menu.ChangePage(target); break;
                case FrontendAction.Back: menu.Back(); break;
                case FrontendAction.CreateSolo: menu.CreateSolo(); break;
                case FrontendAction.HostNew: menu.HostNew(); break;
                case FrontendAction.Connect: menu.Connect(); break;
                case FrontendAction.LoadSlot: if(index>=0 && index<3 && menu.CanLoadSlot(index)) menu.LoadSlot(index); break;
                case FrontendAction.AskDelete: if(index>=0 && index<3) menu.AskDelete(index); break;
                case FrontendAction.ConfirmDelete: menu.ConfirmDelete(); break;
                case FrontendAction.CancelDelete: menu.CancelDelete(); break;
                case FrontendAction.OpenTrash: menu.OpenTrash(); break;
                case FrontendAction.RestoreTrash: menu.RestoreTrash(index); break;
                case FrontendAction.PreviousTrash: menu.TurnTrashPage(-1); break;
                case FrontendAction.NextTrash: menu.TurnTrashPage(1); break;
            }
        }
    }
}
