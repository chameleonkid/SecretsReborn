using UnityEngine;

namespace SecretsReborn
{
    public enum ItemQuality { Normal, Uncommon, Rare, Epic, Legendary }
    public enum ItemPurpose { Equipment, Gold, HealthPotion, ManaPotion, Arrows }
    [CreateAssetMenu(menuName = "SecretsReborn/Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField, Min(1)] private int maxStack = 1;
        [SerializeField] private ClothingAppearance armorAppearance;
        [SerializeField] private ItemKind kind;
        [SerializeField] private bool twoHanded;
        [SerializeField, TextArea] private string description = "";
        [SerializeField, Min(0)] private int armorValue = 0;
        public int ArmorValue => Mathf.Max(0, armorValue);
        [SerializeField] private ItemPurpose purpose;
        [SerializeField, Min(1)] private int useAmount = 2;
        public ItemPurpose Purpose => purpose;
        public int UseAmount => Mathf.Max(1, useAmount);
        public void SetPurpose(ItemPurpose value, int amount) { purpose = value; useAmount = Mathf.Max(1, amount); }
        public string Description
        {
            get
            {
                string stats = weapon != null ? "Schaden: " + weapon.Damage + " · Cooldown: " + weapon.Cooldown.ToString("0.##") + " s"
                    : lamp != null ? "Lichtradius: " + lamp.LightRadius.ToString("0.#") + " · Helligkeit: " + lamp.Brightness + "/10\n"
                        + (lamp.RevealsRunes ? "Enthüllt nahe Runen · L / Y: Licht" : "Warmes Licht · L / Y: Licht")
                    : Rules.kind == ItemKind.Armor || Rules.kind == ItemKind.Head || Rules.kind == ItemKind.Shoulders
                        || Rules.kind == ItemKind.Waist || Rules.kind == ItemKind.Hands || Rules.kind == ItemKind.Legs
                        || Rules.kind == ItemKind.Feet || Rules.kind == ItemKind.Shield ? "Rüstungswert: " + ArmorValue
                    : purpose == ItemPurpose.HealthPotion ? "Heilt " + (UseAmount / 2f).ToString("0.#") + " Herzen · A / Enter: benutzen"
                    : purpose == ItemPurpose.ManaPotion ? "Stellt " + UseAmount + " Mana wieder her · A / Enter: benutzen"
                    : purpose == ItemPurpose.Gold ? "Gold · für spätere Händler aufbewahren"
                    : purpose == ItemPurpose.Arrows ? "Pfeile · Munition für spätere Fernkampfwaffen" : "Ausrüstungsgegenstand";
                return string.IsNullOrWhiteSpace(description) ? stats : description + "\n" + stats;
            }
        }
        [SerializeField] private WeaponDefinition weapon;
        public WeaponDefinition Weapon => weapon;
        [SerializeField] private LampDefinition lamp;
        public LampDefinition Lamp => lamp;
        public void SetLamp(LampDefinition profile) => lamp = profile;
        public Color IconTint => weapon != null ? weapon.Tint : lamp != null && lamp.RevealsRunes ? lamp.LightColor : armorAppearance != null ? armorAppearance.Tint : Color.white;
        public void SetWeapon(WeaponDefinition profile) => weapon = profile;
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
