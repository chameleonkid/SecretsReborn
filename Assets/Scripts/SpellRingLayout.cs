using System;

namespace SecretsReborn
{
    public static class SpellRingLayout
    {
        public const int Slots=8;
        public static float Angle(int item,int count)
        {
            if(count<=0 || item<0 || item>=count) throw new ArgumentOutOfRangeException();
            return item*360f/count;
        }
        // Use the authored eight ring positions without rewriting their RectTransforms.
        public static int Position(int item,int visible)
        {
            if (visible<1 || visible>Slots || item<0 || item>=visible) throw new ArgumentOutOfRangeException();
            return (int)Math.Round(item*Slots/(double)visible,MidpointRounding.AwayFromZero)%Slots;
        }
        public static int Page(int selected,int count,int direction)
        {
            if (count<=0) return 0;
            int pages=(count+Slots-1)/Slots;
            int next=((selected/Slots+direction)%pages+pages)%pages;
            return next*Slots;
        }
    }
}
