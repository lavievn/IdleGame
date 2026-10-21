using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace IdleGame.Tests.SaveLoad.PlayMode
{
    [TestFixture]
#if UNITY_EDITOR
    [PrebuildSetup(typeof(SaveLoadPlayModePrebuild))]
#endif
    public sealed class SaveLoadPlayModeTests : SaveLoadPlayModeFixture
    {
        [UnityTest]
        public IEnumerator Awake_UsesConfiguredDirectory_AndLocksIt()
        {
            Files.BeforeManagerIo(Manager);
            Assert.That(Manager.gameObject.activeInHierarchy, Is.True);
            Assert.That(PlayModeRuntime.Directory(Manager), Is.EqualTo(Files.SaveDirectory));
            Assert.That(PlayModeRuntime.Instance, Is.SameAs(Manager));
            Assert.That(Files.SaveDirectoryExists(), Is.True);

            // Validate bad requests without ever activating an unconfigured
            // component: no test attempts the default/player-directory branch.
            var invalidOwner = new GameObject("W1.8a invalid configuration (never active)");
            invalidOwner.SetActive(false);
            invalidOwner.transform.SetParent(Manager.transform, false);
            Component invalid = invalidOwner.AddComponent(PlayModeRuntime.Type("SaveManager"));
            foreach (string badPath in new[] { "", "relative-save-directory" })
            {
                bool invalidRefused = false;
                try { PlayModeRuntime.Call(invalid, "ConfigureSaveDirectoryBeforeAwake", badPath); }
                catch (ArgumentException) { invalidRefused = true; }
                Assert.That(invalidRefused, Is.True);
                Assert.That(PlayModeRuntime.Directory(invalid), Is.Null);
                Assert.That(invalidOwner.activeInHierarchy, Is.False);
            }

            string other = Path.Combine(Files.Root, "must-not-be-created");
            bool refused = false;
            try { PlayModeRuntime.Call(Manager, "ConfigureSaveDirectoryBeforeAwake", other); }
            catch (InvalidOperationException) { refused = true; }
            Assert.That(refused, Is.True, "Awake must permanently lock the instance path.");
            Files.BeforeManagerIo(Manager);
            Assert.That(PlayModeRuntime.Directory(Manager), Is.EqualTo(Files.SaveDirectory));

            // A disabled component has already run Awake; disabling cannot unlock it.
            ((Behaviour)Manager).enabled = false;
            refused = false;
            try { PlayModeRuntime.Call(Manager, "ConfigureSaveDirectoryBeforeAwake", other); }
            catch (InvalidOperationException) { refused = true; }
            Assert.That(refused, Is.True);
            ((Behaviour)Manager).enabled = true;
            Assert.That(PlayModeRuntime.Directory(Manager), Is.EqualTo(Files.SaveDirectory));
            Assert.That(Files.DirectoryExistsUnderRoot(other), Is.False, "Rejected configuration must not create a directory.");
            yield return null;
            Files.BeforeManagerIo(Manager);
            Assert.That(PlayModeRuntime.Instance, Is.SameAs(Manager));
        }

        [UnityTest]
        public IEnumerator SaveLoad_RestoresCanonicalHeroEquipmentAndMap()
        {
            var source = Hero();
            PlayModeRuntime.Set(source, "isDirty", true);
            string equipment = JsonUtility.ToJson(Sword(source));
            string inventory = JsonUtility.ToJson(source);
            Assert.That((bool)ManagerCall("SaveGame", source, PlayModeRuntime.Slot("ManualSave1")), Is.True);
            Assert.That(PlayModeRuntime.Get(source, "isDirty"), Is.EqualTo(false));
            Assert.That(Files.Exists("ManualSave1.json"), Is.True);
            Assert.That(Files.Exists("ManualSave1.tmp"), Is.False);
            byte[] saved = Files.Read("ManualSave1.json");
            yield return null;

            var destination = Hero("{\"currentLevel\":1,\"currentExp\":0}");
            Assert.That((bool)ManagerCall("LoadGame", destination, PlayModeRuntime.Slot("ManualSave1")), Is.True);
            Assert.That(PlayModeRuntime.Get(destination, "currentLevel"), Is.EqualTo(7));
            Assert.That(PlayModeRuntime.Get(destination, "currentExp"), Is.EqualTo(73));
            Assert.That(PlayModeRuntime.Get(destination, "statPoints"), Is.EqualTo(2));
            Assert.That(PlayModeRuntime.Get(destination, "addedHealth"), Is.EqualTo(12));
            Assert.That(PlayModeRuntime.Get(destination, "addedDamage"), Is.EqualTo(2));
            Assert.That(PlayModeRuntime.Get(destination, "gold"), Is.EqualTo(5000000123L));
            Assert.That(List(destination, "inventory").Count, Is.EqualTo(1));
            Assert.That(PlayModeRuntime.Get(List(destination, "inventory")[0], "itemId"), Is.EqualTo("material:refinement"));
            Assert.That(PlayModeRuntime.Get(List(destination, "inventory")[0], "quantity"), Is.EqualTo(12));
            Assert.That(PlayModeRuntime.Get(destination, "equippedItemId"), Is.EqualTo("sample:health_charm"));
            Assert.That(List(destination, "equipmentItems").Count, Is.EqualTo(1));
            Assert.That(JsonUtility.ToJson(Sword(destination)), Is.EqualTo(equipment), "Keep affixes, equipped slot and forge history exactly.");
            Assert.That(PlayModeRuntime.Get(destination, "worldMapId"), Is.EqualTo("TV-20"));
            Assert.That(PlayModeRuntime.Get(destination, "worldMapWaveProgress"), Is.EqualTo(3));
            Assert.That(PlayModeRuntime.Get(destination, "worldMapLoopWave"), Is.EqualTo(4));
            Assert.That(List(destination, "worldMapVisited"), Is.EqualTo(new[] { "TV-20", "TL-16" }));
            Assert.That(List(destination, "worldMapCleared"), Is.EqualTo(new[] { "TL-16" }));
            Assert.That(Files.Read("ManualSave1.json"), Is.EqualTo(saved), "Load must not rewrite the snapshot.");
            // Saving only clears dirty state in RAM; source progression/gear stay fixed.
            PlayModeRuntime.Set(source, "isDirty", true);
            Assert.That(JsonUtility.ToJson(source), Is.EqualTo(inventory));
        }

        [UnityTest]
        public IEnumerator FutureVersions_RejectSaveAndLoad_WithoutMutatingMemoryOrFiles()
        {
            var current = Hero();
            Assert.That((bool)ManagerCall("SaveGame", current, PlayModeRuntime.Slot("ManualSave1")), Is.True);
            byte[] validFile = Files.Read("ManualSave1.json");
            Files.Write("ManualSave1.tmp", "existing pending write");
            byte[] pending = Files.Read("ManualSave1.tmp");

            // Five inputs inside ONE UnityTest: three confirmed version failures,
            // plus affix and nested checkpoint versions. No random data or test skipping.
            foreach (string kind in new[] { "inventory", "equipment", "affix", "forge", "checkpoint" })
            {
                var future = Hero();
                SetFutureVersion(future, kind);
                PlayModeRuntime.Set(future, "isDirty", true);
                string futureJson = JsonUtility.ToJson(future);
                object sourceItems = List(future, "equipmentItems");
                Assert.That((bool)ManagerCall("SaveGame", future, PlayModeRuntime.Slot("ManualSave1")), Is.False, kind);
                ManagerCall("SaveGame", "ManualSave1", futureJson); // Raw JSON cannot bypass validation.
                Assert.That(JsonUtility.ToJson(future), Is.EqualTo(futureJson), kind);
                Assert.That(PlayModeRuntime.Get(future, "isDirty"), Is.EqualTo(true), kind);
                Assert.That(List(future, "equipmentItems"), Is.SameAs(sourceItems), kind);
                Assert.That(Files.Read("ManualSave1.json"), Is.EqualTo(validFile), kind);
                Assert.That(Files.Read("ManualSave1.tmp"), Is.EqualTo(pending), kind);

                // Seed only the owned fixture slot; the runtime must reject this load.
                Files.Write("ManualSave2.json", futureJson);
                byte[] futureFile = Files.Read("ManualSave2.json");
                var destination = Hero();
                PlayModeRuntime.Set(destination, "currentLevel", 4);
                PlayModeRuntime.Set(destination, "currentExp", 11);
                PlayModeRuntime.Set(destination, "isDirty", false);
                string before = JsonUtility.ToJson(destination);
                object targetItems = List(destination, "equipmentItems");
                Assert.That((bool)ManagerCall("LoadGame", destination, PlayModeRuntime.Slot("ManualSave2")), Is.False, kind);
                Assert.That(JsonUtility.ToJson(destination), Is.EqualTo(before), kind);
                Assert.That(PlayModeRuntime.Get(destination, "isDirty"), Is.EqualTo(false), kind);
                Assert.That(List(destination, "equipmentItems"), Is.SameAs(targetItems), kind);
                Assert.That(Files.Read("ManualSave2.json"), Is.EqualTo(futureFile), kind);
                Assert.That(Files.Exists("ManualSave2.tmp"), Is.False, kind);

                PlayModeRuntime.Set(current, "isDirty", true);
                string currentBefore = JsonUtility.ToJson(current);
                Assert.That((bool)ManagerCall("SaveGame", current, PlayModeRuntime.Slot("ManualSave2")), Is.False,
                    "A current Hero must not overwrite a future-version slot: " + kind);
                Assert.That(JsonUtility.ToJson(current), Is.EqualTo(currentBefore), kind);
                Assert.That(Files.Read("ManualSave2.json"), Is.EqualTo(futureFile), kind);
                Assert.That(Files.Exists("ManualSave2.tmp"), Is.False, kind);
                yield return null;
                Files.BeforeManagerIo(Manager);
            }
        }

        private static void SetFutureVersion(object hero, string kind)
        {
            object item = Sword(hero);
            switch (kind)
            {
                case "inventory":
                    PlayModeRuntime.Set(hero, "inventoryVersion", PlayModeRuntime.Constant("EquipmentInventory", "SchemaVersion") + 1);
                    break;
                case "equipment":
                    PlayModeRuntime.Set(hero, "equipmentVersion", PlayModeRuntime.Constant("EquipmentFoundation", "Version") + 1);
                    break;
                case "affix":
                    PlayModeRuntime.Set(item, "affixSchemaVersion", PlayModeRuntime.Constant("EquipmentFoundation", "AffixSchemaVersion") + 1);
                    break;
                case "forge":
                    PlayModeRuntime.Set(item, "forgeSchemaVersion", PlayModeRuntime.Constant("EquipmentRefinement", "SchemaVersion") + 1);
                    break;
                case "checkpoint":
                    PlayModeRuntime.Set(List(item, "forgeHistory")[0], "checkpointVersion", PlayModeRuntime.Constant("EquipmentRefinement", "SchemaVersion") + 1);
                    break;
                default: throw new ArgumentException(kind);
            }
        }
    }
}
