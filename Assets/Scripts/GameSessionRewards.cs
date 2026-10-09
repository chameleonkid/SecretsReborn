using System;
using System.Collections.Generic;
using UnityEngine;

namespace SecretsReborn
{
    public sealed partial class GameSession
    {
        private readonly Dictionary<string, ChestRewardState> chestRewards = new Dictionary<string, ChestRewardState>();
        private readonly Dictionary<string, int> rewardConfirmedFrames = new Dictionary<string, int>();
        public bool HasAnyReward => chestRewards.Count != 0;
        public bool RewardPresentationActive
        {
            get
            {
                var actor = NetworkCoop.Running ? NetworkCoop.Active.LocalCharacter
                    : Camera.main?.GetComponent<CameraFollow>()?.Target?.GetComponent<CharacterInventory>();
                return IsReceivingReward(actor);
            }
        }
        public bool IsReceivingReward(CharacterInventory actor) => actor != null && chestRewards.ContainsKey(actor.CharacterId);
        public bool RewardInputBlocked(CharacterInventory actor) => IsReceivingReward(actor)
            || actor != null && rewardConfirmedFrames.TryGetValue(actor.CharacterId, out var frame) && frame == Time.frameCount;
        internal ChestRewardState RewardFor(CharacterInventory actor) => actor != null && chestRewards.TryGetValue(actor.CharacterId, out var reward) ? reward : null;
        internal bool BeginChestReward(TreasureChest chest, CharacterInventory actor, ItemDefinition item)
        {
            if (!CanFight(actor) || chest == null || item == null || IsReceivingReward(actor)) return false;
            chestRewards[actor.CharacterId] = new ChestRewardState
            { characterId = actor.CharacterId, chestId = chest.ChestId, itemId = item.ItemId, confirmAt = Time.unscaledTimeAsDouble + .45 };
            CancelRevive(actor);
            actor.GetComponent<PlayerMovement>()?.SetRewardImmobilized(true);
            var body = actor.GetComponent<Rigidbody2D>(); if (body != null) body.linearVelocity = Vector2.zero;
            return true;
        }
        internal bool ConfirmChestReward(CharacterInventory actor, string chestId)
        {
            if (actor == null || !chestRewards.TryGetValue(actor.CharacterId, out var reward) || reward.chestId != chestId) return false;
            if (NetworkCoop.Request(actor, CoopAction.ConfirmReward, target: chestId)) return true;
            if (!actor.HasStateAuthority || Busy || Time.unscaledTimeAsDouble < reward.confirmAt) return false;
            chestRewards.Remove(actor.CharacterId); actor.GetComponent<PlayerMovement>()?.SetRewardImmobilized(false);
            rewardConfirmedFrames[actor.CharacterId] = Time.frameCount; return true;
        }
        internal void ReleaseChestReward(string characterId) { chestRewards.Remove(characterId); rewardConfirmedFrames.Remove(characterId); }
        internal ChestRewardState[] CaptureChestRewards() => new List<ChestRewardState>(chestRewards.Values).ToArray();
        internal void AcceptChestRewards(ChestRewardState[] rewards)
        {
            if (!NetworkCoop.IsReplica) return;
            chestRewards.Clear();
            foreach (var reward in rewards ?? Array.Empty<ChestRewardState>())
                if (reward != null && !string.IsNullOrWhiteSpace(reward.characterId)) chestRewards[reward.characterId] = reward;
        }
    }
}
