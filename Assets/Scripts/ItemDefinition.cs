using UnityEngine;

namespace SecretsReborn
{
    public enum ItemQuality { Normal, Uncommon, Rare, Epic, Legendary }
    [CreateAssetMenu(menuName = "SecretsReborn/Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField, Min(1)] private int maxStack = 1;
        [SerializeField] private ClothingAppearance armorAppearance;
        [SerializeField] private ItemKind kind;
        [SerializeField] private bool twoHanded;
        [SerializeField] private Sprite icon;
        [SerializeField] private ItemQuality quality = ItemQuality.Normal;
        [SerializeField] private Rect iconContent = new Rect(0, 0, 1, 1);
        public ItemQuality Quality => quality;
        public Rect IconContent => iconContent;
        public void SetIconContent(Rect area) => iconContent = area;
        public Color QualityColor => quality == ItemQuality.Uncommon ? new Color(.18f, .83f, .25f)
            : quality == ItemQuality.Rare ? new Color(.2f, .5f, 1)
            : quality == ItemQuality.Epic ? new Color(.7f, .32f, .95f)
            : quality == ItemQuality.Legendary ? new Color(1, .5f, .12f) : new Color(.8f, .82f, .8f);
        public string QualityLabel => quality == ItemQuality.Uncommon ? "Ungewöhnlich" : quality == ItemQuality.Rare ? "Selten"
            : quality == ItemQuality.Epic ? "Episch" : quality == ItemQuality.Legendary ? "Legendär" : "Normal";
        public Sprite Icon => icon != null ? icon : armorAppearance != null ? armorAppearance.Frame(0) : null;
        public ItemRules Rules => new ItemRules { kind = kind == ItemKind.None && armorAppearance != null ? ItemKind.Armor : kind,
            twoHanded = twoHanded, maxStack = MaxStack };
        public void SetEquipment(ItemKind category, bool usesBothHands, Sprite image)
        { kind = category; twoHanded = usesBothHands; icon = image; }
        public string ItemId => itemId;
        public string DisplayName => displayName;
        public int MaxStack => Mathf.Max(1, maxStack);
        public ClothingAppearance ArmorAppearance => armorAppearance;
        public void Configure(string id, string label, int stack, ClothingAppearance armor)
        { itemId = id; displayName = label; maxStack = Mathf.Max(1, stack); armorAppearance = armor; }
    }
}
