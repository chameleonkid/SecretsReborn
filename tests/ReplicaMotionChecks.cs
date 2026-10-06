using System;
using SecretsReborn;

public static class ReplicaMotionChecks
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void At(ReplicaMotion motion, double time, float expectedX, float expectedY)
    { motion.Sample(time, out var x, out var y); Check(Math.Abs(x - expectedX) < .001 && Math.Abs(y - expectedY) < .001, "Unexpected pose at " + time); }
    public static void Main()
    {
        var motion = new ReplicaMotion(); motion.Push(0, 0, 0); At(motion, 0, 0, 0);
        motion.Push(.4f, 0, .1); At(motion, .1, 0, 0); At(motion, .15, .2f, 0); At(motion, .2, .4f, 0);
        At(motion, .4, .4f, 0); // Never extrapolate beyond a collision-checked host pose.
        motion.Push(.8f, 0, .2); At(motion, .25, .6f, 0);
        motion.Push(.6f, .4f, .25); At(motion, .25, .6f, 0); At(motion, .3, .6f, .2f); // Bursty arrival is continuous.
        motion.Push(10, 10, .35); At(motion, .35, 10, 10); // Warp.
        motion.Push(10.2f, 10, 1); At(motion, 1, 10.2f, 10); // Long connection gap.
        motion.Push(10.3f, 10, 1.05, true); At(motion, 1.05, 10.3f, 10); // Death/pause.
        Console.WriteLine("PASS: per-frame interpolation, continuity on burst arrivals, bounded position, teleport/gap/death reset.");
    }
}
