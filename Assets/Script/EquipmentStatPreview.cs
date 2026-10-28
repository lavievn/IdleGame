using System;
using System.Collections.Generic;
using UnityEngine;
using TuTienCore;

// Pure comparison data for the character-sheet UI. All values are derived
// using the same combat-stat evaluator as gameplay, never added to Hero data.
public sealed class EquipmentStatLine
{
    public CombatStatId stat;
    public ElementType element;
    public float withoutEquipment;
    public float current;
    public float preview;
    public bool isDerivedGameplayTotal; // HP, ATK and attack speed only.
    public float equippedBonus { get { return current-withoutEquipment; } }
    public float difference { get { return preview-current; } }
}

public sealed class EquipmentStatComparison
{
    public string candidateId;
    public EquipmentSlot slot;
    public AttackMode currentMode;
    public AttackMode mode; // resolved future weapon profile
    public bool isUnequip;
    public readonly List<EquipmentStatLine> lines=new List<EquipmentStatLine>();

    public EquipmentStatLine Find(CombatStatId stat,ElementType element=ElementType.Vo)
    {
        return lines.Find(x=>x.stat==stat && x.element==element);
    }
}

// The mode argument is kept for call compatibility; actual current/future weapon
// assignments determine the modes, never savedAttackMode or a manual stance.
// No random affix rolls, no mutation, no application to combat. This service
// can be called on hover, before equip, or while editing the UI in Play Mode.
public static class EquipmentStatPreview
{
    public static bool TryEquip(EntityDataSO hero,string instanceId,
        EquipmentSlot slot,AttackMode mode,out EquipmentStatComparison result)
    {
        result=null;
        var candidate=EquipmentRuntime.Find(hero,instanceId);
        if(hero==null || !EquipmentFoundation.IsValid(candidate) ||
            !EquipmentFoundation.IsSlotAllowed(candidate,slot))
            return false;
        if(slot==EquipmentSlot.Artifact &&
            !string.IsNullOrEmpty(hero.equippedItemId))
            return false; // same rule as EquipmentRuntime.Equip
        result=Build(hero,candidate,null,slot,mode,false);
        return true;
    }

    public static bool TryEquipLegacy(EntityDataSO hero,string itemId,
        AttackMode mode,out EquipmentStatComparison result)
    {
        result=null;
        var legacy=EquipmentCatalog.Find(itemId);
        if(hero==null || legacy==null ||
            EquipmentRuntime.Equipped(hero,EquipmentSlot.Artifact)!=null)
            return false;
        // Already worn -> zero-delta comparison. Otherwise must be in bag.
        if(hero.equippedItemId!=itemId &&
            EquipmentInventory.Count(hero,itemId)<=0)return false;
        result=Build(hero,null,itemId,EquipmentSlot.Artifact,mode,false);
        return true;
    }

    public static bool TryUnequip(EntityDataSO hero,EquipmentSlot slot,
        AttackMode mode,out EquipmentStatComparison result)
    {
        result=null;
        if(hero==null)return false;
        if(EquipmentRuntime.Equipped(hero,slot)==null &&
            (slot!=EquipmentSlot.Artifact ||
             EquipmentCatalog.Find(hero.equippedItemId)==null))
            return false;
        result=Build(hero,null,null,slot,mode,true);
        return true;
    }

    // Exposed for the character sheet to show persistent/base vs equipment
    // bonuses even when no item is currently being hovered.
    public static EquipmentStatComparison Current(EntityDataSO hero,AttackMode mode)
    {
        return hero==null?null:Build(hero,null,null,EquipmentSlot.Weapon,mode,false,true);
    }

    private static EquipmentStatComparison Build(EntityDataSO hero,
        EquipmentInstance candidate,string legacyCandidate,EquipmentSlot slot,
        AttackMode mode,bool removing,bool currentOnly=false)
    {
        var basis=new List<CombatStatModifier>();
        var mods=hero.combatStatModifiers;
        if(mods!=null)foreach(var mod in mods)
        {
            if(mod==null || IsEquipmentModifier(mod))continue;
            basis.Add(mod);
        }
        var future=new List<CombatStatModifier>(basis);
        bool keepLegacy=slot!=EquipmentSlot.Artifact || currentOnly;
        string nextLegacy=currentOnly || keepLegacy
            ? hero.equippedItemId : legacyCandidate;
        var legacy=EquipmentCatalog.Find(nextLegacy);
        if(legacy!=null)
            future.Add(new CombatStatModifier {
                sourceId=EquipmentInventory.ModifierSource,
                stat=legacy.stat,flat=legacy.flat
            });

        AttackMode currentMode=WeaponBasicAttack.Resolve(hero);
        AttackMode nextMode=AttackMode.Melee; // no weapon in the simulated assignment
        var occupied=new HashSet<EquipmentSlot>();
        if(legacy!=null)occupied.Add(EquipmentSlot.Artifact);
        var seenIds=new HashSet<string>(StringComparer.Ordinal);
        if(hero.equipmentItems!=null)foreach(var item in hero.equipmentItems)
        {
            if(item==null || !EquipmentFoundation.IsValid(item))continue;
            bool isCandidate=candidate!=null && item.instanceId==candidate.instanceId;
            bool wearing=isCandidate || (item.isEquipped &&
                (currentOnly || item.equippedSlot!=slot));
            if(removing && item.isEquipped && item.equippedSlot==slot)
                wearing=false;
            if(!wearing)continue;
            EquipmentSlot destination=isCandidate && !currentOnly?slot:item.equippedSlot;
            if(!EquipmentFoundation.IsSlotAllowed(item,destination) ||
                !seenIds.Add(item.instanceId) || !occupied.Add(destination))continue;
            if(destination==EquipmentSlot.Weapon)nextMode=WeaponBasicAttack.Mode(WeaponBasicAttack.Family(item));
            for(int i=0;i<item.mainStats.Count;i++)
                future.Add(EquipmentRefinement.EffectiveModifier(item,item.mainStats[i],
                    EquipmentFoundation.SourcePrefix+item.instanceId+":m:"+i));
            for(int i=0;i<item.extraStats.Count;i++)
                if(EquipmentFoundation.AffixState(item,item.extraStats[i]) ==
                    EquipmentAffixState.Active)
                    future.Add(EquipmentRefinement.EffectiveModifier(item,item.extraStats[i],
                        EquipmentFoundation.SourcePrefix+item.instanceId+":a:"+i));
        }

        // Current comes from the real Hero modifier snapshot, including
        // conditional effects. Future is a simulated canonical equipment list.
        var current=mods ?? new List<CombatStatModifier>();
        var after=currentOnly?current:future;
        var result=new EquipmentStatComparison {
            candidateId=candidate!=null?candidate.instanceId:legacyCandidate,
            slot=slot,currentMode=currentMode,mode=currentOnly?currentMode:nextMode,isUnequip=removing
        };
        foreach(CombatStatId stat in Enum.GetValues(typeof(CombatStatId)))
        {
            if(stat==CombatStatId.ElementResistance ||
                stat==CombatStatId.ElementDamagePercent)
            {
                foreach(ElementType element in Enum.GetValues(typeof(ElementType)))
                    if(element!=ElementType.Vo)
                        AddLine(result,hero,stat,element,basis,current,after);
            }
            else AddLine(result,hero,stat,ElementType.Vo,basis,current,after);
        }
        return result;
    }

    private static bool IsEquipmentModifier(CombatStatModifier modifier)
    {
        return modifier.sourceId==EquipmentInventory.ModifierSource ||
            (modifier.sourceId!=null &&
             modifier.sourceId.StartsWith(EquipmentFoundation.SourcePrefix,
                 StringComparison.Ordinal));
    }

    private static float Raw(EntityDataSO hero,CombatStatId stat,AttackMode mode)
    {
        if(stat==CombatStatId.AttackSpeed)return hero.baseAttackSpeed;
        if(stat!=CombatStatId.MaxHealth)return 0f;
        long byMode=Math.Max(1,Mathf.RoundToInt(hero.baseHealth*CombatBalance.HeroHealthMultiplier(mode)));
        long hp=byMode+hero.addedHealth+(mode==AttackMode.Melee?Math.Max(0,hero.meleeGrowthBonus):0);
        return (float)Math.Max(1,Math.Min(int.MaxValue,hp));
    }
    private static float Total(EntityDataSO hero,IEnumerable<CombatStatModifier> mods,
        CombatStatId stat,ElementType element,AttackMode mode)
    {
        if(stat==CombatStatId.Attack || stat==CombatStatId.MagicalAttack)
            return WeaponBasicAttack.Value(hero,stat,mode,mods);
        float value=CombatStats.GetWithModifiers(hero,mods,stat,Raw(hero,stat,mode),element,
            new[]{CombatStats.StanceTag(mode)});
        if(stat==CombatStatId.MaxHealth)value=Math.Max(1,RoundStat(value));
        return value;
    }
    private static void AddLine(EquipmentStatComparison output,
        EntityDataSO hero,CombatStatId stat,ElementType element,
        List<CombatStatModifier> basis,IEnumerable<CombatStatModifier> before,
        IEnumerable<CombatStatModifier> after)
    {
        output.lines.Add(new EquipmentStatLine {
            stat=stat,element=element,
            withoutEquipment=Total(hero,basis,stat,element,output.currentMode),
            current=Total(hero,before,stat,element,output.currentMode),
            preview=Total(hero,after,stat,element,output.mode),
            isDerivedGameplayTotal=stat==CombatStatId.Attack || stat==CombatStatId.MagicalAttack ||
                stat==CombatStatId.MaxHealth || stat==CombatStatId.AttackSpeed
        });
    }
    private static float RoundStat(float x)
    {
        return (float)Math.Max(int.MinValue,Math.Min(int.MaxValue,
            Math.Round(x,MidpointRounding.AwayFromZero)));
    }
}
