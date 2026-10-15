using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SecretsReborn
{
    // Presentation only: a number survives the target's death, but never a scene change.
    public sealed class DamageNumbersView : MonoBehaviour
    {
        private sealed class Number { public Text text; public Vector3 position; public float started, offset; public Color color; }
        private static DamageNumbersView instance;
        private readonly List<Number> numbers=new List<Number>();
        private Canvas canvas;
        private int sequence;
        private void Awake()
        {
            instance=this; canvas=GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=4500;
            var scaler=GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1280,720); scaler.matchWidthOrHeight=.5f;
        }
        public static void Show(Vector3 position,int amount,bool critical)
        {
            if (amount<=0) return;
            if (instance==null) new GameObject("Damage numbers",typeof(Canvas),typeof(CanvasScaler),typeof(DamageNumbersView));
            instance.Add(position,amount,critical);
        }
        private void Add(Vector3 position,int amount,bool critical)
        {
            if (numbers.Count>=32) { Destroy(numbers[0].text.gameObject); numbers.RemoveAt(0); }
            var obj=new GameObject(critical ? "Critical damage number" : "Damage number",typeof(RectTransform),typeof(Text),typeof(Outline));
            obj.transform.SetParent(transform,false);
            var text=obj.GetComponent<Text>(); text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text=amount.ToString(); text.fontSize=critical ? 40 : 26; text.fontStyle=FontStyle.Bold;
            text.alignment=TextAnchor.MiddleCenter; text.raycastTarget=false;
            text.color=critical ? new Color(1,.65f,.12f) : new Color(1,.95f,.15f);
            text.rectTransform.sizeDelta=new Vector2(260,70);
            var outline=obj.GetComponent<Outline>(); outline.effectColor=new Color(0,0,0,.9f); outline.effectDistance=new Vector2(1.5f,-1.5f);
            numbers.Add(new Number { text=text, position=position+Vector3.up*.8f, started=Time.unscaledTime,
                offset=((sequence++%3)-1)*14, color=text.color });
        }
        private void LateUpdate()
        {
            var camera=Camera.main;
            for (int i=numbers.Count-1;i>=0;i--)
            {
                var number=numbers[i]; float age=Time.unscaledTime-number.started;
                if (age>=1) { Destroy(number.text.gameObject); numbers.RemoveAt(i); continue; }
                if (camera==null) { number.text.enabled=false; continue; }
                var point=camera.WorldToScreenPoint(number.position);
                number.text.enabled=point.z>0;
                number.text.rectTransform.anchoredPosition=((Vector2)point-new Vector2(Screen.width,Screen.height)*.5f)/canvas.scaleFactor
                    +new Vector2(number.offset,age*38);
                var color=number.color; color.a=Mathf.Clamp01((1-age)/.35f); number.text.color=color;
            }
        }
        private void OnDestroy() { if (instance==this) instance=null; }
    }
}
