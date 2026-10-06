using System;

namespace SecretsReborn
{
    public enum CoopAction { Input, MoveItem, Equip, Unequip, UseItem, Pickup, Chest, Attack, Lamp }
    [Serializable] public sealed class CoopCommand
    {
        public long sequence;
        public CoopAction action;
        public int from, to;
        public float x, y;
        public bool menuOpen;
        public string target;
    }
    [Serializable] public sealed class CoopHello
    {
        public int protocol = 1;
        public string characterToken, scene;
    }
    [Serializable] public sealed class CoopActorPose
    {
        public string id;
        public float x, y, dx, dy, reviveProgress;
        public bool lamp, reviveInterrupted;
        public int swing;
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
        public int protocol = 1;
        public long sequence;
        public string localCharacter, scene;
        public bool gameOver, paused;
        public SaveGameData world;
        public CoopActorPose[] actors;
        public CoopEnemyPose[] enemies;
    }
    // Pure boundary checks: sender-to-character ownership is resolved by the host,
    // never from a character ID, damage amount or position supplied in a command.
    public static class CoopProtocol
    {
        public const int MaximumPlayers = 4;
        // JsonUtility expands null class-array entries to zero/default objects and
        // null strings to empty strings. Canonicalize only these wire empty slots;
        // the persisted SaveGameData validator remains strict and unchanged.
        public static void RestoreWireEmptySlots(SaveGameData data)
        {
            if (data?.characters == null) return;
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
            }
        }
        public static bool ValidHello(CoopHello hello, string scene) => hello != null && hello.protocol == 1
            && hello.scene == scene && Guid.TryParseExact(hello.characterToken, "N", out _);
        public static bool Valid(CoopCommand command)
        {
            if (command == null || command.sequence <= 0 || !Enum.IsDefined(typeof(CoopAction), command.action)
                || command.target != null && command.target.Length > 160) return false;
            switch (command.action)
            {
                case CoopAction.Input: return Finite(command.x) && Finite(command.y)
                    && Math.Abs(command.x) <= 1 && Math.Abs(command.y) <= 1;
                case CoopAction.MoveItem: return Bag(command.from) && Bag(command.to);
                case CoopAction.Equip: return Bag(command.from) && Equipment(command.to);
                case CoopAction.Unequip: return Equipment(command.from) && (command.to == -1 || Bag(command.to));
                case CoopAction.UseItem: return Bag(command.from);
                case CoopAction.Pickup:
                case CoopAction.Chest: return !string.IsNullOrWhiteSpace(command.target);
                default: return true;
            }
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Bag(int slot) => slot >= 0 && slot < InventoryState.Capacity;
        private static bool Equipment(int slot) => slot >= 0 && slot < InventoryState.EquipmentCapacity;
    }
}
