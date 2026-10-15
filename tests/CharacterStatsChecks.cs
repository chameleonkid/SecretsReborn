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
        var criticalStats=new CharacterStats(); criticalStats.AddEquipment(0,0,0,12.5f); criticalStats.AddEquipment(0,0,0,7.5f);
        Check(criticalStats.CritChancePercent==20,"critical bonuses add as percentage points");
        criticalStats.AddEquipment(0,0,0,100); Check(criticalStats.CritChancePercent==100,"critical chance capped");
        Check(!CombatRules.IsCritical(0,0) && CombatRules.IsCritical(100,1),"zero and guaranteed crit");
        Check(CombatRules.IsCritical(20,.199f) && !CombatRules.IsCritical(20,.2f),"crit roll boundary");
        Check(!CombatRules.IsCritical(float.NaN,0) && !CombatRules.IsCritical(50,float.NaN) && !CombatRules.IsCritical(50,-1),"invalid crit input");
        Check(CombatRules.HitDamage(3,true)==6 && CombatRules.HitDamage(3,false)==3 && CombatRules.HitDamage(int.MaxValue,true)==int.MaxValue,"crit damage and overflow");
        Check(CombatRules.MitigatedDamage(CombatRules.HitDamage(3,true),100)==3,"critical damage before armor");
        Console.WriteLine("PASS: equipment/base separation, no heal exploit, clamping, permanent upgrades, caps, migration and damage floor.");
    }
}
