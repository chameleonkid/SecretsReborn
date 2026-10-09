using UnityEngine;
namespace SecretsReborn
{
    public sealed class CharacterCreatorSelection
    {
        private bool male;
        private int skin, style, color, eyes;
        public WorldCharacterSlot Profile(string name) => new WorldCharacterSlot { name = name, hairColor = color, eyeColor = -1,
            bodyStyle = CharacterCustomization.Body(male,skin), hairStyle = CharacterCustomization.Hair(male,style), eyeStyle = CharacterCustomization.Eyes(male,eyes) };
        private string Text(int row) => row == 0 ? "Körper · " + (male ? "B" : "A") : row == 1 ? "Hautton · " + (male && skin == 7 ? "Gebräunt" : CharacterCustomization.SkinNames[skin])
            : row == 2 ? "Frisur · " + CharacterCustomization.HairStyles[style] : row == 3 ? "Haarfarbe · " + CharacterPalette.HairNames[color]
            : "Augen · " + CharacterCustomization.EyeNames[eyes];
        private void Next(int row)
        { if (row == 0) male = !male; else if (row == 1) skin = (skin+1)%8; else if (row == 2) style = (style+1)%13; else if (row == 3) color = (color+1)%6; else eyes = (eyes+1)%7; }
        public void Draw()
        { for (int i = 0; i < 5; i++) if (MenuArt.Button(Text(i), GUILayout.Height(35))) Next(i); }
        public void Draw(Rect area)
        { for (int i = 0; i < 5; i++) if (MenuArt.Button(new Rect(area.x,area.y+i*47,area.width,40),Text(i) + "  ›")) Next(i); }
    }
}
