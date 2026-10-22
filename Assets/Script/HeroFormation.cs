using System.Collections.Generic;
using UnityEngine;

// Movement only. Lives on the EnvironmentManager object, but never changes
// its camera, pan, focus or wave scheduler. One snapshot for all actors/frame.
public sealed class HeroFormation : MonoBehaviour
{
    public enum Phase { Single, Combat, Regrouping, Marching, Halted }
    [Min(1f)] public float spacing=100f;
    [Min(0f)] public float tolerance=5f;
    [Min(0f)] public float regroupHysteresis=10f;
    [Min(.01f)] public float regroupGain=2f;
    [Min(1f)] public float regroupAcceleration=900f;
    [Range(.01f,.9f)] public float minimumMarchFraction=.1f;
    public Phase CurrentPhase { get; private set; }
    public float MarchSpeed { get; private set; }

    private struct Step
    {
        public HeroController hero;
        public int life;
        public float start, end, speed, velocity;
    }
    private readonly List<Step> steps=new List<Step>();
    private int frame=-1;
    private float time=-1f;
    private bool inCombat;

    public static bool TryStep(HeroController hero,EnvironmentManager space,float dt,out float x)
    {
        x=0f;
        if(space==null || space.BattleArea==null)return false;
        var formation=space.GetComponent<HeroFormation>();
        if(formation==null)formation=space.gameObject.AddComponent<HeroFormation>();
        if(formation.frame!=Time.frameCount || formation.time!=Time.time)
            formation.Plan(space,Mathf.Max(0f,dt));
        if(formation.CurrentPhase==Phase.Single || formation.CurrentPhase==Phase.Combat)return false;
        foreach(var step in formation.steps)
            if(step.hero==hero) { x=step.end;return true; }
        return false;
    }

    private void Plan(EnvironmentManager space,float dt)
    {
        frame=Time.frameCount;time=Time.time;
        var living=new List<HeroController>();
        var combatManager=CombatManager.Instance;
        foreach(var hero in HeroController.ActiveHeroes)
        {
            // ActiveHeroes also supplies camera/presentation subjects. Party
            // movement uses B2's authoritative combat registry, not stray visuals.
            var state=combatManager!=null?combatManager.StateFor(hero):null;
            if(hero!=null && hero.IsDeployed && state!=null && state.CanFight)living.Add(hero);
        }
        bool changed=living.Count!=steps.Count;
        foreach(var step in steps)
            if(!living.Contains(step.hero) || step.life!=step.hero.SpawnVersion)changed=true;
        bool combat=false;
        foreach(var monster in MonsterController.ActiveMonsters)
        {
            if(monster==null || !monster.IsAlive)continue;
            foreach(var hero in living)
                if(space.Position(monster.Rect).x<=space.Position(hero.heroRect).x || hero.CanAttack(monster,space))
                { combat=true;break; }
            if(combat)break;
        }
        // Re-form in spatial order after combat/recruitment/death. Never reorder
        // marching actors by stance (which would make them pass through peers).
        bool freshStart=changed && !inCombat;
        if(changed || inCombat || steps.Count<2)
        {
            steps.Clear();
            foreach(var hero in living)
                steps.Add(new Step { hero=hero,life=hero.SpawnVersion,start=space.Position(hero.heroRect).x });
            // Stable insertion sort also handles exactly overlapping spawn points.
            for(int i=1;i<steps.Count;i++)
            {
                var item=steps[i];int j=i-1;
                while(j>=0 && steps[j].start>item.start) { steps[j+1]=steps[j];j--; }
                steps[j+1]=item;
            }
            CurrentPhase=Phase.Regrouping;
        }
        inCombat=combat;
        MarchSpeed=float.MaxValue;
        for(int i=0;i<steps.Count;i++)
        {
            var step=steps[i];step.start=space.Position(step.hero.heroRect).x;
            step.speed=step.hero.EffectiveMoveSpeed();step.end=step.start;
            step.velocity=Mathf.Min(step.speed,step.hero.LastMovementSpeed);
            steps[i]=step;MarchSpeed=Mathf.Min(MarchSpeed,step.speed);
        }
        if(steps.Count<2) { CurrentPhase=Phase.Single;MarchSpeed=steps.Count==0?0f:steps[0].speed;return; }
        if(combat) { CurrentPhase=Phase.Combat;return; }
        // Explicit immobilized state: no endless attempt to regroup around a
        // zero-speed member. A restored speed or new enemy releases it next frame.
        if(MarchSpeed<=0f) { CurrentPhase=Phase.Halted;return; }
        if(CurrentPhase==Phase.Halted)CurrentPhase=Phase.Regrouping;

        float gap=Mathf.Max(1f,spacing), error=0f;
        for(int i=1;i<steps.Count;i++)error=Mathf.Max(error,Mathf.Abs(steps[i].start-steps[i-1].start-gap));
        float allowed=Mathf.Clamp(tolerance,0f,gap*.25f);
        float acceleration=Mathf.Max(1f,regroupAcceleration);
        bool settled=true;
        for(int i=0;i<steps.Count;i++)
        {
            var step=steps[i];
            if(freshStart && step.velocity<=0f)step.velocity=MarchSpeed;
            if(Mathf.Abs(step.velocity-MarchSpeed)>acceleration*dt)settled=false;
            steps[i]=step;
        }
        if(CurrentPhase==Phase.Marching && error>allowed+Mathf.Max(0f,regroupHysteresis))CurrentPhase=Phase.Regrouping;
        if(CurrentPhase==Phase.Regrouping && error<=allowed && settled)CurrentPhase=Phase.Marching;
        for(int i=0;i<steps.Count;i++)
        {
            var step=steps[i];
            float target=MarchSpeed;
            if(CurrentPhase==Phase.Regrouping)
            {
                float ahead=i>0?step.start-steps[i-1].start-gap:0f;
                float behind=i+1<steps.Count?steps[i+1].start-step.start-gap:0f;
                target=Mathf.Max(MarchSpeed*Mathf.Clamp(minimumMarchFraction,.01f,.9f),
                    MarchSpeed+Mathf.Max(.01f,regroupGain)*(ahead-behind));
            }
            // Zero-speed members deliberately halt the common march; combat
            // still bypasses formation. Never boost a slow member to catch up.
            if(MarchSpeed<=0f)target=0f;
            target=Mathf.Clamp(target,0f,step.speed);
            step.velocity=CurrentPhase==Phase.Marching?MarchSpeed:
                Mathf.MoveTowards(step.velocity,target,acceleration*dt);
            step.end=step.start-step.velocity*dt;
            // Collision/overlap safety only. A separated LEADER is never held
            // at an anchor; it keeps a positive, smoothly limited forward speed.
            if(i>0 && CurrentPhase==Phase.Regrouping)
                step.end=Mathf.Min(step.start,Mathf.Max(step.end,steps[i-1].end+gap));
            steps[i]=step;
        }
    }
}
