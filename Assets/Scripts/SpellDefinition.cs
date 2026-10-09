using System;
using UnityEngine;

namespace SecretsReborn
{
    public enum SpellElement { Fire,Ice,Light,Shadow,Water,Lightning }
    public enum SpellTargetKind { Enemy,LivingAlly }
    [Serializable] public sealed class SpellRankDefinition
    {
        [Min(1)] public int totalBudget=5;
        [Min(1)] public int visualProjectiles=1;
        [Min(1)] public int manaCost=10;
        [Min(.1f)] public float castTime=.6f,cooldown=2,range=8;
    }
    [CreateAssetMenu(menuName="SecretsReborn/Spell")]
    public sealed class SpellDefinition : ScriptableObject
    {
        [SerializeField] private string spellId,displayName;
        [SerializeField] private SpellElement element;
        [SerializeField] private SpellTargetKind targets;
        [SerializeField] private Sprite icon;
        [SerializeField] private SpellRankDefinition[] ranks=Array.Empty<SpellRankDefinition>();
        public string SpellId=>spellId;
        public string DisplayName=>displayName;
        public SpellElement Element=>element;
        public SpellTargetKind Targets=>targets;
        public Sprite Icon=>icon;
        public int RankCount=>ranks==null ? 0 : Mathf.Min(ranks.Length,SpellBookState.MaximumRank);
        public SpellRankDefinition Rank(int number)=>number>=1 && number<=RankCount ? ranks[number-1] : null;
        public void Configure(string id,string name,SpellElement group,SpellTargetKind target,SpellRankDefinition[] levels)
        { spellId=id; displayName=name; element=group; targets=target; ranks=levels; }
    }
}
