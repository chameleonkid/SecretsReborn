using System;

namespace SecretsReborn
{
    public enum CoopAction { Input, MoveItem, Equip, Unequip, UseItem, Pickup, Chest, Attack, Lamp, ConfirmReward, BindPotion, UsePotion, StashDeposit, StashWithdraw, CastSpell, CancelSpell, SpellSelection }
    [Serializable] public sealed class CoopCommand
    {
        public long sequence;
        public int areaEpoch;
        public CoopAction action;
        public int from, to;
        public float x, y;
        public bool menuOpen;
        public string target;
        public string expectedItem;
        public int expectedCount;
    }
    [Serializable] public sealed class CoopHello
    {
        public int protocol = 20;
        public string characterToken, scene, playerName;
    }
    [Serializable] public sealed class CoopActorPose
    {
        public string id;
        public float x, y, dx, dy, reviveProgress, potionCooldown;
        public bool lamp, reviveInterrupted;
        public int swing;
        public SpellCastSnapshot cast;
    }
    [Serializable] public sealed class ChestRewardState
    {
        public string characterId, chestId, itemId;
        [NonSerialized] public double confirmAt;
    }
    [Serializable] public sealed class CoopEnemyPose
    {
        public string id;
        public float x, y;
        public int frame;
        public bool active;
    }
    [Serializable] public sealed class CoopSnapshot
    {
        public int protocol = 20;
        public long sequence;
        public int areaEpoch;
        public string localCharacter, scene;
        public bool gameOver, paused;
        public SaveGameData world;
        public CoopActorPose[] actors;
        public CoopEnemyPose[] enemies;
        public ChestRewardState[] rewards;
    }
    [Serializable] public sealed class CoopAreaMessage
    {
        public int epoch;
        public string scene, entrance;
        public bool load;
    }
    [Serializable] public sealed class CoopVote
    {
        public long id;
        public string scene;
        public bool load, answer, cancel;
        public bool retry, initialCheckpoint;
    }
    // Pure boundary checks: sender-to-character ownership is resolved by the host,
    // never from a character ID, damage amount or position supplied in a command.
    public static class CoopProtocol
    {
        public const int MaximumPlayers = 4;
        public static bool MatchesAreaReady(CoopAreaMessage ready, CoopAreaMessage expected) => ready != null && expected != null
            && ready.epoch == expected.epoch && ready.scene == expected.scene && ready.load == expected.load
            && (ready.entrance ?? "") == (expected.entrance ?? "");
        // JsonUtility expands null class-array entries to zero/default objects and
        // null strings to empty strings. Canonicalize only these wire empty slots;
        // the persisted SaveGameData validator remains strict and unchanged.
        public static void RestoreWireEmptySlots(SaveGameData data)
        {
            if (data?.characters == null) return;
            if (data.sharedStash != null) for (int i = 0; i < data.sharedStash.Length; i++)
                if (data.sharedStash[i] != null && data.sharedStash[i].count == 0 && string.IsNullOrEmpty(data.sharedStash[i].itemId)) data.sharedStash[i] = null;
            foreach (var character in data.characters)
            {
                if (character?.bag != null)
                    for (int i = 0; i < character.bag.Length; i++)
                    {
                        var stack = character.bag[i];
                        if (stack != null && stack.count == 0 && string.IsNullOrEmpty(stack.itemId)) character.bag[i] = null;
                    }
                if (character?.equipment != null)
                    for (int i = 0; i < character.equipment.Length; i++)
                        if (character.equipment[i] == "") character.equipment[i] = null;
                if (character?.potionItems != null)
                    for (int i = 0; i < character.potionItems.Length; i++)
                        if (character.potionItems[i] == "") character.potionItems[i] = null;
            }
        }
        public static bool ValidHello(CoopHello hello, string scene) => hello != null && hello.protocol == 20
            && hello.scene == scene && Guid.TryParseExact(hello.characterToken, "N", out _);
        public static bool Valid(CoopCommand command)
        {
            if (command == null || command.sequence <= 0 || command.areaEpoch < 0 || !Enum.IsDefined(typeof(CoopAction), command.action)
                || command.target != null && command.target.Length > 160) return false;
            switch (command.action)
            {
                case CoopAction.SpellSelection: return command.from==0 || command.from==1;
                case CoopAction.CastSpell: return (command.from==0 || command.from==1) && !string.IsNullOrWhiteSpace(command.expectedItem) && command.expectedItem.Length<=64
                    && (command.from==1 || !string.IsNullOrWhiteSpace(command.target));
                case CoopAction.Input: return Finite(command.x) && Finite(command.y)
                    && Math.Abs(command.x) <= 1 && Math.Abs(command.y) <= 1;
                case CoopAction.MoveItem: return Bag(command.from) && Bag(command.to);
                case CoopAction.Equip: return Bag(command.from) && Equipment(command.to);
                case CoopAction.Unequip: return Equipment(command.from) && (command.to == -1 || Bag(command.to));
                case CoopAction.UseItem: return Bag(command.from);
                case CoopAction.UsePotion: return command.from >= 0 && command.from < 2;
                case CoopAction.BindPotion: return (command.from == -1 || Bag(command.from)) && command.to >= 0 && command.to < 2;
                case CoopAction.StashDeposit:
                case CoopAction.StashWithdraw: return Bag(command.from) && Bag(command.to) && !string.IsNullOrWhiteSpace(command.target)
                    && !string.IsNullOrWhiteSpace(command.expectedItem) && command.expectedItem.Length <= 160 && command.expectedCount > 0;
                case CoopAction.Pickup:
                case CoopAction.ConfirmReward:
                case CoopAction.Chest: return !string.IsNullOrWhiteSpace(command.target);
                default: return true;
            }
        }
        public static bool Valid(CoopCommand command, int currentAreaEpoch) => command != null
            && command.areaEpoch == currentAreaEpoch && Valid(command);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Bag(int slot) => slot >= 0 && slot < InventoryState.Capacity;
        private static bool Equipment(int slot) => slot >= 0 && slot < InventoryState.EquipmentCapacity;
    }
}
