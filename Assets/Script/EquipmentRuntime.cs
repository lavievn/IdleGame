using System;
using System.Collections.Generic;

// Equipment instances live in a single canonical list per saved run.
// isEquipped/equippedSlot is the slot assignment. Equipped items do not live
// in a second bag list, so moving an item cannot duplicate it.
public static class EquipmentRuntime
{
    public static void InitializeNewRun(EntityDataSO hero)
    {
        if(hero == null) return;
        hero.equipmentVersion = EquipmentFoundation.Version;
        hero.equipmentItems = new List<EquipmentInstance>();
        hero.professionProgress = new List<ProfessionProgress>();
        hero.learnedRecipes = new List<LearnedProfessionRecipe>();
        hero.learnedSupportSkills = new List<LearnedSupportSkill>();
        hero.gold = 0L;
        hero.demoSwordGranted = false;
        RemoveDerived(hero);
        hero.isDirty = true;
    }

    public static bool AddInstance(EntityDataSO hero, EquipmentInstance item)
    {
        if(hero == null || !EquipmentFoundation.IsValid(item) ||
            hero.equipmentItems == null || item.isEquipped ||
            Find(hero,item.instanceId) != null) return false;
        // A copy of the object is owned by the hero. Never alias incoming
        // instances with another hero or an Inspector template.
        hero.equipmentItems.Add(Copy(item));
        hero.isDirty = true;
        return true;
    }

    public static EquipmentInstance Find(EntityDataSO hero,string instanceId)
    {
        if(hero == null || hero.equipmentItems == null ||
            string.IsNullOrEmpty(instanceId)) return null;
        return hero.equipmentItems.Find(x=>x != null &&
            string.Equals(x.instanceId,instanceId,StringComparison.Ordinal));
    }

    public static EquipmentInstance Equipped(EntityDataSO hero,EquipmentSlot slot)
    {
        if(hero == null || hero.equipmentItems == null) return null;
        return hero.equipmentItems.Find(x=>x != null && x.isEquipped &&
            x.equippedSlot == slot && EquipmentFoundation.IsValid(x) &&
            EquipmentFoundation.IsSlotAllowed(x,slot));
    }

    public static bool Equip(EntityDataSO hero,string instanceId,EquipmentSlot slot)
    {
        var item=Find(hero,instanceId);
        if(!EquipmentFoundation.IsValid(item) ||
            !EquipmentFoundation.IsSlotAllowed(item,slot) ||
            (item.isEquipped && item.equippedSlot==slot)) return false;
        // Legacy trinket stays in the Artifact slot. It must be removed via
        // its existing button before using a new Artifact instance.
        if(slot==EquipmentSlot.Artifact && !string.IsNullOrEmpty(hero.equippedItemId))
            return false;
        var occupied=Equipped(hero,slot);
        if(occupied != null && occupied != item) occupied.isEquipped=false;
        item.isEquipped=true;
        item.equippedSlot=slot;
        RebuildModifiers(hero);
        hero.isDirty=true;
        return true;
    }

    public static bool Unequip(EntityDataSO hero,EquipmentSlot slot)
    {
        var equipped=Equipped(hero,slot);
        if(equipped==null) return false;
        equipped.isEquipped=false;
        RebuildModifiers(hero);
        hero.isDirty=true;
        return true;
    }

    public static bool GrantDemoSword(EntityDataSO hero)
    {
        if(hero==null || hero.demoSwordGranted) return false;
        if(hero.equipmentItems==null) hero.equipmentItems=new List<EquipmentInstance>();
        var sword=EquipmentFoundation.CreateDemoSword();
        if(!AddInstance(hero,sword)) return false;
        hero.demoSwordGranted=true;
        hero.isDirty=true;
        return true;
    }

    // Do not roll rarity or affixes while normalizing: loading a checkpoint
    // must restore the very same item, even when its lines have empty slots.
    public static void Normalize(EntityDataSO hero)
    {
        if(hero == null) return;
        bool changed=false;
        if(hero.equipmentItems==null) {hero.equipmentItems=new List<EquipmentInstance>();changed=true;}
        if(hero.professionProgress==null) {hero.professionProgress=new List<ProfessionProgress>();changed=true;}
        if(hero.learnedRecipes==null) {hero.learnedRecipes=new List<LearnedProfessionRecipe>();changed=true;}
        if(hero.learnedSupportSkills==null) {hero.learnedSupportSkills=new List<LearnedSupportSkill>();changed=true;}
        if(hero.gold<0) {hero.gold=0;changed=true;}
        if(hero.equipmentVersion != EquipmentFoundation.Version)
        {hero.equipmentVersion=EquipmentFoundation.Version;changed=true;}

        var seenIds=new HashSet<string>(StringComparer.Ordinal);
        var occupied=new HashSet<EquipmentSlot>();
        if(!string.IsNullOrEmpty(hero.equippedItemId))
            occupied.Add(EquipmentSlot.Artifact);
        foreach(var item in hero.equipmentItems)
        {
            if(item==null) {changed=true;continue;}
            bool legitimate=EquipmentFoundation.IsValid(item);
            bool unique= !string.IsNullOrEmpty(item.instanceId) &&
                seenIds.Add(item.instanceId);
            // Preserve unknown/future templates and their stats as opaque
            // data, but never let them provide invalid or duplicate bonuses.
            if(item.isEquipped && (!legitimate || !unique ||
                !EquipmentFoundation.IsSlotAllowed(item,item.equippedSlot) ||
                !occupied.Add(item.equippedSlot)))
            {item.isEquipped=false;changed=true;}
        }
        if(RebuildModifiers(hero)) changed=true;
        if(changed) hero.isDirty=true;
    }

    // Called AFTER legacy modifier reconciliation. Only this exact prefix is
    // owned by the v2 equipment system; character traits remain untouched.
    public static bool RebuildModifiers(EntityDataSO hero)
    {
        if(hero==null) return false;
        if(hero.combatStatModifiers==null)
            hero.combatStatModifiers=new List<CombatStatModifier>();
        var expected=new List<CombatStatModifier>();
        var occupied=new HashSet<EquipmentSlot>();
        if(!string.IsNullOrEmpty(hero.equippedItemId))
            occupied.Add(EquipmentSlot.Artifact);
        var ids=new HashSet<string>(StringComparer.Ordinal);
        if(hero.equipmentItems!=null) foreach(var item in hero.equipmentItems)
        {
            if(item==null || !item.isEquipped ||
                !EquipmentFoundation.IsValid(item) ||
                !ids.Add(item.instanceId) ||
                !EquipmentFoundation.IsSlotAllowed(item,item.equippedSlot) ||
                !occupied.Add(item.equippedSlot)) continue;
            for(int i=0;i<item.mainStats.Count;i++)
                expected.Add(item.mainStats[i].AsModifier(
                    EquipmentFoundation.SourcePrefix+item.instanceId+":m:"+i));
            for(int i=0;i<item.extraStats.Count;i++)
                expected.Add(item.extraStats[i].AsModifier(
                    EquipmentFoundation.SourcePrefix+item.instanceId+":a:"+i));
        }
        var actual=new List<CombatStatModifier>();
        foreach(var mod in hero.combatStatModifiers)
            if(mod!=null && mod.sourceId != null &&
                mod.sourceId.StartsWith(EquipmentFoundation.SourcePrefix,
                    StringComparison.Ordinal)) actual.Add(mod);
        bool equal=expected.Count==actual.Count;
        if(equal)for(int i=0;i<expected.Count;i++)
            if(!Same(expected[i],actual[i])) {equal=false;break;}
        if(equal)return false;
        hero.combatStatModifiers.RemoveAll(mod=>mod!=null && mod.sourceId!=null &&
            mod.sourceId.StartsWith(EquipmentFoundation.SourcePrefix,
                StringComparison.Ordinal));
        hero.combatStatModifiers.AddRange(expected);
        return true;
    }

    private static void RemoveDerived(EntityDataSO hero)
    {
        if(hero.combatStatModifiers!=null)
            hero.combatStatModifiers.RemoveAll(mod=>mod!=null && mod.sourceId!=null &&
                mod.sourceId.StartsWith(EquipmentFoundation.SourcePrefix,
                    StringComparison.Ordinal));
    }
    private static bool Same(CombatStatModifier a,CombatStatModifier b)
    {
        return a.sourceId==b.sourceId && a.stat==b.stat &&
            a.flat==b.flat && a.percent==b.percent &&
            a.element==b.element && a.anyElement==b.anyElement &&
            TagsEqual(a.requiredTags,b.requiredTags) &&
            TagsEqual(a.blockedTags,b.blockedTags);
    }
    private static bool TagsEqual(List<string> a,List<string> b)
    {
        int na=a==null?0:a.Count,nb=b==null?0:b.Count;
        if(na!=nb) return false;
        for(int i=0;i<na;i++) if(a[i]!=b[i])return false;
        return true;
    }
    private static EquipmentInstance Copy(EquipmentInstance item)
    {
        var output=new EquipmentInstance {
            instanceId=item.instanceId, templateId=item.templateId,
            level=item.level,rarity=item.rarity,refineLevel=item.refineLevel,
            isEquipped=false,equippedSlot=item.equippedSlot
        };
        foreach(var line in item.mainStats) output.mainStats.Add(CopyLine(line));
        foreach(var line in item.extraStats) output.extraStats.Add(CopyLine(line));
        return output;
    }
    private static ItemStatLine CopyLine(ItemStatLine line)
    {
        return new ItemStatLine {
            stat=line.stat,flat=line.flat,percent=line.percent,
            element=line.element,anyElement=line.anyElement,
            requiredTags=line.requiredTags==null?new List<string>():
                new List<string>(line.requiredTags),
            blockedTags=line.blockedTags==null?new List<string>():
                new List<string>(line.blockedTags)
        };
    }
}
