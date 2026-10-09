using System;
using System.Collections.Generic;
using SecretsReborn;
internal static class SpellRingLayoutChecks
{
    public static void Main()
    {
        for(int count=1;count<=8;count++)
        {
            var positions=new HashSet<int>();
            for(int i=0;i<count;i++)
            { int position=SpellRingLayout.Position(i,count); if(position<0 || position>=8 || !positions.Add(position)) throw new Exception("Overlapping ring entries"); }
        }
        if (SpellRingLayout.Position(1,2)!=4) throw new Exception("Two choices should be opposite");
        if (SpellRingLayout.Page(0,10,1)!=8 || SpellRingLayout.Page(8,10,1)!=0 || SpellRingLayout.Page(0,10,-1)!=8
            || SpellRingLayout.Page(3,4,1)!=0 || SpellRingLayout.Page(0,0,1)!=0) throw new Exception("Page wrapping");
        foreach(int count in new[] {1,2,3,8,9,12,32,64})
        {
            for(int selected=0;selected<count;selected++)
            {
                float selectedAngle=SpellRingLayout.Angle(selected,count);
                var angles=new HashSet<float>();
                for(int i=0;i<count;i++) if(!angles.Add((SpellRingLayout.Angle(i,count)-selectedAngle+360)%360)) throw new Exception("Overlapping icon angles");
            }
        }
        if(Math.Abs(SpellRingLayout.Angle(1,2)-180)>.001 || Math.Abs(SpellRingLayout.Angle(2,3)-240)>.001) throw new Exception("Uneven icon distribution");
        Console.WriteLine("PASS: icon rings through 64 entries, distinct angles, opposite two-entry layout and book page wrapping.");
    }
}
