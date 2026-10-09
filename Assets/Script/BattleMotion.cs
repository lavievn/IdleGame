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

    public static float ForwardApproach(float position, float target, float range, float speed, float dt, int direction)
    {
        float ahead = (target - position) * direction;
        float step = Math.Min(Math.Max(0f, ahead - range), Math.Max(0f, speed) * Math.Max(0f, dt));
        return position + direction * step;
    }

    public static float MonsterApproach(float position, float target, float range, float speed, float dt, float groundMinusCamera)
    {
        if (dt <= 0f) return position;
        float gap = target - position - range;
        if (gap <= 0f) return position;
        // This signed offset changes the ground's carrier motion, not the monster's own heading.
        // The subsequent common camera pan yields base movement + ground pan on screen.
        return position + Math.Min(gap, Math.Max(0f, speed) * dt + groundMinusCamera);
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


// Presentation-only timing. Never feeds the world/camera/combat position system.
public static class MapTitleMotion
{
    public const float HoldSeconds = 3f;
    public const float TravelSeconds = 0.85f;
    public static float Progress(float elapsed)
    {
        float t = Math.Max(0f,Math.Min(1f,(elapsed - HoldSeconds) / TravelSeconds));
        return t*t*(3f-2f*t);
    }
}
public static class DamagePopupMotion
{
    public const float Duration = 1f;
    public static float AwaySign(float victimX,float attackerX,float fallback)
    {
        float d = victimX - attackerX;
        return Math.Abs(d) < .001f ? (fallback >= 0f ? 1f : -1f) : (d > 0f ? 1f : -1f);
    }
    private static float T(float elapsed) { return Math.Max(0f,Math.Min(1f,elapsed/Duration)); }
    public static float X(float elapsed,float sign)
    {
        float t=T(elapsed),pop=1f-(1f-t)*(1f-t)*(1f-t);
        return (sign>=0f?1f:-1f)*48f*pop;
    }
    public static float Y(float elapsed)
    {
        float t=T(elapsed),pop=1f-(1f-t)*(1f-t)*(1f-t);
        return 17f*pop + 20f*(float)Math.Sin(Math.PI*t);
    }
    public static float Alpha(float elapsed) { return 1f-T(elapsed); }
}
