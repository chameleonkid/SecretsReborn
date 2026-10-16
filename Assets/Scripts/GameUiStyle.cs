using UnityEngine;
namespace SecretsReborn
{
    public static class GameUiStyle
    {
        public static readonly Color Surface=new Color(.018f,.038f,.03f,.84f);
        public static readonly Color Slot=new Color(.018f,.04f,.032f,.62f);
        public static readonly Color Gold=new Color(.78f,.65f,.34f,.9f);
        public static readonly Color Text=new Color(.96f,.92f,.78f);
        public static readonly Color Muted=new Color(.70f,.75f,.69f);
        public static bool LocalOverlayOpen
        {
            get
            {
                if(SaveBook.IsOpen || GameSession.Existing?.Busy==true) return true;
                var follow=Camera.main!=null ? Camera.main.GetComponent<CameraFollow>() : null;
                var actor=follow!=null && follow.Target!=null ? follow.Target.GetComponent<CharacterInventory>() : null;
                return actor!=null && (actor.GetComponent<InventoryInteraction>()?.IsOpen==true || actor.GetComponent<SpellRingMenu>()?.IsOpen==true);
            }
        }
        public static void Fill(Rect area,Color tint)
        { var previous=GUI.color; GUI.color=tint; GUI.DrawTexture(area,Texture2D.whiteTexture); GUI.color=previous; }
        public static void Border(Rect area,Color tint,float width=1)
        {
            Fill(new Rect(area.x,area.y,area.width,width),tint); Fill(new Rect(area.x,area.yMax-width,area.width,width),tint);
            Fill(new Rect(area.x,area.y,width,area.height),tint); Fill(new Rect(area.xMax-width,area.y,width,area.height),tint);
        }
        public static void Panel(Rect area)
        {
            Fill(area,Surface); Border(area,new Color(.34f,.27f,.12f,.9f));
            var inner=new Rect(area.x+5,area.y+5,area.width-10,area.height-10); Border(inner,Gold);
            foreach(float x in new[]{inner.x,inner.xMax}) foreach(float y in new[]{inner.y,inner.yMax})
            {
                Fill(new Rect(x==inner.x ? x : x-24,y-1,24,2),Gold);
                Fill(new Rect(x-1,y==inner.y ? y : y-24,2,24),Gold);
            }
        }
        public static void Hint(Rect area,string value)
        {
            Fill(area,Surface); Border(area,new Color(.78f,.65f,.34f,.45f));
            var style=new GUIStyle(GUI.skin.label) {alignment=TextAnchor.MiddleCenter,fontSize=14}; style.normal.textColor=Text; GUI.Label(area,value,style);
        }
    }
}
