using System;
namespace SecretsReborn
{
    [Serializable] public sealed class WorldCharacterSlot
    {
        public string id, name;
        public int hairColor = -1, eyeColor = -1;
        public string bodyStyle, hairStyle, eyeStyle;
        public WorldCharacterSlot Copy() => new WorldCharacterSlot { id = id, name = name, hairColor = hairColor, eyeColor = eyeColor,
            bodyStyle = bodyStyle, hairStyle = hairStyle, eyeStyle = eyeStyle };
    }
}
