using System;

namespace SecretsReborn
{
    [Serializable]
    public sealed class VitalsSaveData
    {
        public int health, maxHealth, mana, maxMana;
    }

    // Host-owned runtime state; presentation never writes these values directly.
    public sealed class CharacterVitalsState
    {
        public const int StartingHearts = 3;
        public const int MaximumHearts = 20;
        // One health unit is half a heart, including attack/healing amounts.
        public int Health { get; private set; } = StartingHearts * 2;
        public int MaxHealth { get; private set; } = StartingHearts * 2;
        public int HeartContainers => MaxHealth / 2;
        public int HeartFill(int index) => index < 0 || index >= HeartContainers ? 0 : Math.Max(0, Math.Min(2, Health - index * 2));
        public bool AddHeartContainer()
        {
            if (IsDown || MaxHealth >= MaximumHearts * 2) return false;
            MaxHealth += 2; Health += 2; return true;
        }
        public int Mana { get; private set; } = 50;
        public int MaxMana { get; private set; } = 50;
        public bool IsDown => Health == 0;
        public bool Damage(int amount)
        {
            if (amount <= 0 || IsDown) return false;
            Health -= Math.Min(amount, Health); return true;
        }
        public bool Heal(int amount)
        {
            if (amount <= 0 || IsDown || Health == MaxHealth) return false;
            Health += Math.Min(amount, MaxHealth - Health); return true;
        }
        // Revival is a distinct host operation; ordinary healing cannot revive.
        public bool Revive(int health)
        {
            if (!IsDown || health <= 0) return false;
            Health = Math.Min(health, MaxHealth); return true;
        }
        public bool SpendMana(int amount)
        {
            if (amount <= 0 || IsDown || amount > Mana) return false;
            Mana -= amount; return true;
        }
        public bool RestoreMana(int amount)
        {
            if (amount <= 0 || Mana == MaxMana) return false;
            Mana += Math.Min(amount, MaxMana - Mana); return true;
        }
        public VitalsSaveData Capture() => new VitalsSaveData { health = Health, maxHealth = MaxHealth, mana = Mana, maxMana = MaxMana };
        public static CharacterVitalsState Restore(VitalsSaveData data)
        {
            Validate(data);
            if (data.maxHealth < StartingHearts * 2 || data.maxHealth > MaximumHearts * 2 || data.maxHealth % 2 != 0)
                throw new ArgumentException("Invalid character vitals.");
            return new CharacterVitalsState { Health = data.health, MaxHealth = data.maxHealth, Mana = data.mana, MaxMana = data.maxMana };
        }
        public static CharacterVitalsState RestoreLegacy(VitalsSaveData data)
        {
            Validate(data);
            // Preserve the health ratio from the old 100-HP baseline; positive HP
            // rounds up to half a heart so migration cannot down a living player.
            int containers = (int)Math.Min(MaximumHearts, Math.Max(StartingHearts, Math.Ceiling(data.maxHealth * 3d / 100d)));
            int maximum = containers * 2;
            int health = (int)Math.Ceiling((double)data.health / data.maxHealth * maximum);
            return Restore(new VitalsSaveData { health = health, maxHealth = maximum, mana = data.mana, maxMana = data.maxMana });
        }
        private static void Validate(VitalsSaveData data)
        {
            if (data == null || data.maxHealth <= 0 || data.maxMana < 0 || data.health < 0 || data.health > data.maxHealth || data.mana < 0 || data.mana > data.maxMana)
                throw new ArgumentException("Invalid character vitals.");
        }
    }
}
