using System;
using System.Collections.Generic;

// 0.0.52: one shared bag for the current run, one primary-Hero trinket slot.
// Items are IDs, not Unity prefab references; the save remains portable.
[Serializable]
public sealed class InventoryStack
{
    public string itemId = "";
    public int quantity = 1;
}

public sealed class EquipmentDefinition
{
    public readonly string id;
    public readonly string name;
    public readonly CombatStatId stat;
    public readonly float flat;
    public EquipmentDefinition(string id, string name, CombatStatId stat, float flat)
    {
        this.id = id; this.name = name; this.stat = stat; this.flat = flat;
    }
}

public static class EquipmentCatalog
{
    public const string HealthCharm = "sample:health_charm";
    public const string AttackCharm = "sample:attack_charm";
    public static readonly EquipmentDefinition Health =
        new EquipmentDefinition(HealthCharm, "Hộ phù Sinh Mệnh", CombatStatId.MaxHealth, 100f);
    public static readonly EquipmentDefinition Attack =
        new EquipmentDefinition(AttackCharm, "Hộ phù Công Kích", CombatStatId.Attack, 20f);

    public static EquipmentDefinition Find(string id)
    {
        if (string.Equals(id, HealthCharm, StringComparison.Ordinal)) return Health;
        if (string.Equals(id, AttackCharm, StringComparison.Ordinal)) return Attack;
        return null;
    }
    public static string StatLabel(EquipmentDefinition item)
    {
        if (item == null) return "";
        return (item.stat == CombatStatId.MaxHealth ? "HP +" : "ATK +") +
            item.flat.ToString("0");
    }
}

public static class EquipmentInventory
{
    public const int SchemaVersion = 1;
    public const string ModifierSource = "equipment:primary-trinket";
    public const int MaxQuantity = 9999;

    public static void InitializeNewRun(EntityDataSO hero)
    {
        if (hero == null) return;
        hero.inventoryVersion = SchemaVersion;
        hero.inventory = new List<InventoryStack>();
        hero.equippedItemId = "";
        hero.testItemsGranted = false;
        RebuildModifier(hero);
        GrantSamples(hero);
        hero.isDirty = true;
    }

    // Migration: old saves have empty bag and no equipped item. An invalid or
    // stale equipment-owned modifier is removed, never counted twice.
    public static void Normalize(EntityDataSO hero)
    {
        if (hero == null) return;
        bool changed = false;
        if (hero.inventory == null) { hero.inventory = new List<InventoryStack>(); changed = true; }
        var unique = new List<InventoryStack>();
        foreach (var entry in hero.inventory)
        {
            if (entry == null || EquipmentCatalog.Find(entry.itemId) == null || entry.quantity <= 0)
            { changed = true; continue; }
            int quantity = Math.Min(MaxQuantity, entry.quantity);
            InventoryStack existing = unique.Find(x => x.itemId == entry.itemId);
            if (existing == null)
                unique.Add(new InventoryStack { itemId = entry.itemId, quantity = quantity });
            else
            {
                existing.quantity = Math.Min(MaxQuantity, existing.quantity + quantity);
                changed = true;
            }
            if (quantity != entry.quantity) changed = true;
        }
        if (changed || unique.Count != hero.inventory.Count)
        {
            hero.inventory = unique;
            changed = true;
        }
        if (EquipmentCatalog.Find(hero.equippedItemId) == null &&
            !string.IsNullOrEmpty(hero.equippedItemId))
        { hero.equippedItemId = ""; changed = true; }
        if (hero.inventoryVersion != SchemaVersion)
        { hero.inventoryVersion = SchemaVersion; changed = true; }
        if (RebuildModifier(hero)) changed = true;
        if (changed) hero.isDirty = true;
    }

    public static int Count(EntityDataSO hero, string itemId)
    {
        if (hero == null || hero.inventory == null) return 0;
        var item = hero.inventory.Find(x => x != null && x.itemId == itemId);
        return item == null ? 0 : item.quantity;
    }

    public static bool Add(EntityDataSO hero, string itemId, int quantity = 1)
    {
        if (hero == null || EquipmentCatalog.Find(itemId) == null ||
            quantity <= 0 || quantity > MaxQuantity) return false;
        if (hero.inventory == null) hero.inventory = new List<InventoryStack>();
        var item = hero.inventory.Find(x => x != null && x.itemId == itemId);
        if (item == null)
            hero.inventory.Add(new InventoryStack { itemId = itemId, quantity = quantity });
        else
        {
            if (item.quantity > MaxQuantity - quantity) return false;
            item.quantity += quantity;
        }
        hero.isDirty = true;
        return true;
    }

    public static bool GrantSamples(EntityDataSO hero)
    {
        if (hero == null || hero.testItemsGranted) return false;
        Normalize(hero);
        if (!CanAdd(hero, EquipmentCatalog.HealthCharm) ||
            !CanAdd(hero, EquipmentCatalog.AttackCharm)) return false;
        Add(hero, EquipmentCatalog.HealthCharm);
        Add(hero, EquipmentCatalog.AttackCharm);
        hero.testItemsGranted = true;
        hero.isDirty = true;
        return true;
    }

    private static bool CanAdd(EntityDataSO hero, string itemId)
    {
        return Count(hero,itemId) < MaxQuantity;
    }

    public static bool Equip(EntityDataSO hero, string itemId)
    {
        if (hero == null || EquipmentCatalog.Find(itemId) == null ||
            Count(hero,itemId) <= 0 || hero.equippedItemId == itemId) return false;
        if (!string.IsNullOrEmpty(hero.equippedItemId) &&
            !CanAdd(hero,hero.equippedItemId)) return false;
        var stack = hero.inventory.Find(x => x != null && x.itemId == itemId);
        stack.quantity--;
        if (stack.quantity == 0) hero.inventory.Remove(stack);
        string old = hero.equippedItemId;
        hero.equippedItemId = itemId;
        if (!string.IsNullOrEmpty(old)) Add(hero,old,1);
        RebuildModifier(hero);
        hero.isDirty = true;
        return true;
    }

    public static bool Unequip(EntityDataSO hero)
    {
        if (hero == null || EquipmentCatalog.Find(hero.equippedItemId) == null) return false;
        if (!CanAdd(hero,hero.equippedItemId)) return false;
        string old = hero.equippedItemId;
        hero.equippedItemId = "";
        Add(hero,old,1);
        RebuildModifier(hero);
        hero.isDirty = true;
        return true;
    }

    // Derive one combat modifier from the equipped ID. Never accumulate gear
    // effects inside permanent stats or alongside old load-time modifiers.
    private static bool RebuildModifier(EntityDataSO hero)
    {
        if (hero.combatStatModifiers == null)
            hero.combatStatModifiers = new List<CombatStatModifier>();
        var definition = EquipmentCatalog.Find(hero.equippedItemId);
        int count = 0;
        bool correct = true;
        foreach (var mod in hero.combatStatModifiers)
        {
            if (mod == null || mod.sourceId != ModifierSource) continue;
            count++;
            if (definition == null || mod.stat != definition.stat ||
                mod.flat != definition.flat || mod.percent != 0f ||
                mod.anyElement || (mod.requiredTags != null && mod.requiredTags.Count > 0) ||
                (mod.blockedTags != null && mod.blockedTags.Count > 0))
                correct = false;
        }
        if (correct && count == (definition == null ? 0 : 1)) return false;
        hero.combatStatModifiers.RemoveAll(x => x != null && x.sourceId == ModifierSource);
        if (definition != null)
            hero.combatStatModifiers.Add(new CombatStatModifier {
                sourceId = ModifierSource, stat = definition.stat, flat = definition.flat
            });
        return true;
    }
}
