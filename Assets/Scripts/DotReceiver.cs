using UnityEngine;

namespace SecretsReborn
{
    // Shared target-side entry point for spells, weapon hits and future enemy abilities.
    [DisallowMultipleComponent] public sealed class DotReceiver : MonoBehaviour
    {
        private readonly DotState state=new DotState();
        private CharacterInventory actor;
        private TreeMeleeEnemy enemy;
        private WorldSessionState world;
        private DotEffectsView view;
        private int replicaMask;
        private bool Authority => !NetworkCoop.IsReplica && (actor!=null ? actor.HasStateAuthority : enemy!=null && enemy.HasStateAuthority);
        private bool Living => isActiveAndEnabled && (actor!=null ? !GameSession.Instance.World.CharacterVitals(actor.CharacterId).IsDown : enemy!=null && enemy.Alive);
        public int ActiveMask => Living ? (Authority ? state.ActiveMask : replicaMask) : 0;
        private void Awake()
        {
            actor=GetComponent<CharacterInventory>(); enemy=GetComponent<TreeMeleeEnemy>();
            view=GetComponent<DotEffectsView>(); if(view==null) view=gameObject.AddComponent<DotEffectsView>();
        }
        public bool ApplyHost(DotApplication definition)
        {
            if(!Authority || !Living || GameSession.Instance.Busy || NetworkCoop.Active?.ChangingArea==true) return false;
            if(world!=GameSession.Instance.World) { Clear(); world=GameSession.Instance.World; }
            return state.Apply(definition,UnityEngine.Random.value*.99999994d);
        }
        public void ApplyReplica(int mask) { if(!Authority) replicaMask=mask&7; }
        public void Clear() { state.Clear(); replicaMask=0; if(view!=null) view.Clear(); }
        private void Update()
        {
            if(!Living || GameSession.Instance.Busy || NetworkCoop.Active?.ChangingArea==true) { Clear(); return; }
            if(!Authority) return;
            if(world!=GameSession.Instance.World) { Clear(); world=GameSession.Instance.World; }
            if(SaveBook.IsOpen || NetworkCoop.Active?.VotePending==true || (!Application.isFocused && !NetworkCoop.Running)) return;
            if(actor!=null && GameSession.Instance.IsReceivingReward(actor)) return;
            state.Advance(Time.deltaTime,Tick);
        }
        private void Tick(DotKind kind,int damage)
        {
            if(!Living) { Clear(); return; }
            if(enemy!=null) GameSession.Instance.ApplyEnemyDamage(enemy,damage,Vector2.zero,hitReaction:false);
            else if(actor!=null) GameSession.Instance.ApplyDamage(actor,damage);
            if(!Living) Clear();
        }
        private void LateUpdate() => view.Present(ActiveMask);
        private void OnDisable() => Clear();
    }
}
