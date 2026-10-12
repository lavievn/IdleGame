using System;
using System.Collections.Generic;

// B2 Skill Foundation. Pure runtime models: no scene, prefab or SaveManager dependency.
// A martial-art tier is a distinct move. A practice level (1..9) changes
// efficiency, not its cast animation or targeting rules.
public enum SkillTargetRule { Nearest, LowestHealth }
public enum SkillHitRule { LockedTarget, RetargetOnDeath }

public sealed class SkillTierDefinition
{
    public readonly string ArtId;
    public readonly int Tier, MaximumTier, ProjectileCount;
    public readonly float DamagePercent, ManaAtLevel1, ManaAtLevel9;
    public readonly float CooldownAtLevel1, CooldownAtLevel9;
    public readonly float CastSeconds, ReleaseSeconds, Range, FlightSeconds;
    public readonly SkillTargetRule TargetRule;
    public readonly SkillHitRule HitRule;
    public readonly bool ProtectedUltimate;
    public readonly float PracticeDamageBonusPerLevel;
    // Optional true hit-stun from a skill; damage alone does not imply control.
    public readonly HitControlEffect ControlOnHit;

    public SkillTierDefinition(string artId,int tier,int maximumTier,int projectileCount,
        float damagePercent,float manaAtLevel1,float manaAtLevel9,
        float cooldownAtLevel1,float cooldownAtLevel9,
        float castSeconds,float releaseSeconds,float range,float flightSeconds,
        SkillTargetRule targetRule,SkillHitRule hitRule,bool protectedUltimate=false,
        float practiceDamageBonusPerLevel=0f,HitControlEffect controlOnHit=default(HitControlEffect))
    {
        if (string.IsNullOrEmpty(artId)) throw new ArgumentException("Art id is required");
        if (maximumTier<1 || tier<1 || tier>maximumTier)
            throw new ArgumentOutOfRangeException("tier");
        if (projectileCount<1 || damagePercent<=0f || range<=0f || flightSeconds<0f ||
            manaAtLevel1<0f || manaAtLevel9<0f || cooldownAtLevel1<0f ||
            cooldownAtLevel9<0f || castSeconds<0f || releaseSeconds<0f ||
            practiceDamageBonusPerLevel<0f)
            throw new ArgumentOutOfRangeException("Skill tier has invalid values");
        if (protectedUltimate && tier!=maximumTier)
            throw new ArgumentException("Cast/release immunity is restricted to the highest tier");
        ArtId=artId;Tier=tier;MaximumTier=maximumTier;ProjectileCount=projectileCount;
        DamagePercent=damagePercent;ManaAtLevel1=manaAtLevel1;ManaAtLevel9=manaAtLevel9;
        CooldownAtLevel1=cooldownAtLevel1;CooldownAtLevel9=cooldownAtLevel9;
        CastSeconds=castSeconds;ReleaseSeconds=releaseSeconds;Range=range;FlightSeconds=flightSeconds;
        TargetRule=targetRule;HitRule=hitRule;ProtectedUltimate=protectedUltimate;
        PracticeDamageBonusPerLevel=practiceDamageBonusPerLevel;
        ControlOnHit=controlOnHit;
    }
    private static float ByPractice(float low,float high,int level)
    {
        if (level<1 || level>9) throw new ArgumentOutOfRangeException("level");
        return low+(high-low)*(level-1)/8f;
    }
    public float ManaCost(int level) { return ByPractice(ManaAtLevel1,ManaAtLevel9,level); }
    public float Cooldown(int level) { return ByPractice(CooldownAtLevel1,CooldownAtLevel9,level); }
    public float PowerPercent(int level)
    {
        if (level<1 || level>9) throw new ArgumentOutOfRangeException("level");
        return DamagePercent+(level-1)*PracticeDamageBonusPerLevel;
    }
}

public sealed class MartialArtTierProgress
{
    public int Level { get; private set; } = 1;
    public int Experience { get; private set; }
    // The progression formula is deliberately external: it is not balanced yet.
    public void GainExperience(int amount,Func<int,int> experienceForNextLevel)
    {
        if (amount<0) throw new ArgumentOutOfRangeException("amount");
        if (experienceForNextLevel==null) throw new ArgumentNullException("experienceForNextLevel");
        Experience=checked(Experience+amount);
        while (Level<9)
        {
            int required=experienceForNextLevel(Level);
            if (required<=0) throw new ArgumentException("Level-up threshold must be positive");
            if (Experience<required) break;
            Experience-=required;
            Level++;
        }
    }
}

public sealed class MartialArtKnowledge
{
    private readonly Dictionary<int,MartialArtTierProgress> unlocked =
        new Dictionary<int,MartialArtTierProgress>();
    public bool IsUnlocked(int tier) { return unlocked.ContainsKey(tier); }
    public MartialArtTierProgress At(int tier)
    {
        MartialArtTierProgress progress;
        return unlocked.TryGetValue(tier,out progress)?progress:null;
    }
    public void LearnSingleTier(int tier)
    {
        if (tier<1) throw new ArgumentOutOfRangeException("tier");
        if (!unlocked.ContainsKey(tier)) unlocked[tier]=new MartialArtTierProgress();
    }
    public void LearnCompleteManual(int maximumTier)
    {
        if (maximumTier<1) throw new ArgumentOutOfRangeException("maximumTier");
        for(int i=1;i<=maximumTier;i++) LearnSingleTier(i);
    }
}

public enum SkillCastPhase { Casting, Releasing, Finished }

public sealed class SkillCastContext
{
    public readonly SkillTierDefinition Tier;
    public readonly int PracticeLevel, LifeVersion, SpawnVersion;
    public readonly float ManaPaid, CooldownSeconds, PowerPercent;
    public SkillCastPhase Phase { get; internal set; }
    public float PhaseElapsed { get; internal set; }
    public int EmittedCount { get; internal set; }
    public bool IsProtected => Tier.ProtectedUltimate && Phase!=SkillCastPhase.Finished;
    internal SkillCastContext(SkillTierDefinition tier,int practiceLevel,
        int lifeVersion,int spawnVersion)
    {
        Tier=tier;PracticeLevel=practiceLevel;LifeVersion=lifeVersion;SpawnVersion=spawnVersion;
        ManaPaid=tier.ManaCost(practiceLevel);
        CooldownSeconds=tier.Cooldown(practiceLevel);
        PowerPercent=tier.PowerPercent(practiceLevel);
        Phase=SkillCastPhase.Casting;
    }
    public void Invalidate() { Phase=SkillCastPhase.Finished; }
}

public struct SkillStepResult
{
    public SkillCastContext Cast;
    public int Emissions;
    public bool BeganRelease;
    public bool Completed;
}

public sealed class HeroSkillRuntime
{
    private readonly Dictionary<string,Dictionary<int,SkillTierDefinition>> definitions =
        new Dictionary<string,Dictionary<int,SkillTierDefinition>>();
    private readonly Dictionary<string,MartialArtKnowledge> knowledge =
        new Dictionary<string,MartialArtKnowledge>();
    private readonly Dictionary<string,float> cooldowns = new Dictionary<string,float>();
    private string selectedArt;
    private int selectedTier;
    private SkillCastContext active;
    public bool AutoCast { get; set; }
    public float CurrentMana { get; private set; }
    public float MaximumMana { get; private set; }
    public float ManaRegenPerSecond { get; private set; }
    public SkillCastContext ActiveCast => active;
    public bool IsExecuting => active!=null;
    public bool IsDamageImmune => active!=null && active.IsProtected;
    // Future stun/freeze/silence implementations MUST consult this gate.
    // No crowd-control status system exists in B2 yet.
    public bool IsControlImmune => IsDamageImmune;

    public void ConfigureMana(float maximum,float regeneration,bool fill=false)
    {
        if (maximum<0f || regeneration<0f) throw new ArgumentOutOfRangeException("mana");
        MaximumMana=maximum;ManaRegenPerSecond=regeneration;
        CurrentMana=fill?maximum:Math.Min(CurrentMana,maximum);
    }
    public void RegisterTier(SkillTierDefinition tier)
    {
        if (tier==null) throw new ArgumentNullException("tier");
        Dictionary<int,SkillTierDefinition> set;
        if (!definitions.TryGetValue(tier.ArtId,out set))
        {
            set=new Dictionary<int,SkillTierDefinition>();definitions.Add(tier.ArtId,set);
        }
        else foreach(var existing in set.Values)
            if (existing.MaximumTier!=tier.MaximumTier)
                throw new ArgumentException("Mixed maximum tiers for one martial art");
        set[tier.Tier]=tier;
    }
    public MartialArtKnowledge KnowledgeOf(string artId)
    {
        MartialArtKnowledge k;
        if (!knowledge.TryGetValue(artId,out k))
        {
            k=new MartialArtKnowledge();knowledge.Add(artId,k);
        }
        return k;
    }
    public bool LearnSingleTier(string artId,int tier)
    {
        if (Definition(artId,tier)==null) return false;
        KnowledgeOf(artId).LearnSingleTier(tier);
        return true;
    }
    public bool LearnCompleteManual(string artId)
    {
        Dictionary<int,SkillTierDefinition> set;
        if (!definitions.TryGetValue(artId,out set)||set.Count==0) return false;
        int maximum=0;foreach(var def in set.Values) maximum=def.MaximumTier;
        // Do not claim a complete manual if not all the tier definitions exist.
        for(int i=1;i<=maximum;i++) if (!set.ContainsKey(i)) return false;
        KnowledgeOf(artId).LearnCompleteManual(maximum);
        return true;
    }
    public SkillTierDefinition Definition(string artId,int tier)
    {
        Dictionary<int,SkillTierDefinition> set;
        SkillTierDefinition def;
        return artId!=null && definitions.TryGetValue(artId,out set) &&
            set.TryGetValue(tier,out def)?def:null;
    }
    public SkillTierDefinition Equipped => Definition(selectedArt,selectedTier);
    public MartialArtTierProgress EquippedProgress =>
        selectedArt!=null?KnowledgeOf(selectedArt).At(selectedTier):null;
    public bool Equip(string artId,int tier)
    {
        if (IsExecuting || Definition(artId,tier)==null ||
            !KnowledgeOf(artId).IsUnlocked(tier)) return false;
        selectedArt=artId;selectedTier=tier;
        return true;
    }
    public float CooldownRemaining(string artId)
    {
        float value;return artId!=null && cooldowns.TryGetValue(artId,out value)?value:0f;
    }
    public bool CanBegin
    {
        get
        {
            var def=Equipped;var p=EquippedProgress;
            return def!=null && p!=null && active==null &&
                CooldownRemaining(def.ArtId)<=0f &&
                CurrentMana+0.0001f>=def.ManaCost(p.Level);
        }
    }
    public bool TryBegin(int lifeVersion,int spawnVersion)
    {
        if (!CanBegin) return false;
        var def=Equipped;var progress=EquippedProgress;
        active=new SkillCastContext(def,progress.Level,lifeVersion,spawnVersion);
        CurrentMana=Math.Max(0f,CurrentMana-active.ManaPaid);
        return true;
    }
    public void Cancel()
    {
        if (active!=null) active.Invalidate();
        active=null;
    }
    public void ResetTransientForNewLife()
    {
        Cancel();cooldowns.Clear();CurrentMana=MaximumMana;
    }
    public SkillStepResult Advance(float dt)
    {
        var result=new SkillStepResult();
        if (dt<=0f) return result;
        CurrentMana=Math.Min(MaximumMana,CurrentMana+ManaRegenPerSecond*dt);
        var keys=new List<string>(cooldowns.Keys);
        foreach(var key in keys) cooldowns[key]=Math.Max(0f,cooldowns[key]-dt);
        var cast=active;
        if (cast==null) return result;
        result.Cast=cast;
        float remaining=dt;
        if (cast.Phase==SkillCastPhase.Casting)
        {
            float needed=Math.Max(0f,cast.Tier.CastSeconds-cast.PhaseElapsed);
            float used=Math.Min(needed,remaining);
            cast.PhaseElapsed+=used;remaining-=used;
            if (cast.PhaseElapsed+.00001f>=cast.Tier.CastSeconds)
            {
                cast.Phase=SkillCastPhase.Releasing;cast.PhaseElapsed=0f;
                result.BeganRelease=true;
                // Per-art cooldown starts at release, shared between its tiers:
                // re-equipping a lower tier cannot bypass it.
                cooldowns[cast.Tier.ArtId]=cast.CooldownSeconds;
            }
        }
        if (cast.Phase==SkillCastPhase.Releasing)
        {
            cast.PhaseElapsed=Math.Min(cast.Tier.ReleaseSeconds,cast.PhaseElapsed+remaining);
            // Emissions are spread across the FULL release animation.
            // The first projectile fires as soon as casting completes.
            while(cast.EmittedCount<cast.Tier.ProjectileCount)
            {
                float threshold=cast.Tier.ProjectileCount==1?0f:
                    cast.Tier.ReleaseSeconds*cast.EmittedCount/(cast.Tier.ProjectileCount-1);
                if (cast.PhaseElapsed+.00001f<threshold) break;
                cast.EmittedCount++;result.Emissions++;
            }
            if (cast.PhaseElapsed+.00001f>=cast.Tier.ReleaseSeconds &&
                cast.EmittedCount==cast.Tier.ProjectileCount)
            {
                cast.Invalidate();active=null;result.Completed=true;
            }
        }
        return result;
    }
}
