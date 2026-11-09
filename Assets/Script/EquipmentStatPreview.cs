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
    public bool isDerivedGameplayTotal;
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
    public string currentItemName, previewItemName, currentGrantedSkillId, previewGrantedSkillId;
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

        var currentWeapon=WeaponBasicAttack.Equipped(hero);
        var currentItem=EquipmentRuntime.Equipped(hero,slot);
        EquipmentInstance nextWeapon=null;
        AttackMode currentMode=WeaponBasicAttack.Mode(WeaponBasicAttack.Family(currentWeapon));
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
            if(destination==EquipmentSlot.Weapon)nextWeapon=item;
            EquipmentRuntime.AddEffectiveModifiers(item,future);
        }

        // Current comes from the real Hero modifier snapshot, including
        // conditional effects. Future is a simulated canonical equipment list.
        var current=mods ?? new List<CombatStatModifier>();
        var after=currentOnly?current:future;
        var result=new EquipmentStatComparison {
            candidateId=candidate!=null?candidate.instanceId:legacyCandidate,
            slot=slot,currentMode=currentMode,
            mode=currentOnly?currentMode:WeaponBasicAttack.Mode(WeaponBasicAttack.Family(nextWeapon)),isUnequip=removing,
            currentItemName=ItemName(currentItem,slot==EquipmentSlot.Artifact?hero.equippedItemId:null),
            previewItemName=currentOnly?ItemName(currentItem,null):ItemName(candidate,legacyCandidate),
            currentGrantedSkillId=EquipmentFoundation.GrantedSkillId(currentWeapon),
            previewGrantedSkillId=EquipmentFoundation.GrantedSkillId(currentOnly?currentWeapon:nextWeapon)
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

    private static string ItemName(EquipmentInstance item,string legacyId) =>
        item!=null?EquipmentRefinement.DisplayName(item):EquipmentCatalog.Find(legacyId)?.name;

    private static bool IsEquipmentModifier(CombatStatModifier modifier)
    {
        return modifier.sourceId==EquipmentInventory.ModifierSource ||
            (modifier.sourceId!=null &&
             modifier.sourceId.StartsWith(EquipmentFoundation.SourcePrefix,
                 StringComparison.Ordinal));
    }

    private static float Total(EntityDataSO hero,IEnumerable<CombatStatModifier> mods,
        CombatStatId stat,ElementType element,AttackMode mode)
    {
        if(stat==CombatStatId.Attack || stat==CombatStatId.MagicalAttack)
            return WeaponBasicAttack.Value(hero,stat,mode,mods);
        if(stat==CombatStatId.MaxHealth)
            return Math.Max(1,CombatStats.GetIntWithModifiers(hero,mods,stat,hero.HeroHealthBeforeModifiers));
        float baseline=stat==CombatStatId.AttackSpeed?hero.baseAttackSpeed:CombatSkillProgress.ResourceBaseline(stat);
        string[] context=stat==CombatStatId.Armor?CombatDamage.IncomingTags(DamageSchool.Physical,element):
            stat==CombatStatId.MagicResistance?CombatDamage.IncomingTags(DamageSchool.Magical,element):
            new[]{CombatStats.StanceTag(mode)};
        float value=CombatStats.GetWithModifiers(hero,mods,stat,baseline,element,context);
        return CombatSkillProgress.IsResourceStat(stat)?Math.Max(0,value):value;
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
                stat==CombatStatId.MaxHealth || stat==CombatStatId.AttackSpeed || CombatSkillProgress.IsResourceStat(stat)
        });
    }
}
