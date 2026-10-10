using System;

namespace SecretsReborn
{
    // Matches the two equipment columns and the central lamp below the preview.
    public static class InventoryNavigation
    {
        private static readonly EquipmentSlot[] Left = { EquipmentSlot.Head, EquipmentSlot.Shoulders, EquipmentSlot.Armor, EquipmentSlot.Hands, EquipmentSlot.Waist, EquipmentSlot.Legs, EquipmentSlot.Feet };
        private static readonly EquipmentSlot[] Right = { EquipmentSlot.Ring1, EquipmentSlot.Ring2, EquipmentSlot.Amulet, EquipmentSlot.Seal, EquipmentSlot.Cloak, EquipmentSlot.MainHand, EquipmentSlot.OffHand };
        private static int Clamp(int value, int maximum) => Math.Max(0, Math.Min(maximum, value));
        public static int Navigate(int index, int dx, int dy)
        {
            if (index >= 55) index=30;
            if (index < 40) return Clamp(index / 10 + dy, 3) * 10 + Clamp(index % 10 + dx, 9);
            var slot = (EquipmentSlot)(index - 40);
            if (slot == EquipmentSlot.Lamp)
                return dx > 0 ? 40 + (int)EquipmentSlot.OffHand : dx < 0 || dy < 0 ? 40 + (int)EquipmentSlot.Feet : index;
            int row = Array.IndexOf(Left, slot); bool left = row >= 0;
            if (!left) row = Array.IndexOf(Right, slot);
            if (row < 0) return 40;
            if (row == 6 && (dy > 0 || left && dx > 0 || !left && dx < 0)) return 40 + (int)EquipmentSlot.Lamp;
            row = Clamp(row + dy, 6);
            if (dx < 0) left = true; else if (dx > 0) left = false;
            return 40 + (int)(left ? Left[row] : Right[row]);
        }
    }
}
