using UnityEngine;
using UnityEngine.UI;

namespace SecretsReborn
{
    public sealed class MenuPortraitView : MonoBehaviour
    {
        [SerializeField] private CharacterAppearance source;
        [SerializeField] private RawImage[] layers;
        private string last;
        public void Configure(CharacterAppearance appearance,RawImage[] images) { source=appearance; layers=images; }
        public void Present(WorldCharacterSlot profile,LobbyPortrait equipment=null)
        {
            string key=profile.bodyStyle+"|"+profile.hairStyle+"|"+profile.eyeStyle+"|"+profile.hairColor+"|"+profile.eyeColor+"|"+equipment?.armor+"|"+equipment?.head+"|"+equipment?.feet;
            if(key==last || source==null) return; last=key;
            bool male=CharacterCustomization.IsMale(profile.bodyStyle);
            for(int layer=0;layer<layers.Length;layer++)
            {
                Sprite sprite=source.FrontPreviewLayer(layer,out var tint);
                var selected=CharacterLookLibrary.Find(layer==0 ? profile.bodyStyle : layer==2 ? profile.eyeStyle : layer==3 ? profile.hairStyle : null);
                if(selected!=null) { sprite=selected.Frame(0); if(layer==0 || layer==2) tint=Color.white; }
                if(layer==1 && male) { var clothing=CharacterLookLibrary.Find("male-3-outfits-rpc-male-ranger-clothes"); if(clothing!=null) sprite=clothing.Frame(0); }
                if(layer==3 && profile.hairColor>=0) tint=CharacterPalette.Hair[profile.hairColor];
                string gear=layer==1 ? equipment?.armor : layer==4 ? equipment?.feet : layer==5 ? equipment?.head : null;
                if(!string.IsNullOrEmpty(gear))
                {
                    var clothing=source.GetComponent<CharacterInventory>().Find(gear)?.ArmorAppearance;
                    if(clothing!=null) { sprite=clothing.Frame(0,male); tint=clothing.Tint; }
                }
                var image=layers[layer]; image.enabled=sprite!=null; if(sprite==null) continue;
                Rect uv=sprite.textureRect; image.texture=sprite.texture; image.color=tint;
                image.uvRect=new Rect(uv.x/sprite.texture.width,uv.y/sprite.texture.height,uv.width/sprite.texture.width,uv.height/sprite.texture.height);
            }
        }
    }
}
