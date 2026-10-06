using System;

namespace SecretsReborn
{
    // Transient host timers: deliberately not saved across loads.
    public sealed class CombatCooldown
    {
        private double readyAt;
        public bool TryUse(double now, double duration)
        {
            if (double.IsNaN(now) || double.IsInfinity(now) || now < 0 || double.IsNaN(duration) || double.IsInfinity(duration) || duration <= 0 || now < readyAt) return false;
            readyAt = now + duration; return true;
        }
    }
    public static class CombatRules
    {
        public static int MitigatedDamage(int amount, int armor)
        {
            if (amount <= 0) return 0;
            long denominator = 100L + Math.Max(0, armor);
            return (int)Math.Max(1, (amount * 100L + denominator - 1) / denominator);
        }
        // Frame rows verified against the original LogWalkDown/Up/Right/Left clips.
        public static int LogFacingRow(float x, float y) => Math.Abs(x) > Math.Abs(y) ? (x > 0 ? 2 : 3) : (y > 0 ? 1 : 0);
        public static bool InArc(float dx, float dy, float facingX, float facingY, float range)
        {
            float distanceSquared = dx * dx + dy * dy;
            float facingSquared = facingX * facingX + facingY * facingY;
            if (range <= 0 || float.IsNaN(distanceSquared) || float.IsInfinity(distanceSquared) || facingSquared < .01f || distanceSquared > range * range) return false;
            if (distanceSquared < .01f) return true;
            // A 120 degree sweep in front of the character.
            return dx * facingX + dy * facingY >= .5 * Math.Sqrt(distanceSquared * facingSquared);
        }
    }
}
