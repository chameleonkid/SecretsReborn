using System;

namespace SecretsReborn
{
    public enum DotKind { Burn, Poison, Bleed }

    [Serializable] public sealed class DotApplication
    {
        public DotKind kind;
        public float chancePercent=100;
        public float duration=3;
        public float tickInterval=1;
        public int damagePerTick=1;
        public bool Valid => Enum.IsDefined(typeof(DotKind),kind) && Finite(chancePercent) && chancePercent>0 && chancePercent<=100
            && Finite(duration) && duration>0 && duration<=120 && Finite(tickInterval) && tickInterval>=.1f
            && tickInterval<=duration && damagePerTick>0;
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public DotApplication Copy() => (DotApplication)MemberwiseClone();
    }

    // Host clock only. One slot per effect type, independent of source and visual state.
    public sealed class DotState
    {
        private sealed class Effect
        {
            public DotApplication definition;
            public double ends, next;
        }
        private readonly Effect[] effects=new Effect[3];
        private double clock;
        public int ActiveMask
        {
            get { int mask=0; for(int i=0;i<effects.Length;i++) if(effects[i]!=null) mask|=1<<i; return mask; }
        }
        public bool Apply(DotApplication definition,double roll)
        {
            if(definition==null || !definition.Valid || double.IsNaN(roll) || roll<0 || roll>=1 || roll>=definition.chancePercent/100d) return false;
            int index=(int)definition.kind;
            var current=effects[index];
            if(current==null) effects[index]=new Effect { definition=definition.Copy(),ends=clock+definition.duration,next=clock+definition.tickInterval };
            else
            {
                // The latest source supplies tick strength/interval, but cannot delay an imminent tick.
                current.definition=definition.Copy();
                current.ends=Math.Max(current.ends,clock+definition.duration);
            }
            return true;
        }
        public void Advance(double elapsed,Action<DotKind,int> tick)
        {
            if(double.IsNaN(elapsed) || double.IsInfinity(elapsed) || elapsed<=0 || tick==null) return;
            clock+=elapsed;
            for(int i=0;i<effects.Length;i++)
            {
                var effect=effects[i];
                if(effect==null) continue;
                // Include the final tick exactly at expiry, and catch up after a slow frame.
                while(effects[i]==effect && effect.next<=clock+1e-5 && effect.next<=effect.ends+1e-5)
                {
                    effect.next+=effect.definition.tickInterval;
                    tick((DotKind)i,effect.definition.damagePerTick);
                }
                if(effects[i]==effect && clock>=effect.ends) effects[i]=null;
            }
        }
        public void Clear() { Array.Clear(effects,0,effects.Length); clock=0; }
    }
}
