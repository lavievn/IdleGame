using System;
using System.Collections.Generic;
using TuTienCore;

// Schema only: no drops, crafting RNG, gold deductions or book rewards are
// activated by this file. These classes are compatible with Unity JsonUtility.
public enum EquipmentSlot
{
    Weapon, Head, Chest, Boots, Artifact, Necklace, RingLeft, RingRight
}
public enum EquipmentRarity
{
    Common, Hoang, Huyen, Dia, Thien, Tien
}
public enum SupportProfession
{
    Forging, Alchemy, Talisman, Enchantment
}
public enum RefinementFailureEffect
{
    NoChange, LoseRefineLevel, DestroyItem
}

[Serializable]
public sealed class ItemStatLine
{
    public CombatStatId stat;
    public float flat;
    public float percent;
    public ElementType element = ElementType.Vo;
    public bool anyElement;
    public List<string> requiredTags = new List<string>();
    public List<string> blockedTags = new List<string>();

    public CombatStatModifier AsModifier(string sourceId)
    {
        return new CombatStatModifier {
            sourceId = sourceId, stat = stat, flat = flat, percent = percent,
            element = element, anyElement = anyElement,
            requiredTags = requiredTags == null ? new List<string>() :
                new List<string>(requiredTags),
            blockedTags = blockedTags == null ? new List<string>() :
                new List<string>(blockedTags)
        };
    }
}

[Serializable]
public sealed class EquipmentInstance
{
    // Unique per physical item. Never stack two equipment pieces just because
    // they share a template, level or rarity.
    public string instanceId = "";
    public string templateId = "";
    public int level = 1;
    public EquipmentRarity rarity = EquipmentRarity.Common;
    public int refineLevel = 0;
    // None of these lines are generated again when an item is loaded.
    public List<ItemStatLine> mainStats = new List<ItemStatLine>();
    public List<ItemStatLine> extraStats = new List<ItemStatLine>();
    public bool isEquipped;
    public EquipmentSlot equippedSlot = EquipmentSlot.Weapon;
}

[Serializable]
public sealed class ProfessionProgress
{
    public SupportProfession profession;
    public int level = 1;
    public int experience;
}
[Serializable]
public sealed class LearnedProfessionRecipe
{
    public string recipeId = "";
    public int learnedRank = 1;
}
[Serializable]
public sealed class LearnedSupportSkill
{
    public string skillId = "";
    public int level = 1;
}
[Serializable]
public sealed class CraftingMaterialCost
{
    public string itemId = "";
    public int quantity = 1;
}
[Serializable]
public sealed class CraftingRecipeData
{
    public string recipeId = "";
    public SupportProfession profession;
    public int requiredProfessionLevel = 1;
    public int goldCost;
    public List<CraftingMaterialCost> materials = new List<CraftingMaterialCost>();
    public string outputTemplateId = "";
    public int outputQuantity = 1;
}
[Serializable]
public sealed class RefinementRuleData
{
    public int targetRefineLevel = 1;
    public int requiredProfessionLevel = 1;
    public int goldCost;
    // Integer precision: 1 / 1,000,000 for future success checks.
    // No probability is evaluated until a refinement action is implemented.
    public int successWeightPerMillion = 1000000;
    public RefinementFailureEffect onFailure = RefinementFailureEffect.NoChange;
    public List<CraftingMaterialCost> materials = new List<CraftingMaterialCost>();
}

public sealed class EquipmentTemplate
{
    public readonly string id;
    public readonly string name;
    public readonly EquipmentSlot slot;
    public readonly CombatStatId mainStat;
    public EquipmentTemplate(string id,string name,EquipmentSlot slot,CombatStatId stat)
    { this.id=id;this.name=name;this.slot=slot;mainStat=stat; }
}

public static class EquipmentFoundation
{
    public const int Version = 2;
    public const string SourcePrefix = "equipment:instance:";
    public const string DemoSword = "sample:iron_sword";

    // Ratios are true probabilities, NOT percentage values. 0.005% =
    // 0.00005 chance, 0.001% = 0.00001 chance.
    // Other tiers are intentionally undefined until the drop table is balanced.
    public const double ThienDropProbability = 0.00005d;
    public const double TienDropProbability = 0.00001d;
    public const int MaximumItemLevel = 999;
    public const int MaximumRefineLevel = 100;

    // Extend via catalog data later; these initial templates establish
    // real slot compatibility, but no other gear is generated/dropped yet.
    private static readonly EquipmentTemplate[] templates = {
        new EquipmentTemplate(DemoSword,"Thiết Kiếm",EquipmentSlot.Weapon,CombatStatId.Attack),
        new EquipmentTemplate("base:head","Mũ vải",EquipmentSlot.Head,CombatStatId.Armor),
        new EquipmentTemplate("base:chest","Áo vải",EquipmentSlot.Chest,CombatStatId.Armor),
        new EquipmentTemplate("base:boots","Giày vải",EquipmentSlot.Boots,CombatStatId.MovementSpeed),
        new EquipmentTemplate("base:artifact","Pháp bảo",EquipmentSlot.Artifact,CombatStatId.MaxHealth),
        new EquipmentTemplate("base:necklace","Dây chuyền",EquipmentSlot.Necklace,CombatStatId.MagicResistance),
        new EquipmentTemplate("base:ring","Nhẫn",EquipmentSlot.RingLeft,CombatStatId.Attack)
    };
    public static EquipmentTemplate FindTemplate(string id)
    {
        if(string.IsNullOrEmpty(id)) return null;
        foreach(var item in templates)
            if(string.Equals(item.id,id,StringComparison.Ordinal)) return item;
        return null;
    }
    public static EquipmentInstance CreateCommon(string templateId,int level,float baseStat)
    {
        var template=FindTemplate(templateId);
        if(template==null || level<1 || level>MaximumItemLevel ||
            float.IsNaN(baseStat) || float.IsInfinity(baseStat)) return null;
        var main=new ItemStatLine {stat=template.mainStat,flat=baseStat};
        if(template.slot==EquipmentSlot.Weapon)
            main.requiredTags.Add("stance:melee");
        return new EquipmentInstance {
            instanceId=Guid.NewGuid().ToString("N"),
            templateId=templateId,level=level,
            rarity=EquipmentRarity.Common,refineLevel=0,
            mainStats=new List<ItemStatLine>{main}
        };
    }

    public static string RarityName(EquipmentRarity rarity)
    {
        switch(rarity)
        {
            case EquipmentRarity.Hoang:return "Hoàng";
            case EquipmentRarity.Huyen:return "Huyền";
            case EquipmentRarity.Dia:return "Địa";
            case EquipmentRarity.Thien:return "Thiên";
            case EquipmentRarity.Tien:return "Tiên";
            default:return "Phổ thông";
        }
    }

    // Provisional progression cap to make the schema testable. This limits
    // OPTIONAL lines only; guaranteed main stats are never counted here.
    // The actual balance curve will be data-driven in a later release.
    public static int MaximumExtraLines(int itemLevel)
    {
        int level=Math.Max(1,Math.Min(MaximumItemLevel,itemLevel));
        return Math.Min(6,1 + (level - 1)/10);
    }

    // Prototype creates only a common sword. Higher-quality equipment and
    // their stats will be rolled using a dedicated data table later.
    public static EquipmentInstance CreateDemoSword()
    {
        return CreateCommon(DemoSword,1,10f);
    }

    // Pure-data validation; never silently fabricate random stats on load.
    public static bool IsValid(EquipmentInstance item)
    {
        if (item == null || string.IsNullOrEmpty(item.instanceId)) return false;
        var template=FindTemplate(item.templateId);
        if (template == null || item.level < 1 || item.level > MaximumItemLevel ||
            (int)item.rarity < (int)EquipmentRarity.Common ||
            (int)item.rarity > (int)EquipmentRarity.Tien ||
            item.refineLevel < 0 || item.refineLevel > MaximumRefineLevel ||
            item.mainStats == null || item.mainStats.Count != 1 ||
            item.mainStats[0] == null || item.mainStats[0].stat != template.mainStat ||
            item.extraStats == null || item.extraStats.Count > MaximumExtraLines(item.level))
            return false;
        foreach(var line in item.mainStats) if (!ValidLine(line)) return false;
        foreach(var line in item.extraStats) if (!ValidLine(line)) return false;
        return true;
    }
    private static bool ValidLine(ItemStatLine line)
    {
        return line != null && Enum.IsDefined(typeof(CombatStatId),line.stat) &&
            !float.IsInfinity(line.flat) && !float.IsNaN(line.flat) &&
            !float.IsInfinity(line.percent) && !float.IsNaN(line.percent);
    }

    public static bool IsSlotAllowed(EquipmentInstance item,EquipmentSlot slot)
    {
        var definition=item == null ? null : FindTemplate(item.templateId);
        if (definition == null) return false;
        // Ring templates added later will allow two slots, never 2 copies of 1 item.
        return definition.slot == slot ||
            (definition.slot == EquipmentSlot.RingLeft && slot == EquipmentSlot.RingRight);
    }
}
