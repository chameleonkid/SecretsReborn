using UnityEngine;
namespace SecretsReborn
{
    public enum LobbyAction { Select,CreateOpen,CreateConfirm,Ready,Release,Start,Leave,Delete,DeleteConfirm,Cancel }
    public sealed class LobbyMenuAction : MonoBehaviour
    {
        [SerializeField] private LobbyAction action;
        [SerializeField] private int cardIndex;
        public LobbyAction Action => action;
        public int CardIndex => cardIndex;
        public void Configure(LobbyAction kind,int index=0) { action=kind; cardIndex=index; }
        public void Click() => GetComponent<MenuGraphicButton>().Confirm(Perform);
        void Perform()
        {
            var net=NetworkCoop.Active; if(net==null || !net.LobbyActive) return;
            string id=cardIndex>=0 && cardIndex<net.Lobby.characters.Length ? net.Lobby.characters[cardIndex].id : null;
            switch(action)
            {
                case LobbyAction.Select: if(id!=null) net.ChooseLobbyCharacter(id); break;
                case LobbyAction.CreateOpen: net.BeginLobbyCreator(); break;
                case LobbyAction.CreateConfirm: net.CreateLobbyCharacter(); break;
                case LobbyAction.Ready: net.ChooseLobbyCharacter(net.LobbySelection,ready:!net.LobbyReady); break;
                case LobbyAction.Release: net.ClearLobbySelection(); break;
                case LobbyAction.Start: if(net.LobbyCanStart) net.StartLobbyAdventure(); break;
                case LobbyAction.Leave: net.StopAndReload(); break;
                case LobbyAction.Delete: net.RequestLobbyDelete(id); break;
                case LobbyAction.DeleteConfirm: net.ConfirmLobbyDelete(); break;
                case LobbyAction.Cancel: net.CancelLobbyDialog(); break;
            }
        }
    }
}
