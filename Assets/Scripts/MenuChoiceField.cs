using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecretsReborn
{
    public sealed class MenuChoiceField : Selectable,ISubmitHandler
    {
        [SerializeField] private Text label;
        [SerializeField] private string[] options;
        [SerializeField] private int selected;
        public UnityEvent<int> onValueChanged=new UnityEvent<int>();
        public int Value => selected;
        public void Configure(Text text,string[] values) { label=text; options=values; Show(); }
        public void SetOption(int index,string text) { options[index]=text; Show(); }
        public void SetValueWithoutNotify(int value) { selected=Mathf.Clamp(value,0,options.Length-1); Show(); }
        public void Previous() => Step(-1);
        public void Next() => Step(1);
        void Step(int direction)
        {
            if(!IsInteractable() || options==null || options.Length==0) return;
            selected=(selected+direction+options.Length)%options.Length; Show();
            EventSystem.current?.SetSelectedGameObject(gameObject); onValueChanged.Invoke(selected);
        }
        void Show() { if(label!=null && options?.Length>0) label.text=options[Mathf.Clamp(selected,0,options.Length-1)]; }
        public override void OnMove(AxisEventData data)
        {
            if(data.moveDir==MoveDirection.Left || data.moveDir==MoveDirection.Right) { Step(data.moveDir==MoveDirection.Left ? -1 : 1); data.Use(); }
            else base.OnMove(data);
        }
        public void OnSubmit(BaseEventData data) => Next();
    }
}
