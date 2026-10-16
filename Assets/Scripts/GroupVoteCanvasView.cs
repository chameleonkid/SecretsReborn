using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
namespace SecretsReborn
{
    public sealed class GroupVoteCanvasView : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private Text heading,destination,progress,hint;
        [SerializeField] private GameObject[] rows;
        [SerializeField] private Text[] names,states;
        [SerializeField] private ConsentMarkGraphic[] marks;
        [SerializeField] private Button accept,cancel;
        private NetworkCoop owner;
        public bool Visible => gameObject.activeInHierarchy;
        public int AcceptedCount => marks.Count(x=>x.gameObject.activeInHierarchy && x.Accepted);
        public int PlayerCount => rows.Count(x=>x.activeInHierarchy);
        public void Configure(RectTransform box,Text title,Text place,Text status,Text controls,GameObject[] entries,Text[] players,Text[] answers,ConsentMarkGraphic[] checks,Button yes,Button no)
        { panel=box; heading=title; destination=place; progress=status; hint=controls; rows=entries; names=players; states=answers; marks=checks; accept=yes; cancel=no; }
        public void Bind(NetworkCoop net)
        {
            owner=net;
            if(EventSystem.current==null)
            {
                var system=new GameObject("Game UI EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule)); system.transform.SetParent(transform.parent,false);
                var module=system.GetComponent<InputSystemUIInputModule>(); module.AssignDefaultActions(); module.move=module.submit=module.cancel=null;
            }
        }
        public void Show() { gameObject.SetActive(true); Render(); }
        public void Hide() => gameObject.SetActive(false);
        public void Accept() => owner?.AnswerVote(true);
        public void Cancel() => owner?.AnswerVote(false);
        void Update() { if(owner==null || !owner.VotePending) { Hide(); return; } Render(); }
        void Render()
        {
            if(owner==null) return;
            heading.text=owner.VoteHeading; destination.text=owner.VoteDestination+"\nAlle verbundenen Spieler müssen zustimmen.";
            var roster=owner.VotePlayers; panel.sizeDelta=new Vector2(panel.sizeDelta.x,350+58*roster.Count);
            int agreed=0;
            for(int i=0;i<rows.Length;i++)
            {
                rows[i].SetActive(i<roster.Count); if(i>=roster.Count) continue;
                var player=roster[i]; names[i].text=player.name+(player.client==owner.LobbyClientId ? "  (Du)" : ""); marks[i].Accepted=player.accepted;
                states[i].text=player.accepted ? "Zugestimmt" : "Wartet auf Antwort";
                states[i].color=player.accepted ? new Color(.45f,.94f,.57f) : new Color(.72f,.74f,.67f);
                if(player.accepted) agreed++;
            }
            progress.text=agreed+" / "+roster.Count+" Zustimmungen  ·  "+owner.VoteSecondsRemaining+" s";
            accept.interactable=cancel.interactable=!owner.VoteAnswered;
            hint.text=owner.VoteAnswered ? "Deine Antwort wurde gesendet. Warte auf die Gruppe …" : "Enter / A: zustimmen   ·   Esc / B: abbrechen";
        }
    }
}
