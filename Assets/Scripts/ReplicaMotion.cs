using System;

namespace SecretsReborn
{
    // Presentation between authoritative samples. No prediction or extrapolation
    // through collisions; long gaps and teleports start from the new host pose.
    public sealed class ReplicaMotion
    {
        public const double BlendSeconds = .1;
        private bool initialized;
        private float fromX, fromY, toX, toY;
        private double started;
        public void Push(float x, float y, double now, bool snap = false)
        {
            Sample(now, out var currentX, out var currentY);
            double dx = x - toX, dy = y - toY;
            bool reset = !initialized || snap || now - started > .5 || dx * dx + dy * dy > 6.25;
            fromX = reset ? x : currentX; fromY = reset ? y : currentY;
            toX = x; toY = y; started = now; initialized = true;
        }
        public void Sample(double now, out float x, out float y)
        {
            double fraction = Math.Max(0, Math.Min(1, (now - started) / BlendSeconds));
            x = (float)(fromX + (toX - fromX) * fraction);
            y = (float)(fromY + (toY - fromY) * fraction);
        }
    }
}
