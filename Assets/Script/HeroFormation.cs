using System.Collections.Generic;
using UnityEngine;

// Movement only. Lives on the EnvironmentManager object, but never changes
// its camera, pan, focus or wave scheduler. One snapshot for all actors/frame.
public sealed class HeroFormation : MonoBehaviour
{
    public enum Phase { Single, Combat, Regrouping, Marching }
    [Min(1f)] public float spacing=100f;
    [Min(0f)] public float tolerance=5f;
    [Min(0f)] public float regroupHysteresis=10f;
    public Phase CurrentPhase { get; private set; }
    public float MarchSpeed { get; private set; }

    private struct Step
    {
        public HeroController hero;
        public int life;
        public float start, end, speed;
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
            steps[i]=step;MarchSpeed=Mathf.Min(MarchSpeed,step.speed);
        }
        if(steps.Count<2) { CurrentPhase=Phase.Single;MarchSpeed=steps.Count==0?0f:steps[0].speed;return; }
        if(combat) { CurrentPhase=Phase.Combat;return; }

        float gap=Mathf.Max(1f,spacing), error=0f;
        // Anchor moves only with world pan, not with a calculated team centre.
        // All slot targets are <= their actor's current X: no backward snap.
        float anchor=float.MaxValue;
        for(int i=0;i<steps.Count;i++)anchor=Mathf.Min(anchor,steps[i].start-i*gap);
        for(int i=1;i<steps.Count;i++)error=Mathf.Max(error,Mathf.Abs(steps[i].start-steps[i-1].start-gap));
        float allowed=Mathf.Clamp(tolerance,0f,gap*.25f);
        if(CurrentPhase==Phase.Marching && error>allowed+Mathf.Max(0f,regroupHysteresis))CurrentPhase=Phase.Regrouping;
        if(CurrentPhase==Phase.Regrouping && error<=allowed)CurrentPhase=Phase.Marching;
        for(int i=0;i<steps.Count;i++)
        {
            var step=steps[i];
            if(CurrentPhase==Phase.Marching)step.end=step.start-MarchSpeed*dt;
            else
            {
                step.end=Mathf.MoveTowards(step.start,anchor+i*gap,step.speed*dt);
                // A fast rear actor may not overtake a slower front actor while
                // spacing is being established. Waiting never adds speed later.
                if(i>0)step.end=Mathf.Min(step.start,Mathf.Max(step.end,steps[i-1].end+gap));
            }
            steps[i]=step;
        }
    }
}
