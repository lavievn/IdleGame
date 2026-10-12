using System;

// Actual crowd control is opt-in. Every ordinary hit can play a cosmetic
// reaction, but it never interrupts movement, normal attacks or casting.
// Weapon/passive/skill sources may attach this profile to the hit.
public struct HitControlEffect
{
    public float Chance;
    public float StaggerSeconds;
    public float GraceAfterSeconds;

    public HitControlEffect(float chance,float staggerSeconds,float graceAfterSeconds)
    {
        if (chance<0f || chance>1f || staggerSeconds<0f || graceAfterSeconds<0f)
            throw new ArgumentOutOfRangeException("hit control values");
        Chance=chance;
        StaggerSeconds=staggerSeconds;
        GraceAfterSeconds=graceAfterSeconds;
    }
    public bool CanStagger => Chance>0f && StaggerSeconds>0f;
}

// One instance per Hero/monster lifetime. Hit-stun is short, non-stackable,
// and followed by resistance: multiple fast attackers cannot stunlock forever.
public sealed class HitControlState
{
    public const float MaximumMiniStaggerSeconds = .25f;
    public const float MinimumPostStaggerGraceSeconds = .6f;

    public float StaggerRemaining { get; private set; }
    public float GraceRemaining { get; private set; }
    public float StaggerResistance { get; set; } // 0..1; boss may use 1.
    public bool IsStaggered => StaggerRemaining>0f;

    public void Reset()
    {
        StaggerRemaining=0f;GraceRemaining=0f;
        // Pool reuse may bind a different kind of monster to this controller.
        // Any equipment/boss resistance must be reapplied for the new life.
        StaggerResistance=0f;
    }

    public void Advance(float dt)
    {
        if (dt<=0f) return;
        StaggerRemaining=Math.Max(0f,StaggerRemaining-dt);
        GraceRemaining=Math.Max(0f,GraceRemaining-dt);
    }

    public bool TryApply(HitControlEffect effect,float randomRoll,bool immune=false)
    {
        if (!effect.CanStagger || immune || StaggerRemaining>0f ||
            GraceRemaining>0f) return false;
        float resistance=Math.Max(0f,Math.Min(1f,StaggerResistance));
        float probability=effect.Chance*(1f-resistance);
        if (randomRoll<0f || randomRoll>1f)
            throw new ArgumentOutOfRangeException("randomRoll");
        if (probability<1f && randomRoll>=probability) return false;
        float duration=Math.Min(MaximumMiniStaggerSeconds,effect.StaggerSeconds);
        StaggerRemaining=duration;
        GraceRemaining=duration+Math.Max(MinimumPostStaggerGraceSeconds,effect.GraceAfterSeconds);
        return true;
    }
}
