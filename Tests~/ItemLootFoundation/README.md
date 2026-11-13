# Item/Loot foundation — data-only regression

```bash
bash Tests~/ItemLootFoundation/run.sh
# Optional existing compiler/Mono executables or wrapper paths:
CSC=/path/to/csharp-compiler-wrapper MONO=/path/to/mono bash Tests~/ItemLootFoundation/run.sh
```

Requires C# 7.2+ and Mono; no Editor, Unity license, asset cache or player save.
The runner exits nonzero on any failed group and compiles the actual production
EquipmentFoundation, EquipmentRuntime, EquipmentInventory, EquipmentRefinement,
EquipmentStatPreview, ProductionLootConfig and CombatLootRewards implementations.
It uses the repository's existing UnityStub/NestedEquipmentSaveStub for engine
types and in-memory JSON. One explicit environment boundary throws if crossed;
it does not replace any tested Item/Loot/Forge logic. The duplicate factory test
injects an invalid repeated ID through the supported factory injection seam;
the production receipt and inventory validators must reject it.

The 19 groups cover catalog integrity/immutable access, independent RNG streams,
invalid configuration/overflow, stale/duplicate receipts, actual data APIs from
loot through Forge, physical/magical stats and HP, skill compatibility, all eight
slots, legacy charms, missing fields/templates, unsupported versions, stable IDs
and deep-copy ownership. Numeric reward values and seeds are fixtures only.

JSON checks are **UnityStub serialization**, not Unity JsonUtility or SaveManager
Save/Load gameplay. No SaveManager, scene, file-based save fixture or gameplay
manager is created. Stale context tests call the production receipt boundary;
they do not perform GameManager Retry/Travel or exercise the Unity death callback.
Unity Play Mode and Windows Player remain **NOT TESTED**.

See `Docs/automation-reports/CLOUD_ITEM_LOOT_FOUNDATION_REPORT.md` for audit,
baseline reproduction, balance decisions and the native acceptance checklist.
