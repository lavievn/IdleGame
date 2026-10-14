using System;
using System.Collections.Generic;
using TuTienCore;

// 0.0.52 forge TEST rules. No failure RNG, no destruction, no refunds.
// Actual item rolls are immutable; only revealed flags and progression change.
public static class EquipmentRefinement
{
    public const string MaterialId = "material:refinement";
    public const string MaterialName = "Nguyên liệu rèn";
    public const int RegularCost = 1;
    public const int SpecialCost = 10;
    public const int StageSteps = 10;
    public const int SchemaVersion = 1;

    public enum Operation { ItemLevelUp, ItemLevelDown, NormalUp, NormalDown, SpecialUp, SpecialDown }

    private static readonly CombatStatId[] SwordAffixPool = {
        CombatStatId.Attack, CombatStatId.AttackSpeed,
        CombatStatId.ElementDamagePercent, CombatStatId.PhysicalDamagePercent
    };

    public static string SwordName(EquipmentRarity rarity)
    {
        switch(rarity)
        {
            case EquipmentRarity.Hoang:return "Thiết Kiếm";
            case EquipmentRarity.Huyen:return "Hàn Ngân Kiếm";
            case EquipmentRarity.Dia:return "Kim Quang Kiếm";
            case EquipmentRarity.Thien:return "Thanh Ngọc Kiếm";
            case EquipmentRarity.Tien:return "Tiên Linh Kiếm";
            default:return "Mộc Kiếm";
        }
    }

    public static string DisplayName(EquipmentInstance item)
    {
        if(item==null)return "";
        if(item.templateId==EquipmentFoundation.DemoSword)
            return SwordName(item.rarity);
        var template=EquipmentFoundation.FindTemplate(item.templateId);
        return template==null?item.templateId:template.name;
    }

    public static EquipmentRarity Next(EquipmentRarity rarity)
    {
        return (EquipmentRarity)Math.Min((int)EquipmentRarity.Tien,(int)rarity+1);
    }

    // New test gear rolls each stat type ONCE. No subsequent rerolls exist.
    public static EquipmentInstance CreateForgeDemoSword()
    {
        var item=EquipmentFoundation.CreateDemoSword();
        var rng=new System.Random(Guid.NewGuid().GetHashCode());
        item.itemElement=ElementType.Kim;
        item.potentialRarity=EquipmentRarity.Tien;
        item.affixSchemaVersion=EquipmentFoundation.AffixSchemaVersion;
        item.forgeSchemaVersion=SchemaVersion;
        item.forgeStageStartRarity=EquipmentRarity.Common;
        item.forgeStageTargetRarity=EquipmentRarity.Dia; // demonstrative initial ceiling
        item.naturalRefineLimit=StageSteps;
        int[] reqRefine={1,2,4,7};
        int[] reqLevel={1,3,5,8};
        var pool=new List<CombatStatId>(SwordAffixPool);
        for(int i=0;i<4;i++)
        {
            int pick=rng.Next(pool.Count);
            CombatStatId stat=pool[pick];
            pool.RemoveAt(pick);
            var line=new ItemStatLine {
                stat=stat,revealed=false,
                requiredItemLevel=reqLevel[i],
                requiredRefineLevel=reqRefine[i],
                element=ElementType.Kim,
                flat=stat==CombatStatId.Attack ? rng.Next(2,7) :
                     stat==CombatStatId.AttackSpeed ? (float)(.01d*rng.Next(2,6)) :
                     (float)(.01d*rng.Next(3,10))
            };
            if(stat==CombatStatId.Attack || stat==CombatStatId.AttackSpeed)
                line.requiredTags.Add("stance:melee");
            item.extraStats.Add(line);
        }
        item.affixSlotCount=item.extraStats.Count;
        return item;
    }

    // Never modify or regenerate the saved stats of an older item.
    public static bool NormalizeMetadata(EquipmentInstance item)
    {
        if(item==null || item.forgeSchemaVersion!=0)return false;
        item.forgeSchemaVersion=SchemaVersion;
        item.forgeStageProgress=0;
        item.forgeStageStartRarity=item.rarity;
        item.forgeStageTargetRarity=Next(item.rarity);
        item.specialForgeCount=0;
        item.powerBaseLevel=item.level;
        item.powerBaseRefine=item.refineLevel;
        item.powerBaseRarity=item.rarity;
        if(item.naturalRefineLimit==0)
            item.naturalRefineLimit=Math.Min(EquipmentFoundation.MaximumRefineLevel,item.refineLevel+StageSteps);
        item.forgeHistory=new List<ForgeStageSnapshot>();
        return true;
    }

    public static float PowerMultiplier(EquipmentInstance item)
    {
        if(item==null || item.forgeSchemaVersion!=SchemaVersion)return 1f;
        float scale=1f+.025f*(item.level-item.powerBaseLevel)+
            .02f*(item.refineLevel-item.powerBaseRefine)+
            .14f*((int)item.rarity-(int)item.powerBaseRarity);
        return Math.Max(.25f,scale);
    }

    public static CombatStatModifier EffectiveModifier(EquipmentInstance item,
        ItemStatLine line,string sourceId)
    {
        var result=line.AsModifier(sourceId);
        float mult=PowerMultiplier(item);
        result.flat*=mult;
        result.percent*=mult;
        return result;
    }

    public static int ProgressPercent(EquipmentInstance item)
    {
        return item==null?0:Math.Max(0,Math.Min(100,
            100*item.forgeStageProgress/StageSteps));
    }

    public static bool TryApply(EntityDataSO hero,string instanceId,
        Operation operation,out string message)
    {
        message="Không thể thao tác rèn.";
        var item=EquipmentRuntime.Find(hero,instanceId);
        if(hero==null || !EquipmentFoundation.IsValid(item))return false;
        NormalizeMetadata(item);
        int cost=0;
        switch(operation)
        {
            case Operation.ItemLevelUp:
                if(item.level>=EquipmentFoundation.MaximumItemLevel)
                {message="Trang bị đã đạt cấp tối đa.";return false;}
                cost=RegularCost;break;
            case Operation.ItemLevelDown:
                if(item.level<=1){message="Cấp trang bị không thể thấp hơn 1.";return false;}
                break;
            case Operation.NormalUp:
                if(item.forgeStageProgress>=StageSteps ||
                   item.refineLevel>=EquipmentFoundation.MaximumRefineLevel)
                {message="Chặng rèn thường đã chạm giới hạn.";return false;}
                cost=RegularCost;break;
            case Operation.NormalDown:
                if(item.forgeStageProgress<=0)
                {message="Không còn lần rèn thường trong chặng này.";return false;}
                break;
            case Operation.SpecialUp:
                if(item.forgeStageTargetRarity<=item.forgeStageStartRarity ||
                   item.forgeStageStartRarity==EquipmentRarity.Tien)
                {message="Không còn phẩm chất để đột phá.";return false;}
                cost=SpecialCost;break;
            case Operation.SpecialDown:
                if(item.forgeHistory==null || item.forgeHistory.Count==0)
                {message="Không có chặng rèn đặc biệt để hoàn tác.";return false;}
                if(item.forgeStageProgress!=0)
                {message="Hãy giảm rèn thường về 0 trước khi hạ rèn đặc biệt.";return false;}
                break;
            default:return false;
        }
        if(cost>0 && EquipmentInventory.Count(hero,MaterialId)<cost)
        {message="Thiếu "+MaterialName+" (cần "+cost+").";return false;}
        // Deduct only after every validation succeeds.
        if(cost>0 && !Consume(hero,cost))
        {message="Không thể trừ nguyên liệu.";return false;}

        switch(operation)
        {
            case Operation.ItemLevelUp:item.level++;break;
            case Operation.ItemLevelDown:item.level--;break;
            case Operation.NormalUp:
                item.refineLevel++;
                item.forgeStageProgress++;
                if(item.forgeStageProgress==StageSteps)
                    item.rarity=item.forgeStageTargetRarity;
                break;
            case Operation.NormalDown:
                item.refineLevel--;
                item.forgeStageProgress--;
                item.rarity=item.forgeStageStartRarity;
                break;
            case Operation.SpecialUp:
                if(item.forgeHistory==null)
                    item.forgeHistory=new List<ForgeStageSnapshot>();
                item.forgeHistory.Add(new ForgeStageSnapshot {
                    previousRarity=item.rarity,
                    stageStart=item.forgeStageStartRarity,
                    stageTarget=item.forgeStageTargetRarity,
                    stageProgress=item.forgeStageProgress,
                    naturalLimit=item.naturalRefineLimit
                });
                // Complete this stage instantly even if progress is only 60%.
                item.rarity=item.forgeStageTargetRarity;
                item.forgeStageStartRarity=item.rarity;
                item.forgeStageTargetRarity=DrawNextTarget(item.rarity);
                item.forgeStageProgress=0;
                item.naturalRefineLimit=Math.Min(EquipmentFoundation.MaximumRefineLevel,
                    item.refineLevel+StageSteps);
                item.specialForgeCount++;
                break;
            case Operation.SpecialDown:
                int index=item.forgeHistory.Count-1;
                var snapshot=item.forgeHistory[index];
                item.forgeHistory.RemoveAt(index);
                item.rarity=snapshot.previousRarity;
                item.forgeStageStartRarity=snapshot.stageStart;
                item.forgeStageTargetRarity=snapshot.stageTarget;
                item.forgeStageProgress=snapshot.stageProgress;
                item.naturalRefineLimit=snapshot.naturalLimit;
                item.specialForgeCount=Math.Max(0,item.specialForgeCount-1);
                break;
        }

        if(operation==Operation.ItemLevelUp || operation==Operation.NormalUp ||
           operation==Operation.SpecialUp)
        {
            item.hasBeenRefined=true;
            item.highestRefineLevel=Math.Max(item.highestRefineLevel,item.refineLevel);
            EquipmentFoundation.RevealEligibleAffixes(item);
        }
        EquipmentRuntime.RebuildModifiers(hero);
        hero.isDirty=true;
        message="Đã cập nhật trang bị.";
        return true;
    }

    private static bool Consume(EntityDataSO hero,int cost)
    {
        if(hero==null || hero.inventory==null || cost<=0)return false;
        var stack=hero.inventory.Find(x=>x!=null && x.itemId==MaterialId);
        if(stack==null || stack.quantity<cost)return false;
        stack.quantity-=cost;
        if(stack.quantity==0)hero.inventory.Remove(stack);
        return true;
    }

    // Per-special-stage prototype ceiling, never a reroll of affix identity.
    private static EquipmentRarity DrawNextTarget(EquipmentRarity current)
    {
        if(current>=EquipmentRarity.Tien)return EquipmentRarity.Tien;
        int remaining=(int)EquipmentRarity.Tien-(int)current;
        if(remaining==1)return EquipmentRarity.Tien;
        var rng=new System.Random(Guid.NewGuid().GetHashCode());
        return (EquipmentRarity)Math.Min((int)EquipmentRarity.Tien,
            (int)current+(rng.Next(100)<35 ? 2 : 1));
    }
}
