using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace SecretsReborn
{
    public sealed class SpellRingView : MonoBehaviour
    {
        [SerializeField] private Text title,details,page;
        [SerializeField] private Button[] choices;
        [SerializeField] private Image[] highlights;
        [SerializeField] private Text[] labels;
        [SerializeField] private Image spellIcon;
        private SpellRingMenu owner;
        private int offset;
        private readonly int[] entries=new int[SpellRingLayout.Slots];
        [SerializeField] private RectTransform ringRoot;
        [SerializeField] private Image iconTemplate;
        [SerializeField] private RectTransform marker;
        [SerializeField] private GameObject bookPanel;
        [SerializeField] private Text bookText;
        [SerializeField] private Vector2 radius=new Vector2(94,74);
        [SerializeField] private float rotationSpeed=540;
        private readonly List<Image> ringIcons=new List<Image>();
        private float rotation;
        private int ringCount;
        private readonly List<Button> bookButtons=new List<Button>();
        public int IconCount => ringCount;
        public bool RingVisible => ringRoot!=null && ringRoot.gameObject.activeSelf;
        public void Bind(SpellRingMenu menu)
        {
            owner=menu;
            if (EventSystem.current==null)
            { var obj=new GameObject("Magic EventSystem",typeof(EventSystem)); obj.transform.SetParent(transform,false); var module=obj.AddComponent<InputSystemUIInputModule>(); module.AssignDefaultActions(); module.move=module.submit=module.cancel=null; }
            if (choices!=null) for (int i=0;i<choices.Length;i++) { int index=i; choices[i].onClick.AddListener(()=>owner.Choose(entries[index])); }
            if(bookPanel!=null) for(int i=0;i<8;i++)
            {
                int index=i; var row=new GameObject("BookChoice-"+i,typeof(RectTransform),typeof(Image),typeof(Button)); row.transform.SetParent(bookPanel.transform,false);
                var rect=row.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1); rect.anchoredPosition=new Vector2(25,-20-i*42); rect.sizeDelta=new Vector2(540,42);
                var image=row.GetComponent<Image>(); image.color=Color.clear;
                var button=row.GetComponent<Button>(); button.targetGraphic=image; button.transition=Selectable.Transition.None; button.navigation=new Navigation {mode=Navigation.Mode.None};
                button.onClick.AddListener(()=>owner.Choose(offset+index)); bookButtons.Add(button);
            }
        }
        public void RenderIcons(string heading,IReadOnlyList<string> items,IReadOnlyList<Sprite> icons,int selected,string description,Transform actor,bool book,bool targeting)
        {
            title.text=heading; details.text=description;
            bookPanel.SetActive(book); ringRoot.gameObject.SetActive(!book && !targeting && items.Count>0);
            if (book)
            {
                int start=selected/8*8;
                offset=start;
                bookText.text="";
                for (int i=start;i<Mathf.Min(start+8,items.Count);i++) bookText.text+=(i==selected ? "▶ " : "    ")+items[i].Replace("\n"," · ")+"\n\n";
                for(int i=0;i<bookButtons.Count;i++) bookButtons[i].gameObject.SetActive(start+i<items.Count);
                page.text="Zauberbuch · "+(items.Count==0 ? "Leer" : "Seite "+(start/8+1)+" / "+Mathf.CeilToInt(items.Count/8f));
            }
            else page.text=targeting ? "Pfeile / Stick: Ziel · X / T: Einzel / Alle · A / Enter: bestätigen · B / Esc: zurück" : "Pfeile / Stick: drehen · A / Enter: wählen · Y / Tab: Zauberbuch · B / Esc: zurück";
            if (ringCount!=items.Count)
            { ringCount=items.Count; rotation=ringCount>0 ? SpellRingLayout.Angle(selected,ringCount) : 0; }
            while (ringIcons.Count<items.Count)
            {
                int index=ringIcons.Count; var image=Instantiate(iconTemplate,ringRoot); image.name="Icon-"+index; image.gameObject.SetActive(true);
                image.GetComponent<Button>().onClick.AddListener(()=>owner.Choose(index)); ringIcons.Add(image);
            }
            if (Camera.main!=null && actor!=null)
            {
                var screen=Camera.main.WorldToScreenPoint(actor.position+Vector3.up*.6f);
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,screen,null,out var local);
                // Keep the ring clear of the header and screen edges.
                var bounds=((RectTransform)transform).rect;
                local.x=Mathf.Clamp(local.x,bounds.xMin+radius.x+30,bounds.xMax-radius.x-30);
                local.y=Mathf.Clamp(local.y,bounds.yMin+radius.y+35,bounds.yMax-radius.y-130);
                ringRoot.anchoredPosition=new Vector2(Mathf.Round(local.x),Mathf.Round(local.y));
            }
            if(items.Count>0) rotation=Mathf.MoveTowardsAngle(rotation,SpellRingLayout.Angle(selected,items.Count),rotationSpeed*Time.unscaledDeltaTime);
            marker.anchoredPosition=new Vector2(0,radius.y);
            for(int i=0;i<ringIcons.Count;i++)
            {
                var image=ringIcons[i]; image.gameObject.SetActive(i<items.Count && !book && !targeting);
                if (i>=items.Count) continue;
                image.sprite=i<icons.Count ? icons[i] : null;
                image.color=i==selected ? Color.white : new Color(.8f,.8f,.8f,1);
                float angle=(SpellRingLayout.Angle(i,items.Count)-rotation)*Mathf.Deg2Rad;
                image.rectTransform.anchoredPosition=new Vector2(Mathf.Round(Mathf.Sin(angle)*radius.x),Mathf.Round(Mathf.Cos(angle)*radius.y));
                image.rectTransform.sizeDelta=Vector2.one*(i==selected ? 42 : 34);
            }
        }
        public void Back() => owner.Back();
        public void Book() => owner.ToggleBook();
        public void Previous() => owner.Page(-1);
        public void Next() => owner.Page(1);
        public void Render(string heading,IReadOnlyList<string> items,int selected,string description,Sprite icon)
        {
            title.text=heading; details.text=description; offset=selected/choices.Length*choices.Length;
            page.text=items.Count==0 ? "" : "Seite "+(offset/choices.Length+1)+" / "+Mathf.CeilToInt(items.Count/(float)choices.Length);
            spellIcon.sprite=icon; spellIcon.enabled=icon!=null;
            for (int i=0;i<choices.Length;i++) { choices[i].gameObject.SetActive(false); highlights[i].enabled=false; }
            int visible=Mathf.Min(choices.Length,items.Count-offset);
            for (int i=0;i<visible;i++)
            {
                int position=SpellRingLayout.Position(i,visible); entries[position]=offset+i;
                choices[position].gameObject.SetActive(true); labels[position].text=items[offset+i]; highlights[position].enabled=selected==offset+i;
            }
        }
    }
}
