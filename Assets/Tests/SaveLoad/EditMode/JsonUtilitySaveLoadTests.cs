using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace IdleGame.Tests.SaveLoad
{
    [TestFixture]
    public sealed class JsonUtilitySaveLoadTests : SaveLoadFixture
    {
        [Test, Category("NativeJsonCharacterization")]
        public void ValidCharacter_RoundTripsAuthoritativeDataAndNestedGearInTemporaryDirectory()
        {
            var source = Deserialize();
            string json = JsonUtility.ToJson(source, true);
            string path = WriteSnapshot(json);
            var loaded = Deserialize(File.ReadAllText(path));

            Assert.That(Path.GetDirectoryName(path), Is.EqualTo(TemporaryDirectory));
            Assert.That(File.ReadAllText(path), Is.EqualTo(json));
            Assert.That(loaded, Is.Not.SameAs(source));
            Assert.That(RuntimeData.Get<string>(loaded, "entityName"), Is.EqualTo("Kiểm thử — Hỏa Linh"));
            Assert.That(RuntimeData.Get<int>(loaded, "currentLevel"), Is.EqualTo(7));
            Assert.That(RuntimeData.Get<int>(loaded, "currentExp"), Is.EqualTo(73));
            Assert.That(RuntimeData.Get<int>(loaded, "statPoints"), Is.EqualTo(2));
            Assert.That(RuntimeData.Get<int>(loaded, "addedHealth"), Is.EqualTo(12));
            Assert.That(RuntimeData.Get<int>(loaded, "addedDamage"), Is.EqualTo(2));
            Assert.That(RuntimeData.Get<int>(loaded, "originHealth"), Is.EqualTo(400));
            Assert.That(RuntimeData.Get<int>(loaded, "meleeGrowthBonus"), Is.EqualTo(450));
            Assert.That(RuntimeData.Get<int>(loaded, "meleeGrowthThroughLevel"), Is.EqualTo(7));
            Assert.That(Convert.ToInt32(RuntimeData.Get<object>(loaded, "savedAttackMode")), Is.EqualTo(1));
            Assert.That(Convert.ToInt32(List(loaded, "spiritRoots")[0]), Is.EqualTo(3));
            Assert.That(List(loaded, "rootTiers")[0], Is.EqualTo(4));
            Assert.That(List(loaded, "rootWeights")[0], Is.EqualTo(1f));
            Assert.That(RuntimeData.Get<long>(loaded, "gold"), Is.EqualTo(5000000123L));
            Assert.That(RuntimeData.Get<string>(loaded, "equippedItemId"), Is.EqualTo("sample:health_charm"));
            Assert.That(RuntimeData.Get<bool>(loaded, "testItemsGranted"), Is.True);
            Assert.That(RuntimeData.Get<bool>(loaded, "demoSwordGranted"), Is.True);
            Assert.That(RuntimeData.Get<int>(List(loaded, "inventory")[0], "quantity"), Is.EqualTo(12));
            Assert.That(RuntimeData.Get<int>(List(loaded, "professionProgress")[0], "experience"), Is.EqualTo(27));
            Assert.That(RuntimeData.Get<string>(List(loaded, "learnedRecipes")[0], "recipeId"), Is.EqualTo("forge:iron"));
            Assert.That(RuntimeData.Get<string>(List(loaded, "learnedSupportSkills")[0], "skillId"), Is.EqualTo("buff:armor"));
            Assert.That(RuntimeData.Get<string>(loaded, "worldMapId"), Is.EqualTo("TV-20"));
            Assert.That(RuntimeData.Get<int>(loaded, "worldMapWaveProgress"), Is.EqualTo(3));
            Assert.That(RuntimeData.Get<int>(loaded, "worldMapLoopWave"), Is.EqualTo(4));
            Assert.That(RuntimeData.Get<int>(loaded, "mapVisits"), Is.EqualTo(6));
            Assert.That(RuntimeData.Get<int>(loaded, "completedWavesInMap"), Is.EqualTo(3));
            Assert.That(List(loaded, "worldMapVisited"), Is.EqualTo(new[] { "TV-20", "TL-16" }));
            Assert.That(List(loaded, "worldMapCleared"), Is.EqualTo(new[] { "TL-16" }));
            Assert.That(JsonUtility.ToJson(Sword(loaded)), Is.EqualTo(JsonUtility.ToJson(Sword(source))));
            Assert.That(Sword(loaded), Is.Not.SameAs(Sword(source)));
            string sourceBefore = JsonUtility.ToJson(source);
            RuntimeData.Set(Sword(loaded), "refineLevel", 5);
            Assert.That(JsonUtility.ToJson(source), Is.EqualTo(sourceBefore), "Loaded items must not alias source items.");
        }

        [Test, Category("NativeJsonCharacterization")]
        public void MissingFields_OverwriteRetainsExistingValuesRatherThanApplyingMigration()
        {
            var template = Deserialize();
            string gearBefore = JsonUtility.ToJson(Sword(template));
            JsonUtility.FromJsonOverwrite("{\"currentLevel\":3}", template);

            Assert.That(RuntimeData.Get<int>(template, "currentLevel"), Is.EqualTo(3));
            Assert.That(RuntimeData.Get<int>(template, "currentExp"), Is.EqualTo(73));
            Assert.That(RuntimeData.Get<int>(template, "statPoints"), Is.EqualTo(2));
            Assert.That(RuntimeData.Get<int>(template, "addedDamage"), Is.EqualTo(2));
            Assert.That(JsonUtility.ToJson(Sword(template)), Is.EqualTo(gearBefore));
            // This documents serializer behavior, not safe LoadGame behavior.
        }

        [Test, Category("NativeJsonCharacterization")]
        public void MissingFields_FreshEntityRetainsItsDeclaredDefaults()
        {
            var loaded = Deserialize("{\"currentLevel\":7}");
            Assert.That(RuntimeData.Get<int>(loaded, "currentLevel"), Is.EqualTo(7));
            Assert.That(RuntimeData.Get<int>(loaded, "currentExp"), Is.Zero);
            Assert.That(RuntimeData.Get<int>(loaded, "statPoints"), Is.Zero);
            Assert.That(Convert.ToInt32(RuntimeData.Get<object>(loaded, "savedAttackMode")), Is.Zero);
            Assert.That(List(loaded, "inventory"), Is.Empty);
            Assert.That(List(loaded, "equipmentItems"), Is.Empty);
            Assert.That(List(loaded, "learnedSupportSkills"), Is.Empty);
            Assert.That(RuntimeData.Get<int>(loaded, "worldMapVersion"), Is.Zero);
        }

        [Test, Category("NativeJsonCharacterization")]
        public void FutureVersions_NativeJsonPreservesKnownFieldsWithoutVersionValidation()
        {
            var source = Deserialize();
            foreach (string field in new[] { "balanceVersion", "combatStatsVersion", "inventoryVersion", "equipmentVersion", "worldMapVersion" })
                RuntimeData.Set(source, field, 1000);
            RuntimeData.Set(Sword(source), "affixSchemaVersion", 1000);
            RuntimeData.Set(Sword(source), "forgeSchemaVersion", 1000);
            RuntimeData.Set(List(Sword(source), "forgeHistory")[0], "checkpointVersion", 1000);

            var loaded = Deserialize(JsonUtility.ToJson(source));
            foreach (string field in new[] { "balanceVersion", "combatStatsVersion", "inventoryVersion", "equipmentVersion", "worldMapVersion" })
                Assert.That(RuntimeData.Get<int>(loaded, field), Is.EqualTo(1000), field);
            Assert.That(JsonUtility.ToJson(Sword(loaded)), Is.EqualTo(JsonUtility.ToJson(Sword(source))));
        }

        [Test, Category("NativeJsonCharacterization")]
        public void UnknownFutureField_IsNotRetainedByNativeReserialization()
        {
            var loaded = Deserialize("{\"currentLevel\":7,\"futureOnlyAuditPayload\":\"must-not-be-silently-resaved\"}");
            string output = JsonUtility.ToJson(loaded);
            Assert.That(RuntimeData.Get<int>(loaded, "currentLevel"), Is.EqualTo(7));
            Assert.That(output, Does.Not.Contain("futureOnlyAuditPayload"));
            // Native JsonUtility is not an opaque forward-compatible save store.
        }

        [Test, Category("SaveDataNormalization")]
        public void CurrentEquipment_NormalizePreservesRolledDataAndForgeHistoryWithoutDuplicatingEffects()
        {
            var loaded = Deserialize();
            string swordBefore = JsonUtility.ToJson(Sword(loaded));
            NormalizeEquipment(loaded);
            string normalized = JsonUtility.ToJson(loaded);
            NormalizeEquipment(loaded);
            NormalizeEquipment(loaded);

            Assert.That(JsonUtility.ToJson(Sword(loaded)), Is.EqualTo(swordBefore));
            Assert.That(JsonUtility.ToJson(loaded), Is.EqualTo(normalized), "Normalization must be idempotent.");
            int traitCount = 0, legacyCount = 0, gearCount = 0;
            foreach (object modifier in List(loaded, "combatStatModifiers"))
            {
                string source = RuntimeData.Get<string>(modifier, "sourceId");
                if (source == "trait:origin:test")
                {
                    traitCount++;
                    Assert.That(RuntimeData.Get<float>(modifier, "flat"), Is.EqualTo(3f));
                }
                else if (source == "equipment:primary-trinket")
                {
                    legacyCount++;
                    Assert.That(RuntimeData.Get<float>(modifier, "flat"), Is.EqualTo(100f));
                }
                else if (source.StartsWith("equipment:instance:", StringComparison.Ordinal))
                {
                    gearCount++;
                    Assert.That(source, Does.StartWith("equipment:instance:audit-sword-001:"));
                }
                else Assert.Fail("Unexpected modifier source: " + source);
            }
            Assert.That(traitCount, Is.EqualTo(1));
            Assert.That(legacyCount, Is.EqualTo(1));
            Assert.That(gearCount, Is.EqualTo(2), "Only the main stat and revealed active magic affix apply.");
            Assert.That(RuntimeData.Get<bool>(List(Sword(loaded), "extraStats")[0], "revealed"), Is.False);
        }

        [Test, Category("SaveDataNormalization")]
        public void LegacyEquipment_MigrationPreservesRollsAndExistingForgeHistory()
        {
            var loaded = Deserialize();
            object sword = Sword(loaded);
            RuntimeData.Set(loaded, "equipmentVersion", 2);
            RuntimeData.Set(sword, "affixSchemaVersion", 0);
            RuntimeData.Set(sword, "forgeSchemaVersion", 1);
            string mainBefore = JsonUtility.ToJson(List(sword, "mainStats")[0]);
            string historyBefore = JsonUtility.ToJson(List(sword, "forgeHistory")[0]);
            NormalizeEquipment(loaded);

            Assert.That(RuntimeData.Get<string>(sword, "instanceId"), Is.EqualTo("audit-sword-001"));
            Assert.That(RuntimeData.Get<int>(sword, "level"), Is.EqualTo(21));
            Assert.That(RuntimeData.Get<int>(sword, "refineLevel"), Is.EqualTo(4));
            Assert.That(Convert.ToInt32(RuntimeData.Get<object>(sword, "rarity")), Is.EqualTo(1));
            Assert.That(RuntimeData.Get<int>(loaded, "equipmentVersion"), Is.EqualTo(RuntimeData.Constant("EquipmentFoundation", "Version")));
            Assert.That(RuntimeData.Get<int>(sword, "affixSchemaVersion"), Is.EqualTo(RuntimeData.Constant("EquipmentFoundation", "AffixSchemaVersion")));
            Assert.That(RuntimeData.Get<int>(sword, "forgeSchemaVersion"), Is.EqualTo(RuntimeData.Constant("EquipmentRefinement", "SchemaVersion")));
            Assert.That(JsonUtility.ToJson(List(sword, "mainStats")[0]), Is.EqualTo(mainBefore));
            Assert.That(List(sword, "forgeHistory").Count, Is.EqualTo(1));
            Assert.That(JsonUtility.ToJson(List(sword, "forgeHistory")[0]), Is.EqualTo(historyBefore));
            Assert.That(RuntimeData.Get<float>(List(sword, "extraStats")[0], "flat"), Is.EqualTo(.05f));
            Assert.That(RuntimeData.Get<float>(List(sword, "extraStats")[1], "flat"), Is.EqualTo(4f));
            Assert.That(RuntimeData.Get<bool>(List(sword, "extraStats")[0], "revealed"), Is.True);
            Assert.That(RuntimeData.Get<bool>(List(sword, "extraStats")[1], "revealed"), Is.True);
            string normalized = JsonUtility.ToJson(loaded);
            NormalizeEquipment(loaded);
            Assert.That(JsonUtility.ToJson(loaded), Is.EqualTo(normalized));
        }

        [Test, Category("SaveDataNormalization")]
        public void AfterDeserialization_DerivedStatsAreRebuiltFromAuthoritativeProgressAndEquipment()
        {
            var source = Deserialize();
            // Stored derived values deliberately disagree with the canonical level/origin.
            RuntimeData.Set(source, "baseHealth", 9999);
            RuntimeData.Set(source, "baseDamage", 9999);
            RuntimeData.Set(source, "baseAttackSpeed", 9f);
            RuntimeData.Set(source, "expToNextLevel", 9999);
            string sourceBefore = JsonUtility.ToJson(source);
            var loaded = Deserialize(sourceBefore);
            NormalizeEquipment(loaded);
            RuntimeData.Call(loaded, "ApplyHeroBalance");

            // CombatBalance defaults: HP=400+16*6; ATK=10+6;
            // EXP=ceil(100*1.15^6); speed=1+.005*6.
            Assert.That(RuntimeData.Get<int>(loaded, "baseHealth"), Is.EqualTo(496));
            Assert.That(RuntimeData.Get<int>(loaded, "baseDamage"), Is.EqualTo(16));
            Assert.That(RuntimeData.Get<float>(loaded, "baseAttackSpeed"), Is.EqualTo(1.03f).Within(.00001f));
            Assert.That(RuntimeData.Get<int>(loaded, "expToNextLevel"), Is.EqualTo(232));
            Assert.That(RuntimeData.Get<int>(loaded, "currentLevel"), Is.EqualTo(7));
            Assert.That(RuntimeData.Get<int>(loaded, "currentExp"), Is.EqualTo(73));
            Assert.That(RuntimeData.Get<int>(loaded, "statPoints"), Is.EqualTo(2));
            Assert.That(RuntimeData.Get<int>(loaded, "addedHealth"), Is.EqualTo(12));
            Assert.That(RuntimeData.Get<int>(loaded, "addedDamage"), Is.EqualTo(2));
            Assert.That(JsonUtility.ToJson(Sword(loaded)), Is.EqualTo(JsonUtility.ToJson(Sword(source))));
            // Stored origin 400, six level steps of 16, 12 allocated,
            // 450 legacy growth, and the active +100 health charm.
            const int expectedHealth = 400 + 6 * 16 + 12 + 450 + 100;
            int[] expectedAttack = { 31, 21, 25 }; // Base 16 + points 2 + trait 3 + stance-eligible gear.
            for (int mode = 0; mode < 3; mode++)
            {
                object stance = Enum.ToObject(RuntimeData.Type("TuTienCore.AttackMode"), mode);
                Assert.That((int)RuntimeData.Call(loaded, "GetCalculatedHealth", stance), Is.EqualTo(expectedHealth));
                Assert.That((int)RuntimeData.Call(loaded, "GetCalculatedDamage", stance), Is.EqualTo(expectedAttack[mode]));
            }
            Assert.That(JsonUtility.ToJson(source), Is.EqualTo(sourceBefore));
            string normalized = JsonUtility.ToJson(loaded);
            NormalizeEquipment(loaded);
            RuntimeData.Call(loaded, "ApplyHeroBalance");
            Assert.That(JsonUtility.ToJson(loaded), Is.EqualTo(normalized));
        }

        [TestCase("inventoryVersion", "EquipmentInventory", "SchemaVersion")]
        [TestCase("equipmentVersion", "EquipmentFoundation", "Version")]
        [Category("SaveDataSafety")]
        public void FutureSubsystemVersion_NormalizeMustNotDowngradeUnsupportedVersion(
            string field, string versionType, string versionConstant)
        {
            var source = Deserialize();
            int future = RuntimeData.Constant(versionType, versionConstant) + 1;
            RuntimeData.Set(source, field, future);
            var loaded = Deserialize(JsonUtility.ToJson(source));
            string swordBefore = JsonUtility.ToJson(Sword(loaded));
            NormalizeEquipment(loaded);

            Assert.That(RuntimeData.Get<int>(loaded, field), Is.EqualTo(future),
                "Unsupported future data must not be silently relabeled as the current schema.");
            Assert.That(JsonUtility.ToJson(Sword(loaded)), Is.EqualTo(swordBefore));
        }

        [Test, Category("SaveDataSafety")]
        public void FutureForgeVersion_NormalizeMustPreserveUnsupportedProgressAndHistory()
        {
            var source = Deserialize();
            Assert.That((bool)RuntimeData.Static("EquipmentFoundation", "IsValid", Sword(source)), Is.True,
                "The fixture is valid before adding an unsupported Forge version.");
            int future = RuntimeData.Constant("EquipmentRefinement", "SchemaVersion") + 1;
            RuntimeData.Set(Sword(source), "forgeSchemaVersion", future);
            var loaded = Deserialize(JsonUtility.ToJson(source));
            object sword = Sword(loaded);
            Assert.That((bool)RuntimeData.Static("EquipmentFoundation", "IsValid", sword), Is.False,
                "Unsupported Forge metadata blocks runtime use.");
            string swordBefore = JsonUtility.ToJson(sword);
            NormalizeEquipment(loaded);

            // These are safety requirements, not assertions of the known destructive behavior.
            // Do not Ignore/Explicit this test or change expected history to empty to make it pass.
            Assert.That(RuntimeData.Get<int>(sword, "forgeSchemaVersion"), Is.EqualTo(future));
            Assert.That(RuntimeData.Get<int>(sword, "forgeStageProgress"), Is.EqualTo(2));
            Assert.That(RuntimeData.Get<int>(sword, "specialForgeCount"), Is.EqualTo(2));
            Assert.That(List(sword, "forgeHistory").Count, Is.EqualTo(1));
            Assert.That(JsonUtility.ToJson(sword), Is.EqualTo(swordBefore),
                "Unknown forge schema must not lose progress, history, rolls or slot assignment.");
        }
    }
}
