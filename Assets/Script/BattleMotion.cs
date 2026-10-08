using System;

// Pure motion rules, in Ground-local units. No Canvas scale or animation scale enters AI.
public static class BattleMotion
{
    public static float Approach(float position, float target, float range, float speed, float deltaTime)
    {
        float gap = Math.Abs(target - position);
        float step = Math.Min(Math.Max(0f, gap - range), Math.Max(0f, speed * deltaTime));
        return position + Math.Sign(target - position) * step;
    }

    // heroX is sampled AFTER the hero's own movement, BEFORE camera translation.
    // Positive pan moves the entire world to the right. Camera speed is a base
    // speed, not a limit: the left zone edge can force a larger step.
    public static float ZoneCameraStep(float heroX, float zoneLeft, float zoneRight,
        float cameraSpeed, float deltaTime)
    {
        if (deltaTime <= 0f) return 0f;
        float minPan = Math.Min(zoneLeft, zoneRight) - heroX;
        float maxPan = Math.Max(zoneLeft, zoneRight) - heroX;
        float basePan = Math.Max(0f, cameraSpeed) * deltaTime;
        // Recompute from base speed EVERY frame; never retain an edge-follow speed.
        // Inside the zone: keep the base speed. At the right edge: stop or slow
        // just enough to hold the hero. At the left edge: catch up exactly.
        return Math.Max(minPan, Math.Min(maxPan, basePan));
    }

    public static float WaveDelay(float baseDelay, float heroSpeed)
    {
        // Zero speed pauses progress in the caller; no division by zero.
        if (heroSpeed <= 0f) return float.PositiveInfinity;
        return Math.Max(0f, baseDelay) * Math.Max(0.25f, 150f / heroSpeed);
    }

    public static float FlightDuration(float desired, float attackInterval)
    {
        return Math.Min(Math.Max(0.001f, desired), Math.Max(0f, attackInterval) * 0.85f);
    }

    public static float Wrap(float position, float min, float max)
    {
        float length = max - min;
        if (length <= 0f) return position;
        return min + ((position - min) % length + length) % length;
    }
}
