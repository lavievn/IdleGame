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
        // The terrain is carried by backgroundPan, while actors receive cameraPan
        // in EnvironmentManager.LateUpdate. Apply their difference EVERY frame,
        // even while standing to attack, so a ranged defender stays attached to Ground.
        float carrier = groundMinusCamera;
        float gap = target - position - range;
        // Only walking is range-clamped. Carrier correction must never be discarded
        // when the monster is already inside attack range.
        float walk = Math.Min(Math.Max(0f, gap - carrier), Math.Max(0f, speed) * dt);
        return position + carrier + walk;
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

    // 0.0.50: pure-C# critically damped pan for combatants standing still.
    // Pan is shared across actors, projectiles and Ground; never changes AI coords.
    public static float StationaryCombatPan(float remainingDistance, ref float velocity,
        float smoothTime, float dt)
    {
        if (dt <= 0f || remainingDistance <= 0f)
        {
            velocity = 0f;
            return 0f;
        }
        smoothTime = Math.Max(.05f, smoothTime);
        float omega = 2f / smoothTime;
        float x = omega * dt;
        float exponent = 1f / (1f + x + .48f*x*x + .235f*x*x*x);
        float change = -remainingDistance; // current pan is 0 each frame.
        float temp = (velocity + omega * change) * dt;
        velocity = (velocity - omega * temp) * exponent;
        float pan = remainingDistance + (change + temp) * exponent;
        if (pan < 0f)
        {
            velocity = 0f;
            return 0f;
        }
        if (pan >= remainingDistance)
        {
            velocity = 0f;
            return remainingDistance;
        }
        return pan;
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


// A deadzone is a REGION, not a fixed point the Hero must occupy.
// SmoothDamp uses the same 1D spring approximation as Unity Mathf.SmoothDamp;
// we keep it in pure C# so the camera math is testable without Unity.
public sealed class DeadzoneCamera
{
    // Only retain velocity. An idle game may run for days: a cumulative
    // camera world offset would eventually lose float precision.
    private float velocity;
    private bool wasWalking;
    private bool idleRecovering;
    public float Velocity => velocity;
    public bool IsIdleRecovering => idleRecovering;

    public void Reset()
    {
        velocity = 0f;
        wasWalking = false;
        idleRecovering = false;
    }

    private static float Clamp(float x, float lo, float hi)
        => Math.Max(lo, Math.Min(hi, x));

    // All inputs use the stable Ground-local coordinate system. Hero movement
    // is already applied before this call. Return ONE shared pan for actors,
    // attacks and environment; zero when idle inside the deadzone.
    // Compatibility overload for existing single-Hero camera tests and users.
    public float Pan(float focusScreenX, float redLeft, float redRight,
        float viewLeft, float viewRight, float dt, float smoothTime,
        float deadzoneWidthRatio, float deadzoneCenterRatio,
        bool heroMovedThisFrame, float idleRestRatio, float idleSmoothTime)
    {
        return Pan(focusScreenX,redLeft,redRight,viewLeft,viewRight,dt,
            smoothTime,deadzoneWidthRatio,deadzoneCenterRatio,
            heroMovedThisFrame,idleRestRatio,idleSmoothTime,focusScreenX);
    }

    // The focus may be a multi-combatant center while safety still protects
    // the actual Hero. Decoupling these prevents an off-center monster group
    // from pushing the Hero beyond the physical screen edge.
    public float Pan(float focusScreenX, float redLeft, float redRight,
        float viewLeft, float viewRight, float dt, float smoothTime,
        float deadzoneWidthRatio, float deadzoneCenterRatio,
        bool heroMovedThisFrame, float idleRestRatio, float idleSmoothTime,
        float protectedHeroX)
    {
        if (dt <= 0f || viewRight <= viewLeft) return 0f;
        float width = viewRight - viewLeft;
        float center = viewLeft + width * Clamp(deadzoneCenterRatio, .4f, .6f);
        // Keep deadzone narrower than the outer red limits. The default
        // 38% Ground width starts following close to the red border rather
        // than as soon as Hero moves a little from the center.
        float half = .5f * width * Clamp(deadzoneWidthRatio, .02f, .45f);
        float redHalf = Math.Min(center - Math.Min(redLeft, redRight),
            Math.Max(redLeft, redRight) - center);
        if (redHalf > 0f) half = Math.Min(half, Math.Max(0f, redHalf - width * .005f));
        float left = center - half, right = center + half;

        // While running, retain the tested broad deadzone unchanged.
        // On RUN -> STOP, recover toward 40% only if Hero had actually
        // moved ahead of that position. A Hero idle since spawn, or a slow
        // Hero stopped near the center, must NOT make the camera auto-pan.
        float idleRestX = viewLeft + width * Clamp(idleRestRatio, .3f, .5f);
        if (heroMovedThisFrame)
        {
            wasWalking = true;
            idleRecovering = false;
        }
        else if (wasWalking)
        {
            wasWalking = false;
            idleRecovering = focusScreenX < idleRestX - 1f;
        }

        float correction = focusScreenX < left ? left - focusScreenX
            : focusScreenX > right ? right - focusScreenX : 0f;
        if (idleRecovering)
        {
            // Ground and Hero drift RIGHT together after the Hero stops
            // attacking. SmoothDamp retains existing velocity and eases
            // the recovery to a finite rest position; no autonomous scroll.
            correction = Math.Max(0f, idleRestX - focusScreenX);
            smoothTime = Math.Max(.05f, idleSmoothTime);
        }
        else if (focusScreenX < Math.Min(redLeft, redRight)
            || focusScreenX > Math.Max(redLeft, redRight))
        {
            // The red boundary only increases follow responsiveness.
            smoothTime = Math.Max(.08f, smoothTime * .55f);
        }
        float pan = SmoothDamp(0f, correction, ref velocity, smoothTime, dt);
        if (idleRecovering && pan < 0f)
        {
            // Never shake left and right while recovering from a stop.
            pan = 0f;
            velocity = 0f;
            idleRecovering = false;
        }

        // Red-zone boundaries are NOT a clamp. Protect the actual Hero,
        // rather than the centroid, at the real physical screen edges.
        float safety = Math.Min(30f, width * .03f);
        float clampedPan = Clamp(pan,
            viewLeft + safety - protectedHeroX,
            viewRight - safety - protectedHeroX);
        if (Math.Abs(clampedPan - pan) > .0001f)
            velocity = 0f; // Don't retain momentum from a physically clipped step.
        if (idleRecovering && idleRestX - (focusScreenX + clampedPan) <= .5f
            && Math.Abs(velocity) < 2f)
        {
            // Stop once, without repeated re-centering every idle frame.
            idleRecovering = false;
            velocity = 0f;
        }
        if (Math.Abs(velocity) < .01f && Math.Abs(clampedPan) < .001f)
            velocity = 0f;
        return clampedPan;
    }

    // Standard Unity SmoothDamp spring approximation, specialized to float.
    private static float SmoothDamp(float current, float target,
        ref float currentVelocity, float smoothTime, float deltaTime)
    {
        smoothTime = Math.Max(.0001f, smoothTime);
        float omega = 2f / smoothTime;
        float x = omega * deltaTime;
        float exponent = 1f / (1f + x + .48f*x*x + .235f*x*x*x);
        float change = current - target;
        float originalTarget = target;
        float temp = (currentVelocity + omega * change) * deltaTime;
        currentVelocity = (currentVelocity - omega * temp) * exponent;
        float output = target + (change + temp) * exponent;
        // Match Unity's no-overshoot branch when passing a moving target.
        if ((originalTarget - current > 0f) == (output > originalTarget))
        {
            output = originalTarget;
            currentVelocity = 0f;
        }
        return output;
    }
}

// Smooth only the relative offset between the existing combat centroid and
// the tracked Hero. Following Hero's own movement keeps its .54g response;
// sudden spawn/death/viewport changes move the centroid offset gradually.
// The offset is invariant under the ONE pan shared by Hero and monsters.
public sealed class CombatFocusSmoother
{
    private float offset;
    private float velocity;

    public float Offset => offset;
    public float Velocity => velocity;

    public void Reset()
    {
        offset = 0f;
        velocity = 0f;
    }

    // Change the anchor Hero without changing the absolute filtered focus or
    // its velocity. Only map initialization should discard presentation state.
    public void Rebase(float delta) { offset += delta; }

    public float Step(float rawOffset, float dt, float smoothTime, float maxSpeed)
    {
        if (dt <= 0f) return offset;
        smoothTime = Math.Max(.05f, smoothTime);
        maxSpeed = Math.Max(1f, maxSpeed);

        float omega = 2f / smoothTime;
        float x = omega * dt;
        float exponent = 1f / (1f + x + .48f*x*x + .235f*x*x*x);

        // Unity SmoothDamp's max-speed-limited critically damped update.
        // Cap the relative offset change before filtering so new monsters
        // cannot pull the focus across the screen in one or two frames.
        float change = offset - rawOffset;
        float maxChange = maxSpeed * smoothTime;
        change = Math.Max(-maxChange, Math.Min(maxChange, change));
        float adjustedTarget = offset - change;
        float temp = (velocity + omega * change) * dt;
        velocity = (velocity - omega * temp) * exponent;
        float next = adjustedTarget + (change + temp) * exponent;

        // Keep the offset inside the target, never spring past it.
        if ((rawOffset - offset > 0f) == (next > rawOffset))
        {
            next = rawOffset;
            velocity = 0f;
        }
        offset = next;
        if (Math.Abs(rawOffset - offset) < .001f && Math.Abs(velocity) < .01f)
        {
            offset = rawOffset;
            velocity = 0f;
        }
        return offset;
    }
}

// One velocity controller for map entry AND ordinary gameplay. Ground units/s;
// no cumulative world offset, camera teleport, coroutine, or separate entry pan.
public sealed class MultiZoneCamera
{
    public float Velocity { get; private set; }
    public void Reset() { Velocity=0f; }
    private static float Valid(float value,float fallback)
        => float.IsNaN(value)||float.IsInfinity(value)||value<=0f?fallback:value;
    private static float Clamp(float v,float lo,float hi) => Math.Max(lo,Math.Min(hi,v));
    private static float Blend(float a,float b,float t)
    {
        t=Clamp(t,0f,1f);return a+(b-a)*t*t*(3f-2f*t);
    }
    public static float ResponseSpeed(float distance,float width,float green,float yellow,float red,
        float minimum,float maximum,float correction)
    {
        green=Clamp(Valid(green,.2f),.01f,.96f)*width*.5f;
        yellow=Clamp(Valid(yellow,.45f),green*2f/width+.01f,.98f)*width*.5f;
        red=Clamp(Valid(red,.75f),yellow*2f/width+.01f,1f)*width*.5f;
        minimum=Valid(minimum,30f);maximum=Math.Max(minimum,Valid(maximum,450f));
        correction=Math.Max(maximum,Valid(correction,900f));
        float middle=Math.Max(minimum,maximum*.5f);
        // Nonzero crawl near the center, bounded by stopping distance below:
        // a stationary focus reaches rest rather than an endless position Lerp.
        if(distance<=green)return Blend(minimum*.2f,minimum,distance/green);
        if(distance<=yellow)return Blend(minimum,middle,(distance-green)/(yellow-green));
        if(distance<=red)return Blend(middle,maximum,(distance-yellow)/(red-yellow));
        return Blend(maximum,correction,(distance-red)/Math.Max(1f,width*.5f-red));
    }
    public float Pan(float focus,float left,float right,float dt,float green,float yellow,float red,
        float minimum,float maximum,float acceleration,float deceleration,float correction,float maxStep)
    {
        if(dt<=0f || right<=left || float.IsNaN(focus) || float.IsInfinity(focus))return 0f;
        acceleration=Valid(acceleration,900f);deceleration=Valid(deceleration,1200f);
        float limit=Math.Max(Valid(maximum,450f),Valid(correction,900f));
        // A stalled render frame cannot apply an entire catch-up journey at once.
        float remaining=Math.Min(dt,Valid(maxStep,48f)/limit),pan=0f;
        while(remaining>0f)
        {
            float h=Math.Min(remaining,1f/120f);remaining-=h;
            float error=(left+right)*.5f-focus-pan;
            if(Math.Abs(error)<.001f && Math.Abs(Velocity)<.01f) { Velocity=0f;break; }
            float speed=ResponseSpeed(Math.Abs(error),right-left,green,yellow,red,minimum,maximum,correction);
            // Brake BEFORE reaching the center. Substeps keep this continuous
            // across frame rates and through zone boundaries/direction reversals.
            speed=Math.Min(speed,(float)Math.Sqrt(2f*deceleration*Math.Abs(error)));
            float desired=Math.Sign(error)*speed;
            bool braking=Velocity*desired<0f || Math.Abs(desired)<Math.Abs(Velocity);
            float delta=(braking?deceleration:acceleration)*h;
            float next=Velocity+Clamp(desired-Velocity,-delta,delta);
            float step=(Velocity+next)*.5f*h;
            if(Velocity*desired<0f && Math.Abs(Velocity)<deceleration*h)
            {
                // Split a reversal at zero: braking time cannot be reused at
                // the higher deceleration rate to accelerate the other way.
                float stopTime=Math.Abs(Velocity)/deceleration,runTime=h-stopTime;
                next=Math.Sign(desired)*Math.Min(Math.Abs(desired),acceleration*runTime);
                step=Velocity*stopTime*.5f+next*runTime*.5f;
            }
            // Finish a small, physically brakeable final step without oscillation.
            if(step*error>=0f && Math.Abs(step)>=Math.Abs(error) && Math.Abs(Velocity)<=deceleration*h)
            { pan+=error;Velocity=0f;break; }
            pan+=step;Velocity=next;
        }
        return pan;
    }
}
