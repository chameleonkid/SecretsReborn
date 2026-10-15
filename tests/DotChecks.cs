using System;
using SecretsReborn;

public static class DotChecks
{
    static void Check(bool value,string message) { if(!value) throw new Exception(message); }
    static DotApplication Effect(DotKind kind,int amount=1) => new DotApplication { kind=kind,damagePerTick=amount };
    public static void Main()
    {
        var state=new DotState(); int total=0,ticks=0;
        Action<DotKind,int> tick=(kind,amount)=>{ total+=amount; ticks++; };
        Check(state.Apply(Effect(DotKind.Burn),0),"apply burn");
        state.Advance(.9,tick); Check(total==0,"no immediate tick");
        Check(state.Apply(Effect(DotKind.Burn),0),"refresh burn");
        state.Advance(.1,tick); Check(total==1,"refresh preserves next tick");
        state.Advance(2.9,tick); Check(total==3 && state.ActiveMask==0,"three ticks, no extra refreshed stack");
        state.Clear(); total=0; ticks=0;
        Check(state.Apply(Effect(DotKind.Burn),0) && state.Apply(Effect(DotKind.Poison,2),0) && state.Apply(Effect(DotKind.Bleed,3),0),"three kinds");
        Check(state.ActiveMask==7,"all effects coexist");
        state.Advance(3,tick); Check(total==18 && ticks==9 && state.ActiveMask==0,"independent ticks and final expiry ticks");
        state.Apply(Effect(DotKind.Burn),0); state.Advance(.5,tick);
        state.Apply(Effect(DotKind.Poison),0); state.Advance(2.5,tick);
        Check(state.ActiveMask==2,"independent expiry"); state.Advance(.5,tick); Check(state.ActiveMask==0,"last effect expires");
        var chance=Effect(DotKind.Burn); chance.chancePercent=25;
        Check(state.Apply(chance,.249) && !state.Apply(chance,.25),"chance boundary");
        Check(!state.Apply(chance,double.NaN) && !state.Apply(chance,-.1) && !state.Apply(chance,1),"invalid rolls");
        var invalid=Effect(DotKind.Burn); invalid.kind=(DotKind)99; Check(!state.Apply(invalid,0),"invalid kind");
        invalid=Effect(DotKind.Burn); invalid.duration=float.PositiveInfinity; Check(!state.Apply(invalid,0),"infinite duration");
        invalid=Effect(DotKind.Burn); invalid.tickInterval=0; Check(!state.Apply(invalid,0),"zero interval");
        invalid=Effect(DotKind.Burn); invalid.damagePerTick=0; Check(!state.Apply(invalid,0),"zero damage");
        state.Clear(); total=0; ticks=0; var original=Effect(DotKind.Burn);
        state.Apply(original,0); original.damagePerTick=100; state.Advance(1,tick); Check(total==1,"application copies source values");
        state.Advance(double.PositiveInfinity,tick); state.Advance(double.NaN,tick); Check(total==1,"invalid elapsed ignored");
        state.Clear(); Check(state.ActiveMask==0,"clear removes all");
        ticks=0; state.Apply(new DotApplication {duration=120,tickInterval=.1f},0);
        state.Advance(120,(kind,amount)=>ticks++); Check(ticks==1200 && state.ActiveMask==0,"float interval does not lose final tick");
        state.Apply(Effect(DotKind.Burn),0); state.Apply(Effect(DotKind.Poison),0); ticks=0;
        state.Advance(30,(kind,amount)=>{ ticks++; state.Clear(); }); Check(ticks==1,"death/clear stops catchup ticks");
        Console.WriteLine("PASS: independent DoTs, refresh without timer reset/stacks, source copies, chance boundaries, final/catchup ticks, cleanup and invalid data.");
    }
}
