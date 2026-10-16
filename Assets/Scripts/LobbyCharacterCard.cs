using System;
using UnityEngine;
using UnityEngine.UI;
namespace SecretsReborn
{
    public sealed class LobbyCharacterCard : MonoBehaviour
    {
        [SerializeField] private int index;
        [SerializeField] private Text title,status,note;
        [SerializeField] private MenuPortraitView portrait;
        [SerializeField] private Button choose,create,remove;
        [SerializeField] private Sprite selectArt,selectedArt;
        [SerializeField] private Image backdrop;
        public void Configure(int row,Text name,Text state,Text hint,MenuPortraitView preview,Button pick,Button add,Button delete,Sprite normal,Sprite selected)
        { index=row; title=name; status=state; note=hint; portrait=preview; choose=pick; create=add; remove=delete; selectArt=normal; selectedArt=selected; backdrop=GetComponent<Image>(); }
        public void Render(NetworkCoop net)
        {
            bool occupied=index<net.Lobby.characters.Length;
            choose.gameObject.SetActive(occupied); create.gameObject.SetActive(!occupied); portrait.gameObject.SetActive(occupied);
            if(!occupied)
            {
                title.text="Freier Charakterplatz"; status.text="Eine neue Figur für diese Welt"; note.text=""; remove.gameObject.SetActive(false);
                create.interactable=net.LobbyConnectedLocally && string.IsNullOrEmpty(net.LobbySelection) && net.Lobby.characters.Length<4;
                backdrop.color=new Color(.025f,.06f,.045f,.72f); return;
            }
            var c=net.Lobby.characters[index]; LobbyPlayer owner=null;
            foreach(var p in net.Lobby.players) if(p.character==c.id) owner=p;
            bool selected=c.id==net.LobbySelection,founder=c.id==net.Lobby.founder;
            title.text=c.name; status.text=owner==null ? "Verfügbar" : owner.name+(owner.ready ? " · Bereit" : " · Wartet");
            note.text=founder ? "Gründerfigur · geschützt" : "";
            choose.interactable=net.LobbyConnectedLocally && (owner==null || owner.client==net.LobbyClientId);
            choose.image.sprite=selected ? selectedArt : selectArt;
            remove.gameObject.SetActive(net.LobbyIsHost && !founder); remove.interactable=owner==null;
            LobbyPortrait equipment=null; foreach(var p in net.Lobby.portraits ?? Array.Empty<LobbyPortrait>()) if(p.id==c.id) equipment=p;
            portrait.Present(c,equipment); backdrop.color=selected ? new Color(.13f,.19f,.09f,.9f) : new Color(.025f,.06f,.045f,.72f);
        }
    }
}
