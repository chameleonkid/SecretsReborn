using System;

namespace SecretsReborn
{
    public struct CharacterStats
    {
        public int Armor, BonusHalfHearts, BonusMana, WeaponDamage;
        public float AttackCooldown;
        public float CritChancePercent;
        public void AddEquipment(int armor,int health,int mana,float critChancePercent=0)
        {
            Armor=(int)Math.Min(1000000,(long)Armor+Math.Max(0,armor));
            BonusHalfHearts=(int)Math.Min(40,(long)BonusHalfHearts+Math.Max(0,health));
            BonusMana=(int)Math.Min(10000,(long)BonusMana+Math.Max(0,mana));
            CritChancePercent=CombatRules.CritChance(CritChancePercent+CombatRules.CritChance(critChancePercent));
        }
    }
}
