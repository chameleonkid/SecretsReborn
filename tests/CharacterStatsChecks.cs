using System;
using SecretsReborn;
internal static class CharacterStatsChecks
{
    static void Check(bool ok,string why) { if (!ok) throw new Exception(why); }
    public static void Main()
    {
        var v=new CharacterVitalsState(); v.Damage(1); v.SpendMana(20); v.SetEquipmentBonuses(4,30);
        Check(v.Health==5 && v.Mana==30 && v.MaxHealth==10 && v.MaxMana==80,"equip must not heal");
        v.Heal(99); v.RestoreMana(99); v.SetEquipmentBonuses(0,0);
        Check(v.Health==6 && v.Mana==50 && v.BaseMaxHealth==6,"unequip clamps without changing base");
        v.SetEquipmentBonuses(4,30); var copy=CharacterVitalsState.Restore(v.Capture()); copy.SetEquipmentBonuses(0,0);
        Check(copy.MaxHealth==6 && copy.MaxMana==50,"save bonuses stay temporary");
        Check(v.AddHeartContainer() && v.BaseMaxHealth==8 && v.MaxHealth==12,"permanent hearts separated");
        Check(v.AddManaCrystal(20) && v.BaseMaxMana==70 && v.MaxMana==100,"permanent mana separated");
        v.SetEquipmentBonuses(int.MaxValue,int.MaxValue); Check(v.MaxHealth==40 && v.MaxMana==10000,"caps");
        v.Damage(999); v.SetEquipmentBonuses(0,0); Check(v.IsDown && !v.AddHeartContainer(),"bonuses never revive");
        var legacy=CharacterVitalsState.Restore(new VitalsSaveData { health=8,maxHealth=8,mana=60,maxMana=60 });
        Check(legacy.BaseMaxHealth==8 && legacy.BaseMaxMana==60,"legacy values become base");
        var stats=new CharacterStats(); stats.AddEquipment(int.MaxValue,int.MaxValue,int.MaxValue); stats.AddEquipment(int.MaxValue,4,30);
        Check(stats.Armor==1000000 && stats.BonusHalfHearts==40 && stats.BonusMana==10000,"safe aggregation");
        Check(CombatRules.MitigatedDamage(4,100)==2 && CombatRules.MitigatedDamage(1,999)==1,"damage floor");
        Console.WriteLine("PASS: equipment/base separation, no heal exploit, clamping, permanent upgrades, caps, migration and damage floor.");
    }
}
