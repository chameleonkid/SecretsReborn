using UnityEngine;
namespace SecretsReborn
{
    public static class MenuArt
    {
        private static Texture2D background;
        private static GUIStyle button;
        public static bool BlockInput;
        private static readonly Material[] eyeMaterials = new Material[4];
        public static void Backdrop()
        {
            if (background == null) background = Resources.Load<Texture2D>("MenuUI/ForestMenuBackdrop");
            if (background != null) GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height), background, ScaleMode.ScaleAndCrop);
        }
        public static GUIStyle Label(int size = 16, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = alignment, wordWrap = true };
            style.normal.textColor = new Color(.96f,.9f,.73f); return style;
        }
        public static bool Button(string text, params GUILayoutOption[] options)
        {
            if (button == null)
            {
                button = Label(16, TextAnchor.MiddleCenter); button.fontStyle = FontStyle.Bold;
                button.padding = new RectOffset(16,16,10,10);
                button.hover.textColor = new Color(1,.97f,.8f);
            }
            var rect = GUILayoutUtility.GetRect(new GUIContent(text), button, options);
            var old = GUI.color; GUI.color = GUI.enabled ? Color.white : new Color(.55f,.55f,.55f);
            ForestInventorySkin.Button(rect); GUI.color = old;
            if (BlockInput) { GUI.Label(rect,text,button); return false; }
            return GUI.Button(rect, text, button);
        }
        public static bool Button(Rect rect, string text)
        {
            ForestInventorySkin.Button(rect);
            if (BlockInput) { GUI.Label(rect,text,Label(15,TextAnchor.MiddleCenter)); return false; }
            return GUI.Button(rect, text, Label(15, TextAnchor.MiddleCenter));
        }
        public static void Portrait(Rect area, WorldCharacterSlot profile, LobbyPortrait equipment = null)
        {
            var prefab = Resources.Load<GameObject>("Coop/Player"); if (prefab == null) return;
            var appearance = prefab.GetComponent<CharacterAppearance>(); var inventory = prefab.GetComponent<CharacterInventory>();
            var target = PixelPortraitRect(area);
            for (int layer = 0; layer < 6; layer++)
            {
                var sprite = appearance.FrontPreviewLayer(layer, out var tint);
                var selected = CharacterLookLibrary.Find(layer == 0 ? profile.bodyStyle : layer == 2 ? profile.eyeStyle : layer == 3 ? profile.hairStyle : null);
                bool male = CharacterCustomization.IsMale(profile.bodyStyle);
                if (selected != null) { sprite = selected.Frame(0); if (layer == 0 || layer == 2) tint = Color.white; }
                if (layer == 1 && male) { var baseClothes = CharacterLookLibrary.Find("male-3-outfits-rpc-male-ranger-clothes"); if (baseClothes != null) sprite = baseClothes.Frame(0); }
                if (layer == 3 && profile.hairColor >= 0) tint = CharacterPalette.Hair[profile.hairColor];
                Material eyeMaterial = null;
                if (layer == 2 && selected == null && profile.eyeColor >= 0)
                {
                    int index = profile.eyeColor;
                    if (eyeMaterials[index] == null)
                    { var shader = Resources.Load<Shader>("MenuUI/EyeTint"); if (shader != null) { eyeMaterials[index] = new Material(shader) { hideFlags = HideFlags.HideAndDontSave }; eyeMaterials[index].SetColor("_Tint",CharacterPalette.Eyes[index]); } }
                    eyeMaterial = eyeMaterials[index]; tint = Color.white;
                }
                string gear = layer == 1 ? equipment?.armor : layer == 4 ? equipment?.feet : layer == 5 ? equipment?.head : null;
                if (!string.IsNullOrEmpty(gear))
                { var clothing = inventory.Find(gear)?.ArmorAppearance; if (clothing != null) { sprite = clothing.Frame(0,male); tint = clothing.Tint; } }
                if (sprite == null) continue;
                var uv = sprite.textureRect;
                var old = GUI.color; GUI.color = tint;
                var source = new Rect(uv.x/sprite.texture.width,uv.y/sprite.texture.height,uv.width/sprite.texture.width,uv.height/sprite.texture.height);
                if (eyeMaterial == null) GUI.DrawTextureWithTexCoords(target, sprite.texture, source);
                else if (Event.current.type == EventType.Repaint) Graphics.DrawTexture(target,sprite.texture,source,0,0,0,0,Color.white,eyeMaterial);
                GUI.color = old;
            }
        }
        // Every layer uses the same 32-pixel cell at an integer physical screen scale.
        internal static Rect PixelPortraitRect(Rect area)
        {
            float scaleX = Mathf.Max(.001f,GUI.matrix.MultiplyVector(Vector3.right).magnitude);
            float scaleY = Mathf.Max(.001f,GUI.matrix.MultiplyVector(Vector3.up).magnitude);
            float pixels = Mathf.Max(1,Mathf.Floor(Mathf.Min(area.width*scaleX,area.height*scaleY)/32));
            Vector3 center = GUI.matrix.MultiplyPoint3x4(new Vector3(area.center.x,area.center.y,0));
            center.x = Mathf.Round(center.x-pixels*16); center.y = Mathf.Round(center.y-pixels*16);
            Vector3 origin = GUI.matrix.inverse.MultiplyPoint3x4(center);
            return new Rect(origin.x,origin.y,pixels*32/scaleX,pixels*32/scaleY);
        }
    }
    [System.Serializable] public sealed class LobbyPortrait { public string id, armor, head, feet; }
}
