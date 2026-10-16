using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace SecretsReborn
{
    public sealed class LobbyCanvasView : MonoBehaviour
    {
        public static LobbyCanvasView Active { get; private set; }
        [SerializeField] private MainMenuHomeView input;
        [SerializeField] private GameObject panel,selection,creator,confirmation;
        [SerializeField] private LobbyCharacterCard[] cards;
        [SerializeField] private Text heading,status,players,deleteDetails;
        [SerializeField] private InputField characterName;
        [SerializeField] private MenuChoiceField[] fields;
        [SerializeField] private MenuPortraitView portrait;
        [SerializeField] private Button ready,release,start,leave,create;
        [SerializeField] private Sprite readyArt,notReadyArt,startArt,hostStartsArt;
        private int shown=-1,body=-1;
        public GameObject Panel => panel;
        public GameObject CurrentPage => shown==1 ? creator : shown==2 ? confirmation : selection;
        public bool Visible => panel!=null && panel.activeInHierarchy;
        void Awake() => Active=this;
        void OnDestroy() { if(Active==this) Active=null; }
        public void Configure(MainMenuHomeView owner,GameObject root,GameObject lobby,GameObject creation,GameObject confirm,LobbyCharacterCard[] slots,
            Text title,Text message,Text roster,Text deletion,InputField name,MenuChoiceField[] choices,MenuPortraitView preview,
            Button readyButton,Button freeButton,Button startButton,Button leaveButton,Button createButton,Sprite readyImage,Sprite notReadyImage,Sprite startImage,Sprite hostImage)
        { input=owner; panel=root; selection=lobby; creator=creation; confirmation=confirm; cards=slots; heading=title; status=message; players=roster; deleteDetails=deletion; characterName=name; fields=choices; portrait=preview; ready=readyButton; release=freeButton; start=startButton; leave=leaveButton; create=createButton; readyArt=readyImage; notReadyArt=notReadyImage; startArt=startImage; hostStartsArt=hostImage; }
        void Start()
        {
            foreach(var button in panel.GetComponentsInChildren<MenuGraphicButton>(true)) button.Bind(input);
            characterName.onValueChanged.AddListener(value=> { if(NetworkCoop.Active!=null) NetworkCoop.Active.LobbyCharacterName=value; });
            for(int i=0;i<fields.Length;i++) { int row=i; fields[i].onValueChanged.AddListener(value=>NetworkCoop.Active?.LobbyCreator.Select(row,value)); }
        }
        void Update()
        {
            var net=NetworkCoop.Active; bool active=net!=null && net.LobbyActive;
            if(panel.activeSelf!=active) panel.SetActive(active);
            if(!active) { shown=-1; return; }
            int page=net.LobbyCreating ? 1 : net.LobbyPendingDeletion!=null ? 2 : 0; bool changed=shown!=page; shown=page;
            selection.SetActive(page==0); creator.SetActive(page==1); confirmation.SetActive(page==2);
            heading.text=page==1 ? "DEINE NEUE FIGUR" : page==2 ? "FIGUR LÖSCHEN?" : "GEMEINSAM INS ABENTEUER";
            status.text=net.Status;
            if(page==0)
            {
                foreach(var card in cards) card.Render(net);
                players.text="Verbundene Spieler: "+net.Lobby.players.Length+" / 4 · Figuren gehören dieser Host-Welt";
                ready.interactable=release.interactable=net.LobbyConnectedLocally && !string.IsNullOrEmpty(net.LobbySelection);
                ready.image.sprite=net.LobbyReady ? notReadyArt : readyArt;
                start.interactable=net.LobbyCanStart; start.image.sprite=net.LobbyIsHost ? startArt : hostStartsArt;
            }
            else if(page==1)
            {
                if(characterName.text!=net.LobbyCharacterName) characterName.SetTextWithoutNotify(net.LobbyCharacterName);
                int currentBody=net.LobbyCreator.Value(0);
                if(body!=currentBody) { body=currentBody; fields[1].SetOption(7,body==1 ? "Gebräunt" : "Sehr hell"); }
                for(int i=0;i<fields.Length;i++) fields[i].SetValueWithoutNotify(net.LobbyCreator.Value(i));
                portrait.Present(net.LobbyCreator.Profile(net.LobbyCharacterName));
                create.interactable=net.LobbyConnectedLocally && !string.IsNullOrWhiteSpace(net.LobbyCharacterName) && string.IsNullOrEmpty(net.LobbySelection) && net.Lobby.characters.Length<4;
            }
            else
            {
                string name="Figur"; foreach(var c in net.Lobby.characters) if(c.id==net.LobbyPendingDeletion) name=c.name;
                deleteDetails.text="„"+name+"“ löschen?\n\nInventar, Ausrüstung und persönlicher Fortschritt gehen verloren.\nDie Gründerfigur bleibt geschützt.";
            }
            var selected=EventSystem.current?.currentSelectedGameObject;
            bool invalid=selected==null || !selected.activeInHierarchy || selected.GetComponent<Selectable>()?.IsInteractable()==false;
            if(changed || (!input.PointerMode && !input.Busy && invalid)) Focus();
            if(!input.Busy && (Keyboard.current?.escapeKey.wasPressedThisFrame==true || Gamepad.current?.buttonEast.wasPressedThisFrame==true))
            {
                var field=selected!=null ? selected.GetComponent<InputField>() : null;
                if(field!=null && field.isFocused) field.DeactivateInputField();
                else if(page!=0) net.CancelLobbyDialog();
                else EventSystem.current?.SetSelectedGameObject(leave.gameObject);
            }
        }
        void Focus()
        {
            foreach(var selectable in CurrentPage.GetComponentsInChildren<Selectable>())
                if(selectable.IsInteractable() && selectable.navigation.mode!=Navigation.Mode.None)
                { EventSystem.current?.SetSelectedGameObject(selectable.gameObject); return; }
        }
    }
}
