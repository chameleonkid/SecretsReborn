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
        public int Health { get; private set; } = 100;
        public int MaxHealth { get; private set; } = 100;
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
            if (amount <= 0 || Health == MaxHealth) return false;
            Health += Math.Min(amount, MaxHealth - Health); return true;
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
            if (data == null || data.maxHealth <= 0 || data.maxMana < 0 || data.health < 0 || data.health > data.maxHealth || data.mana < 0 || data.mana > data.maxMana)
                throw new ArgumentException("Invalid character vitals.");
            return new CharacterVitalsState { Health = data.health, MaxHealth = data.maxHealth, Mana = data.mana, MaxMana = data.maxMana };
        }
    }
}
