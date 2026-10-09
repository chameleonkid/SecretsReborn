using System;
using SecretsReborn;
internal static class SpellBookChecks
{
    static void Check(bool value,string why) { if (!value) throw new Exception(why); }
    public static void Main()
    {
        var book=new SpellBookState(); Check(book.Rank("fireball")==0,"starts unlearned");
        Check(book.Learn("fireball",3) && book.Rank("fireball")==1,"learn");
        Check(book.Learn("fireball",3) && book.Learn("fireball",3) && !book.Learn("fireball",3),"ranks capped");
        Check(!book.Learn("bad ",3) && !book.Learn("heal",4),"invalid input");
        var save=book.Capture(); save[0].rank=1; Check(book.Rank("fireball")==3,"save copy isolated");
        Check(SpellBookState.Restore(book.Capture()).Rank("fireball")==3,"roundtrip");
        bool rejected=false; try { SpellBookState.Restore(new[] { new LearnedSpellData { spellId="heal",rank=1 },new LearnedSpellData { spellId="heal",rank=2 } }); } catch (ArgumentException) { rejected=true; }
        Check(rejected,"duplicate IDs rejected");
        Console.WriteLine("PASS: learned ranks, caps, invalid data, isolated save copies and roundtrip.");
    }
}
