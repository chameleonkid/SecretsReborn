using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsReborn
{
    public sealed class MainMenu : MonoBehaviour
    {
        internal static string ConnectionError;
        [SerializeField] private string playerName = "Spieler", address = "127.0.0.1";
        private string message;
        public enum Page { Home, Solo, SoloCreate, Multiplayer, Host, Join, SoloLoad, HostLoad, Options, Trash, ConfirmDelete }; private Page page;
        [SerializeField] private string characterName = "Abenteurer";
        private readonly CharacterCreatorSelection creator = new CharacterCreatorSelection();
        private readonly SaveGameData[] slotSummaries = new SaveGameData[3];
        private readonly string[] slotErrors = new string[3];
        private string pendingDelete;
        private bool showTrash;
        private TrashedSave[] trash = Array.Empty<TrashedSave>();
        [SerializeField] private MainMenuHomeView home;
        [SerializeField] private MainMenuPagesView pages;
        private readonly bool[] slotExists=new bool[3];
        private string[] trashDetails=Array.Empty<string>();
        private bool[] trashAllowed=Array.Empty<bool>();
        public const int TrashRowsPerPage=4;
        public int TrashPage {get;private set;}
        public Page CurrentPage => page;
        public Page Screen => pendingDelete!=null ? Page.ConfirmDelete : showTrash ? Page.Trash : page;
        public string Message => message;
        public string PlayerName {get=>playerName;set=>playerName=value;}
        public string Address {get=>address;set=>address=value;}
        public string CharacterName {get=>characterName;set=>characterName=value;}
        public CharacterCreatorSelection Creator => creator;
        public MainMenuPagesView Pages => pages;
        public int TrashCount => trash.Length;
        public int TrashPageCount => Mathf.Max(1,Mathf.CeilToInt(trash.Length/(float)TrashRowsPerPage));
        public bool Available => NetworkCoop.Active!=null && !NetworkCoop.Active.LobbyActive && !NetworkCoop.Active.ChangingArea && !NetworkCoop.Active.JoiningSession;
        internal bool IsHome => page==Page.Home;
        public MainMenuHomeView Home => home;
        public void ConfigureHome(MainMenuHomeView view) => home=view;
        public void ConfigurePages(MainMenuPagesView view) => pages=view;
        private void Start()
        {
            if (!string.IsNullOrEmpty(ConnectionError)) { message = ConnectionError; ConnectionError = null; }
            // Returning from a lobby destroys the previous persistent session.
            // Runtime startup callbacks do not run again on a normal scene reload.
            var session = GameSession.Instance;
            if(home==null)
                foreach(var view in FindObjectsByType<MainMenuHomeView>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    if(view.gameObject.scene==gameObject.scene) { home=view; break; }
            if(home!=null) home.Bind(ChooseHome);
            else Debug.LogError("Hauptmenü-Canvas fehlt in der MainMenu-Szene. Home-Referenz im Inspector zuweisen.",this);
            if(pages==null && home!=null) pages=home.GetComponent<MainMenuPagesView>();
            if(pages!=null) pages.Bind(this);
            var args = Environment.GetCommandLineArgs();
            // Existing opt-in combat/transport smoke tests still start in the adventure.
            if (Debug.isDebugBuild && (Array.IndexOf(args, "--coop-smoke-host") >= 0 || Array.IndexOf(args, "--coop-smoke-client") >= 0
                || Array.IndexOf(args, "--book-test-host") >= 0 || Array.IndexOf(args, "--book-test-client") >= 0))
                GameSession.OpenScene(NetworkCoop.FirstScene);
        }
        public void ChangePage(Page target)
        {
            if(target==Page.Trash) { OpenTrash(); return; }
            if(target==Page.ConfirmDelete) return;
            page = target; message = null; pendingDelete = null; showTrash = false;
            if (target != Page.HostLoad && target != Page.SoloLoad) return;
            RefreshSlots();
        }
        private void ChooseHome(int index)
        {
            if(index==3) { Application.Quit(); return; }
            ChangePage(index==0 ? Page.Solo : index==1 ? Page.Multiplayer : Page.Options);
        }
        private void Update()
        {
            if(Available && page!=Page.Home && home?.Busy!=true && (UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame==true
                || UnityEngine.InputSystem.Gamepad.current?.buttonEast.wasPressedThisFrame==true))
            {
                bool dropdown=pages!=null && pages.GetComponentsInChildren<UnityEngine.UI.Dropdown>().Length>0
                    && Array.Exists(pages.GetComponentsInChildren<UnityEngine.UI.Dropdown>(),x=>x.transform.Find("Dropdown List")!=null);
                var selected=UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
                var input=selected!=null ? selected.GetComponent<UnityEngine.UI.InputField>() : null;
                if(input!=null && input.isFocused) input.DeactivateInputField();
                else if(!dropdown) Back();
            }
            if(home!=null) home.Show(Available || NetworkCoop.Active?.LobbyActive==true,message);
            if(pages!=null) pages.ShowFrontend(Available);
            if(pages!=null && Available) pages.Render(this);
        }
        public void Back()
        {
            if(pendingDelete!=null) pendingDelete=null;
            else if(showTrash) { showTrash=false; RefreshSlots(); }
            else ChangePage(page==Page.Solo || page==Page.Multiplayer || page==Page.Options ? Page.Home
                : page==Page.SoloCreate || page==Page.SoloLoad ? Page.Solo : page==Page.HostLoad ? Page.Host : Page.Multiplayer);
        }
        public string SlotFile(int index)
        {
            if(SaveGameStore.TestSlotRoot!=null) return System.IO.Path.Combine(SaveGameStore.TestSlotRoot,(page==Page.HostLoad ? "multiplayer-slot-" : "slot-")+index+".es3");
            return page==Page.HostLoad ? SaveGameStore.MultiplayerSlotPath(index) : "SecretsReborn/slot-"+(index+1)+".es3";
        }
        public string SlotDetails(int index) => "Slot "+(index+1)+"\n"+(slotErrors[index] ?? SaveSlotLabel.Details(slotSummaries[index]));
        public bool CanLoadSlot(int index) => slotSummaries[index]!=null && slotErrors[index]==null;
        public bool SlotExists(int index) => slotExists[index];
        private void RefreshSlots()
        {
            for (int i = 0; i < 3; i++)
            {
                slotSummaries[i] = null; slotErrors[i] = null;
                try { slotExists[i]=SaveGameStore.Exists(SlotFile(i)); slotSummaries[i] = SaveGameStore.ReadSummary(SlotFile(i)); }
                catch (Exception) { slotErrors[i] = "Spielstand nicht lesbar"; }
            }
        }
        public void CreateSolo() => Attempt(()=>
        {
            if(string.IsNullOrWhiteSpace(characterName)) return false;
            var p=creator.Profile(characterName); var world=new WorldSessionState(Guid.NewGuid().ToString("N"));
            world.CreateSoloProfile(characterName,p.hairColor,p.eyeColor,p.bodyStyle,p.hairStyle,p.eyeStyle); return StartSolo(world);
        });
        public void HostNew() => Attempt(()=>NetworkCoop.Active.OpenLobby(true,null,playerName));
        public void Connect() => Attempt(()=>NetworkCoop.Active.OpenLobby(false,address,playerName));
        public void LoadSlot(int index) => Attempt(()=>page==Page.HostLoad
            ? NetworkCoop.Active.OpenLobby(true,null,playerName,SaveGameStore.Load(SlotFile(index))) : StartSolo(SaveGameStore.Load(SlotFile(index))));
        public void AskDelete(int index) { if(slotExists[index]) pendingDelete=SlotFile(index); }
        public string DeletionDetails
        {
            get { for(int i=0;i<3;i++) if(SlotFile(i)==pendingDelete) return "In den Papierkorb verschieben?\n"+SlotDetails(i); return ""; }
        }
        public void CancelDelete() => pendingDelete=null;
        public void ConfirmDelete()
        {
            if(pendingDelete==null) return;
            try { SaveGameTrash.MoveToTrash(pendingDelete); pendingDelete=null; RefreshSlots(); message="In den Papierkorb verschoben."; }
            catch(Exception error) { message=error.Message; }
        }
        public void OpenTrash()
        {
            try { RefreshTrash(); showTrash=true; pendingDelete=null; TrashPage=0; } catch(Exception error) { message=error.Message; }
        }
        private void RefreshTrash()
        {
            trash=SaveGameTrash.List(); trashDetails=new string[trash.Length]; trashAllowed=new bool[trash.Length];
            for(int i=0;i<trash.Length;i++)
            {
                string details; try { details=SaveSlotLabel.Details(SaveGameTrash.Summary(trash[i].id)); } catch(Exception) { details="Nicht lesbarer Spielstand (Datei bleibt erhalten)."; }
                trashDetails[i]=trash[i].originalPath+"\n"+details; trashAllowed[i]=!SaveGameStore.Exists(trash[i].originalPath);
            }
            TrashPage=Mathf.Clamp(TrashPage,0,TrashPageCount-1);
        }
        public string TrashDetails(int row) { int i=TrashPage*TrashRowsPerPage+row; return i<trash.Length ? trashDetails[i] : null; }
        public bool CanRestore(int row) { int i=TrashPage*TrashRowsPerPage+row; return i<trash.Length && trashAllowed[i]; }
        public void TurnTrashPage(int direction) => TrashPage=Mathf.Clamp(TrashPage+direction,0,TrashPageCount-1);
        public void RestoreTrash(int row)
        {
            int i=TrashPage*TrashRowsPerPage+row; if(i<0 || i>=trash.Length) return;
            try { SaveGameTrash.Restore(trash[i].id); RefreshTrash(); RefreshSlots(); message="Wiederhergestellt."; } catch(Exception error) { message=error.Message; }
        }
        private void Attempt(Func<bool> action)
        { try { if (!action()) message = NetworkCoop.Active.Status; } catch (Exception e) { message = e.Message; } }
        private bool StartSolo(WorldSessionState world)
        {
            if (world != null && world.Multiplayer) throw new ArgumentException("Multiplayer-Spielstände bitte über die Lobby öffnen.");
            world = world ?? new WorldSessionState(Guid.NewGuid().ToString("N"));
            string scene = string.IsNullOrEmpty(world.SavedScenePath) ? NetworkCoop.FirstScene : world.SavedScenePath;
            GameSession.ValidateScene(scene);
            GameSession.Instance.BeginFrontendWorld(world);
            SceneManager.sceneLoaded += RestoreSolo;
            GameSession.OpenScene(scene); return true;
        }
        private static void RestoreSolo(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= RestoreSolo;
            foreach (var actor in FindObjectsByType<CharacterInventory>(FindObjectsSortMode.None))
            {
                actor.RefreshSession(); var position = GameSession.Instance.World.Position(actor.CharacterId);
                if (position != null && position.scenePath == scene.path) GameSession.Warp(actor, new Vector3(position.x, position.y, position.z));
            }
        }
    }
}
