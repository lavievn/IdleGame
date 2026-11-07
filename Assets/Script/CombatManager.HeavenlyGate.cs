using System;
using UnityEngine;
using UnityEngine.UI;
using TuTienCore;
public partial class CombatManager
{
    ActiveMonsterInfo gateTarget;HeavenlyGateConfiguration gateConfig;double gateStrikeTime;int gateHeroLife;EntityDataSO gateHeroData;
    public bool HeavenlyGateActive=>gateTarget!=null;
    public int HeavenlyGateHP=>gateTarget==null?0:gateTarget.currentHP;
    public int HeavenlyGateMaxHP=>gateTarget==null?0:gateTarget.maxHP;public string HeavenlyGateName=>gateConfig?.title;
    public long HeroReleaseCount {get;private set;}public long AcceptedHeroHitCount {get;private set;}public long LastImpactRelease {get;private set;}
    long impactRelease;
    Func<bool> gateFighting;Action<int,int,double> gateObserve;Action gateInvalidated;
    bool CanRunGateCombat=>gateFighting!=null&&gateFighting();
    internal void BeginHeavenlyGate(HeavenlyGateConfiguration config,Func<bool> fighting,Action<int,int,double> observe,Action invalidated)
    {
        if(config==null||!config.IsValid||!primaryHeroState.CanFight||heroStates.Count!=1)throw new InvalidOperationException("One living canonical Hero and explicit gate required");
        ClearBattle(false);gateFighting=fighting;gateObserve=observe;gateInvalidated=invalidated;gateConfig=config.Copy();gateHeroLife=primaryHeroState.LifeVersion;gateHeroData=primaryHeroState.Data;gateStrikeTime=0;
        var space=EnvironmentManager.Instance;
        var go=new GameObject("HeavenlyGatePlaceholder",typeof(RectTransform),typeof(Image));go.SetActive(false);go.transform.SetParent(space.BattleArea,false);
        var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=new Vector2(.5f,0);rect.sizeDelta=new Vector2(64,104);
        var image=go.GetComponent<Image>();image.color=new Color(.29f,.45f,.63f,1);image.raycastTarget=false;var controller=go.AddComponent<MonsterController>();controller.IsHeavenlyGate=true;controller.HeavenlyGateRange=(float)config.targetRange;
        go.SetActive(true);controller.attackMode=AttackMode.Melee;
        var inset=new GameObject("Seal inset",typeof(RectTransform),typeof(Image));inset.transform.SetParent(rect,false);var plate=(RectTransform)inset.transform;plate.anchorMin=plate.anchorMax=new Vector2(.5f,.5f);plate.sizeDelta=new Vector2(42,76);inset.GetComponent<Image>().color=new Color(.44f,.60f,.73f,1);inset.GetComponent<Image>().raycastTarget=false;
        space.SetPosition(rect,space.Position(primaryHeroState.Controller.heroRect)+new Vector2(-Mathf.Min(180,(float)config.targetRange*.8f),0));
        var data=ScriptableObject.CreateInstance<EntityDataSO>();data.entityName=config.title;data.baseHealth=config.hp;data.baseDamage=config.damagePerStrike;data.currentLevel=1;data.combatStatModifiers=new System.Collections.Generic.List<CombatStatModifier>{new CombatStatModifier{sourceId=config.id,stat=CombatStatId.Armor,flat=(float)config.armor},new CombatStatModifier{sourceId=config.id,stat=CombatStatId.MagicResistance,flat=(float)config.magicResistance}};
        gateTarget=new ActiveMonsterInfo{go=go,controller=controller,data=data,maxHP=config.hp,currentHP=config.hp,lifeId=++monsterLifeSequence};activeMonsters.Add(gateTarget);isBattling=true;
        primaryHeroState.AttackTimer=WeaponBasicAttack.Interval(primaryHeroState.SelectedMode,primaryHeroState.Data.baseAttackSpeed);
    }
    void TickHeavenlyGate(float dt)
    {
        if(gateTarget==null)return;if(!CanRunGateCombat)return;
        if(primaryHeroState.Data!=gateHeroData||primaryHeroState.LifeVersion!=gateHeroLife){gateInvalidated?.Invoke();return;}
        gateStrikeTime+=dt;
        // One accepted combat frame resolves both sides, so same-step double KO never wins.
        while(gateStrikeTime>=gateConfig.strikeInterval&&primaryHeroState.CurrentHP>0){gateStrikeTime-=gateConfig.strikeInterval;
            var trace=SynergyMath.EvaluateDamage(gateTarget.data,primaryHeroState.Data,AttackMode.Melee,1f,1f);
            ApplyDamageToHeroState(primaryHeroState,trace.elementDamage,gateConfig.title,trace,null,gateTarget.data,AttackMode.Melee);
        }
        gateObserve?.Invoke(primaryHeroState.CurrentHP,gateTarget.currentHP,dt);
    }
    void ClearGateIdentity(){gateTarget=null;gateConfig=null;gateHeroData=null;gateStrikeTime=0;gateFighting=null;gateObserve=null;gateInvalidated=null;}
}
