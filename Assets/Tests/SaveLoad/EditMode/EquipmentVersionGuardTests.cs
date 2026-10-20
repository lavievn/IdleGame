using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace IdleGame.Tests.SaveLoad
{
    [TestFixture]
    public sealed class EquipmentVersionGuardTests : SaveLoadFixture
    {
        private GameObject managerOwner;

        [TearDown]
        public void DestroyInactiveManager()
        {
            if (managerOwner != null) UnityEngine.Object.DestroyImmediate(managerOwner);
            managerOwner = null;
        }

        private Component CreateIsolatedManager()
        {
            var managerType = RuntimeData.Type("SaveManager");
            object previousInstance = managerType.GetProperty("Instance").GetValue(null);
            managerOwner = new GameObject("W1.7a isolated SaveManager");
            // Do this BEFORE AddComponent: Awake must never choose player Saves.
            managerOwner.SetActive(false);
            Component manager = managerOwner.AddComponent(managerType);
            var directory = managerType.GetField("saveDirectory", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(directory.GetValue(manager), Is.Null, "SaveManager.Awake must not have run.");
            Assert.That(managerType.GetProperty("Instance").GetValue(null), Is.SameAs(previousInstance));
            directory.SetValue(manager, TemporaryDirectory);
            return manager; // Never activate; exercise synchronous persistence APIs only.
        }

        private static object ManualSlot()
        {
            return Enum.Parse(RuntimeData.Type("SaveSlot"), "ManualSave1");
        }

        private static void Unsupported(object hero, string kind, object item = null, int? forced = null)
        {
            if (item == null) item = Sword(hero);
            switch (kind)
            {
                case "inventory":
                    RuntimeData.Set(hero, "inventoryVersion", forced ?? RuntimeData.Constant("EquipmentInventory", "SchemaVersion") + 1);
                    break;
                case "equipment":
                    RuntimeData.Set(hero, "equipmentVersion", forced ?? RuntimeData.Constant("EquipmentFoundation", "Version") + 1);
                    break;
                case "affix":
                    RuntimeData.Set(item, "affixSchemaVersion", forced ?? RuntimeData.Constant("EquipmentFoundation", "AffixSchemaVersion") + 1);
                    RuntimeData.Set(item, "forgeSchemaVersion", 1); // Would migrate without an early guard.
                    break;
                case "forge":
                    RuntimeData.Set(item, "forgeSchemaVersion", forced ?? RuntimeData.Constant("EquipmentRefinement", "SchemaVersion") + 1);
                    break;
                case "checkpoint":
                    RuntimeData.Set(List(item, "forgeHistory")[0], "checkpointVersion", forced ?? RuntimeData.Constant("EquipmentRefinement", "SchemaVersion") + 1);
                    RuntimeData.Set(item, "forgeSchemaVersion", 1);
                    break;
                default: throw new ArgumentException("Unknown version case: " + kind);
            }
        }

        [TestCase("inventory", false)]
        [TestCase("inventory", true)]
        [TestCase("equipment", false)]
        [TestCase("equipment", true)]
        [TestCase("affix", false)]
        [TestCase("affix", true)]
        [TestCase("forge", false)]
        [TestCase("forge", true)]
        [TestCase("checkpoint", false)]
        [TestCase("checkpoint", true)]
        public void DirectGuards_PreserveEntireHeroReferencesAndDirtyFlag(string kind, bool dirty)
        {
            var hero = Deserialize();
            Unsupported(hero, kind);
            RuntimeData.Set(hero, "isDirty", dirty);
            object bag = List(hero, "inventory"), items = List(hero, "equipmentItems");
            object modifiers = List(hero, "combatStatModifiers");
            string before = JsonUtility.ToJson(hero);
            Assert.That((bool)RuntimeData.Static("EquipmentFoundation", "SupportsSaveVersions", hero), Is.False);
            Assert.That(JsonUtility.ToJson(hero), Is.EqualTo(before), "Validation itself must be pure.");

            RuntimeData.Static("EquipmentInventory", "Normalize", hero);
            Assert.That(JsonUtility.ToJson(hero), Is.EqualTo(before), "Inventory guard must precede all mutation.");
            RuntimeData.Static("EquipmentRuntime", "Normalize", hero);
            Assert.That(JsonUtility.ToJson(hero), Is.EqualTo(before), "Equipment guard must precede all migration.");
            Assert.That(List(hero, "inventory"), Is.SameAs(bag));
            Assert.That(List(hero, "equipmentItems"), Is.SameAs(items));
            Assert.That(List(hero, "combatStatModifiers"), Is.SameAs(modifiers));
            Assert.That(RuntimeData.Get<bool>(hero, "isDirty"), Is.EqualTo(dirty));

            if (kind == "affix" || kind == "forge" || kind == "checkpoint")
            {
                Assert.That((bool)RuntimeData.Static("EquipmentRefinement", "NormalizeMetadata", Sword(hero)), Is.False);
                Assert.That(JsonUtility.ToJson(hero), Is.EqualTo(before), "Direct metadata call must also be a no-op.");
            }
        }

        [TestCase("affix")]
        [TestCase("forge")]
        [TestCase("checkpoint")]
        public void WholeListPreflight_UnknownUnequippedLastItemPreventsPartialMigration(string kind)
        {
            var hero = Deserialize();
            object later = Activator.CreateInstance(RuntimeData.Type("EquipmentInstance"));
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(Sword(hero)), later);
            RuntimeData.Set(later, "instanceId", "audit-later-unsupported");
            RuntimeData.Set(later, "templateId", "unknown:future-template");
            RuntimeData.Set(later, "isEquipped", false);
            Unsupported(hero, kind, later);
            List(hero, "equipmentItems").Add(later);
            RuntimeData.Set(Sword(hero), "affixSchemaVersion", 0);
            RuntimeData.Set(Sword(hero), "forgeSchemaVersion", 1);
            RuntimeData.Set(hero, "equipmentVersion", 2);
            string before = JsonUtility.ToJson(hero);

            NormalizeEquipment(hero);
            Assert.That(JsonUtility.ToJson(hero), Is.EqualTo(before),
                "Scan invalid/unknown and unequipped items before migrating the first supported item.");
        }

        [Test]
        public void PureGuard_AcceptsLegacyNullListsWithoutAllocatingOrChangingDirtyFlag()
        {
            var hero = NewEntity();
            RuntimeData.Set(hero, "inventoryVersion", 0);
            RuntimeData.Set(hero, "equipmentVersion", 0);
            RuntimeData.Set(hero, "inventory", null);
            RuntimeData.Set(hero, "equipmentItems", null);
            string before = JsonUtility.ToJson(hero);
            Assert.That((bool)RuntimeData.Static("EquipmentFoundation", "SupportsSaveVersions", hero), Is.True);
            Assert.That(JsonUtility.ToJson(hero), Is.EqualTo(before));
            Assert.That(RuntimeData.Get<object>(hero, "inventory"), Is.Null);
            Assert.That(RuntimeData.Get<object>(hero, "equipmentItems"), Is.Null);
        }

        [TestCase("inventory")]
        [TestCase("forge")]
        public void NegativeVersion_IsUnsupportedAndNeverTreatedAsLegacyZero(string kind)
        {
            var hero = Deserialize();
            Unsupported(hero, kind, forced: -1);
            string before = JsonUtility.ToJson(hero);
            Assert.That((bool)RuntimeData.Static("EquipmentFoundation", "SupportsSaveVersions", hero), Is.False);
            NormalizeEquipment(hero);
            Assert.That(JsonUtility.ToJson(hero), Is.EqualTo(before));
        }

        [TestCase("inventory")]
        [TestCase("equipment")]
        [TestCase("affix")]
        [TestCase("forge")]
        [TestCase("checkpoint")]
        public void RejectedLoad_PreservesDestinationAndExistingFiles(string kind)
        {
            var source = Deserialize();
            Unsupported(source, kind);
            string json = JsonUtility.ToJson(source, true);
            string final = WriteSnapshot(json);
            string temporary = Path.Combine(TemporaryDirectory, "ManualSave1.tmp");
            File.WriteAllText(temporary, "existing pending write");
            var destination = Deserialize();
            RuntimeData.Set(destination, "currentLevel", 4);
            RuntimeData.Set(destination, "currentExp", 11);
            RuntimeData.Set(destination, "isDirty", true);
            string before = JsonUtility.ToJson(destination);
            object items = List(destination, "equipmentItems");
            Component manager = CreateIsolatedManager();

            Assert.That((bool)RuntimeData.Call(manager, "LoadGame", destination, ManualSlot()), Is.False);
            Assert.That(JsonUtility.ToJson(destination), Is.EqualTo(before));
            Assert.That(List(destination, "equipmentItems"), Is.SameAs(items));
            Assert.That(File.ReadAllText(final), Is.EqualTo(json));
            Assert.That(File.ReadAllText(temporary), Is.EqualTo("existing pending write"));
        }

        [TestCase("inventory")]
        [TestCase("equipment")]
        [TestCase("affix")]
        [TestCase("forge")]
        [TestCase("checkpoint")]
        public void RejectedTypedSave_PreservesMemoryDirtyFlagAndExistingFiles(string kind)
        {
            var source = Deserialize();
            Unsupported(source, kind);
            RuntimeData.Set(source, "isDirty", true);
            string before = JsonUtility.ToJson(source);
            string final = WriteSnapshot(SnapshotJson);
            string temporary = Path.Combine(TemporaryDirectory, "ManualSave1.tmp");
            File.WriteAllText(temporary, "existing pending write");
            Component manager = CreateIsolatedManager();

            Assert.That((bool)RuntimeData.Call(manager, "SaveGame", source, ManualSlot()), Is.False);
            Assert.That(JsonUtility.ToJson(source), Is.EqualTo(before));
            Assert.That(File.ReadAllText(final), Is.EqualTo(SnapshotJson));
            Assert.That(File.ReadAllText(temporary), Is.EqualTo("existing pending write"));
        }

        [TestCase("inventory")]
        [TestCase("equipment")]
        [TestCase("affix")]
        [TestCase("forge")]
        [TestCase("checkpoint")]
        public void RejectedRawJsonSave_CannotBypassVersionGuard(string kind)
        {
            var source = Deserialize();
            Unsupported(source, kind);
            string json = JsonUtility.ToJson(source, true);
            string final = WriteSnapshot(SnapshotJson);
            string temporary = Path.Combine(TemporaryDirectory, "ManualSave1.tmp");
            File.WriteAllText(temporary, "existing pending write");
            Component manager = CreateIsolatedManager();

            RuntimeData.Call(manager, "SaveGame", "ManualSave1", json);
            Assert.That(File.ReadAllText(final), Is.EqualTo(SnapshotJson));
            Assert.That(File.ReadAllText(temporary), Is.EqualTo("existing pending write"));
        }

        [TestCase("inventory")]
        [TestCase("forge")]
        public void RejectedSave_DoesNotCreateFinalOrTemporaryFile(string kind)
        {
            var source = Deserialize();
            Unsupported(source, kind);
            RuntimeData.Set(source, "isDirty", false);
            string before = JsonUtility.ToJson(source);
            Component manager = CreateIsolatedManager();
            Assert.That((bool)RuntimeData.Call(manager, "SaveGame", source, ManualSlot()), Is.False);
            RuntimeData.Call(manager, "SaveGame", "ManualSave1", JsonUtility.ToJson(source, true));
            Assert.That(File.Exists(Path.Combine(TemporaryDirectory, "ManualSave1.json")), Is.False);
            Assert.That(File.Exists(Path.Combine(TemporaryDirectory, "ManualSave1.tmp")), Is.False);
            Assert.That(JsonUtility.ToJson(source), Is.EqualTo(before));
        }

        [TestCase("inventory")]
        [TestCase("equipment")]
        [TestCase("affix")]
        [TestCase("forge")]
        [TestCase("checkpoint")]
        public void SupportedData_CannotOverwriteExistingFutureSlot(string kind)
        {
            var future = Deserialize();
            Unsupported(future, kind);
            string original = JsonUtility.ToJson(future, true);
            string final = WriteSnapshot(original);
            string temporary = Path.Combine(TemporaryDirectory, "ManualSave1.tmp");
            File.WriteAllText(temporary, "existing pending write");
            var current = Deserialize();
            RuntimeData.Set(current, "isDirty", true);
            string before = JsonUtility.ToJson(current);
            Component manager = CreateIsolatedManager();

            Assert.That((bool)RuntimeData.Call(manager, "SaveGame", current, ManualSlot()), Is.False);
            RuntimeData.Call(manager, "SaveGame", "ManualSave1", JsonUtility.ToJson(current, true));
            Assert.That(JsonUtility.ToJson(current), Is.EqualTo(before));
            Assert.That(File.ReadAllText(final), Is.EqualTo(original));
            Assert.That(File.ReadAllText(temporary), Is.EqualTo("existing pending write"));
        }

        [Test]
        public void LegacyJsonWithoutEquipmentFields_StillLoadsThroughSaveManager()
        {
            string path = WriteSnapshot("{\"currentLevel\":2,\"currentExp\":0}");
            var destination = Deserialize();
            Component manager = CreateIsolatedManager();
            Assert.That((bool)RuntimeData.Call(manager, "LoadGame", destination, ManualSlot()), Is.True);
            Assert.That(RuntimeData.Get<int>(destination, "currentLevel"), Is.EqualTo(2));
            Assert.That(List(destination, "inventory"), Is.Empty);
            Assert.That(List(destination, "equipmentItems"), Is.Empty);
            Assert.That(RuntimeData.Get<int>(destination, "inventoryVersion"), Is.EqualTo(RuntimeData.Constant("EquipmentInventory", "SchemaVersion")));
            Assert.That(RuntimeData.Get<int>(destination, "equipmentVersion"), Is.EqualTo(RuntimeData.Constant("EquipmentFoundation", "Version")));
            Assert.That(File.ReadAllText(path), Is.EqualTo("{\"currentLevel\":2,\"currentExp\":0}"));
        }

        [Test]
        public void CurrentSnapshot_SaveLoadPreservesRollsHistoryAndProgress()
        {
            var source = Deserialize();
            string swordBefore = JsonUtility.ToJson(Sword(source));
            Component manager = CreateIsolatedManager();
            Assert.That((bool)RuntimeData.Call(manager, "SaveGame", source, ManualSlot()), Is.True);
            var destination = NewEntity();
            Assert.That((bool)RuntimeData.Call(manager, "LoadGame", destination, ManualSlot()), Is.True);
            Assert.That(JsonUtility.ToJson(Sword(destination)), Is.EqualTo(swordBefore));
            Assert.That(RuntimeData.Get<int>(destination, "currentLevel"), Is.EqualTo(7));
            Assert.That(RuntimeData.Get<int>(destination, "currentExp"), Is.EqualTo(73));
            Assert.That(RuntimeData.Get<int>(destination, "statPoints"), Is.EqualTo(2));
            Assert.That(RuntimeData.Get<string>(destination, "worldMapId"), Is.EqualTo("TV-20"));
            Assert.That(RuntimeData.Get<int>(destination, "worldMapWaveProgress"), Is.EqualTo(3));
            Assert.That(RuntimeData.Get<int>(destination, "worldMapLoopWave"), Is.EqualTo(4));
            Assert.That(List(destination, "worldMapVisited"), Is.EqualTo(new[] { "TV-20", "TL-16" }));
            Assert.That(List(destination, "worldMapCleared"), Is.EqualTo(new[] { "TL-16" }));
        }
    }
}
