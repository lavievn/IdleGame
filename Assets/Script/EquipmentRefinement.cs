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
    // Prototype balancing table: 3 / 6 / 12 / 24 / 48 rolls to reach
    // Hoang / Huyen / Dia / Thien / Tien. TUNE before release.
    private static readonly int[] AttemptsPerRarity = { 3, 6, 12, 24, 48 };
    public const int SchemaVersion = 2;
    public static int RequiredAttempts(EquipmentRarity from)
    {
        int rank=(int)from;
        return rank<0 || rank>=AttemptsPerRarity.Length ? 0 :
            AttemptsPerRarity[rank];
    }
    public static int LimitFor(EquipmentRarity from,EquipmentRarity ceiling,int currentRefine)
    {
        int needed=0;
        for(int tier=(int)from;tier<(int)ceiling;tier++)
            needed+=RequiredAttempts((EquipmentRarity)tier);
        return Math.Min(EquipmentFoundation.MaximumRefineLevel,currentRefine+needed);
    }

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
        item.forgeStageTargetRarity=EquipmentRarity.Dia; // initial natural cap
        item.naturalRefineLimit=LimitFor(item.rarity,item.forgeStageTargetRarity,0);
        // Prototype: one fresh affix only on each rarity promotion.
        int[] reqRefine={0,0,0,0};
        int[] reqLevel={1,1,1,1};
        EquipmentRarity[] reqRarity={
            EquipmentRarity.Hoang,EquipmentRarity.Huyen,
            EquipmentRarity.Dia,EquipmentRarity.Thien
        };
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
                requiredRarity=reqRarity[i],
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
        if(item==null || item.forgeSchemaVersion==SchemaVersion)return false;
        // Old forge schema 1 used ten attempts to jump to the ceiling.
        // Preserve rarity, refinement count and ALL saved affixes; reset only
        // the test-stage counter to the new per-rarity rules.
        bool oldForge=item.forgeSchemaVersion==1;
        item.forgeSchemaVersion=SchemaVersion;
        item.forgeStageProgress=0;
        item.forgeStageBasePercent=0;
        item.forgeStageStartRarity=item.rarity;
        item.forgeStageTargetRarity=oldForge &&
            item.forgeStageTargetRarity>item.rarity
            ? item.forgeStageTargetRarity : Next(item.rarity);
        if(!oldForge)
        {
            item.specialForgeCount=0;
            item.powerBaseLevel=item.level;
            item.powerBaseRefine=item.refineLevel;
            item.powerBaseRarity=item.rarity;
            item.forgeHistory=new List<ForgeStageSnapshot>();
        }
        if(item.forgeHistory==null)item.forgeHistory=new List<ForgeStageSnapshot>();
        item.naturalRefineLimit=LimitFor(item.rarity,
            item.forgeStageTargetRarity,item.refineLevel);
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
        // Full bar means "at the natural rarity ceiling", NOT
        // "one upgrade away from the next rarity".
        if(item==null)return 0;
        int current=(int)item.rarity+1;
        int maximum=Math.Max(current,(int)item.forgeStageTargetRarity+1);
        return Math.Max(0,Math.Min(100,100*current/maximum));
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
                if(item.rarity>=item.forgeStageTargetRarity ||
                   item.refineLevel>=item.naturalRefineLimit ||
                   item.refineLevel>=EquipmentFoundation.MaximumRefineLevel)
                {message="Chặng rèn thường đã chạm giới hạn.";return false;}
                cost=RegularCost;break;
            case Operation.NormalDown:
                if(item.refineLevel<=0 ||
                   (item.forgeStageProgress<=0 &&
                    item.rarity<=item.forgeStageStartRarity))
                {message="Không còn lượt rèn để giảm ở chặng này.";return false;}
                break;
            case Operation.SpecialUp:
                if(item.rarity>=EquipmentRarity.Tien)
                {message="Không còn phẩm chất để đột phá.";return false;}
                cost=SpecialCost;break;
            case Operation.SpecialDown:
                if(item.forgeHistory==null || item.forgeHistory.Count==0)
                {message="Không có chặng rèn đặc biệt để hoàn tác.";return false;}
                if(item.forgeStageProgress!=0 ||
                   item.rarity!=item.forgeStageStartRarity)
                {message="Hãy trở lại đầu chặng hiện tại trước khi giảm rèn đặc biệt.";return false;}
                break;
            default:return false;
        }
        if(cost>0 && EquipmentInventory.Count(hero,MaterialId)<cost)
        {message="Thiếu "+MaterialName+" (cần "+cost+").";return false;}
        EquipmentRarity rarityBefore=item.rarity;
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
                if(item.forgeStageProgress>=RequiredAttempts(item.rarity))
                {
                    item.rarity=Next(item.rarity); // advance ONE rarity only
                    item.forgeStageProgress=0;
                }
                break;
            case Operation.NormalDown:
                item.refineLevel--;
                if(item.forgeStageProgress>0)item.forgeStageProgress--;
                else
                {
                    // Reverse only the last promotion. The revealed flag stays.
                    item.rarity=(EquipmentRarity)((int)item.rarity-1);
                    item.forgeStageProgress=RequiredAttempts(item.rarity)-1;
                }
                break;
            case Operation.SpecialUp:
                if(item.forgeHistory==null)
                    item.forgeHistory=new List<ForgeStageSnapshot>();
                item.forgeHistory.Add(new ForgeStageSnapshot {
                    previousRarity=item.rarity,
                    stageStart=item.forgeStageStartRarity,
                    stageTarget=item.forgeStageTargetRarity,
                    stageProgress=item.forgeStageProgress,
                    stageBasePercent=item.forgeStageBasePercent,
                    naturalLimit=item.naturalRefineLimit
                });
                // Special ALWAYS gives exactly one rarity, even if used early.
                // That can waste resources; only the player decides when to use.
                item.rarity=Next(item.rarity);
                item.forgeStageStartRarity=item.rarity;
                // Expand the former natural ceiling by two tiers so a special
                // breakthrough at Dia -> Thien still opens a higher ceiling.
                item.forgeStageTargetRarity=(EquipmentRarity)Math.Min(
                    (int)EquipmentRarity.Tien,
                    Math.Max((int)item.rarity,(int)item.forgeStageTargetRarity+2));
                item.forgeStageProgress=0;
                item.forgeStageBasePercent=0;
                item.naturalRefineLimit=LimitFor(item.rarity,
                    item.forgeStageTargetRarity,item.refineLevel);
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
                item.forgeStageBasePercent=snapshot.stageBasePercent;
                item.naturalRefineLimit=snapshot.naturalLimit;
                item.specialForgeCount=Math.Max(0,item.specialForgeCount-1);
                break;
        }

        if(operation==Operation.ItemLevelUp || operation==Operation.NormalUp ||
           operation==Operation.SpecialUp)
        {
            item.hasBeenRefined=true;
            item.highestRefineLevel=Math.Max(item.highestRefineLevel,item.refineLevel);
        }
        // Discovery is tied STRICTLY to a rarity transition, not to
        // item level, attempt count or save normalization.
        if(item.rarity>rarityBefore)
            EquipmentFoundation.RevealEligibleAffixes(item);
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

}
