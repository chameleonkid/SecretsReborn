using System;
using System.Collections.Generic;

namespace SecretsReborn
{
    [Serializable] public sealed class LearnedSpellData { public string spellId; public int rank; }
    // Persistent host-world character data; active casts and timers are kept elsewhere.
    public sealed class SpellBookState
    {
        private readonly SortedDictionary<string,int> learned=new SortedDictionary<string,int>(StringComparer.Ordinal);
        public const int Capacity=64, MaximumRank=3;
        private static bool ValidId(string id) => !string.IsNullOrWhiteSpace(id) && id.Length<=64 && id==id.Trim();
        public int Rank(string id) => !string.IsNullOrEmpty(id) && learned.TryGetValue(id,out var rank) ? rank : 0;
        public bool Learn(string id,int availableRanks)
        {
            if (!ValidId(id) || availableRanks<1 || availableRanks>MaximumRank || Rank(id)>=availableRanks || Rank(id)==0 && learned.Count>=Capacity) return false;
            learned[id]=Rank(id)+1; return true;
        }
        public LearnedSpellData[] Capture()
        {
            var copy=new LearnedSpellData[learned.Count]; int i=0;
            foreach (var entry in learned) copy[i++]=new LearnedSpellData { spellId=entry.Key,rank=entry.Value }; return copy;
        }
        public static SpellBookState Restore(LearnedSpellData[] data)
        {
            if (data==null || data.Length>Capacity) throw new ArgumentException("Invalid spell book.");
            var book=new SpellBookState();
            foreach (var entry in data)
            {
                if (entry==null || !ValidId(entry.spellId) || entry.rank<1 || entry.rank>MaximumRank || book.learned.ContainsKey(entry.spellId)) throw new ArgumentException("Invalid learned spell.");
                book.learned.Add(entry.spellId,entry.rank);
            }
            return book;
        }
    }
}
