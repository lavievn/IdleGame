using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace IdleGame.Tests.SaveLoad.PlayMode
{
    // Resolve the real Assembly-CSharp types; an asmdef cannot reference the
    // predefined runtime assembly. No runtime serializer/normalizer is copied.
    internal static class PlayModeRuntime
    {
        internal static Type Type(string name) => System.Type.GetType(name + ", Assembly-CSharp", true);
        internal static object Get(object value, string field) => value.GetType().GetField(field).GetValue(value);
        internal static void Set(object value, string field, object data) => value.GetType().GetField(field).SetValue(value, data);
        internal static int Constant(string type, string field) => (int)Type(type).GetField(field).GetRawConstantValue();
        internal static object Slot(string name) => Enum.Parse(Type("SaveSlot"), name);
        internal static object Call(object value, string method, params object[] arguments)
        {
            var types = Array.ConvertAll(arguments, argument => argument.GetType());
            var member = value.GetType().GetMethod(method, types);
            if (member == null) throw new MissingMethodException(value.GetType().Name, method);
            try { return member.Invoke(value, arguments); }
            catch (TargetInvocationException error) when (error.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }
        internal static string Directory(Component manager) => (string)manager.GetType().GetProperty("SaveDirectory").GetValue(manager);
        internal static UnityEngine.Object Instance => (UnityEngine.Object)Type("SaveManager").GetProperty("Instance").GetValue(null);
    }

    internal sealed class OwnedPlayModeSaves
    {
        private readonly string owner = Guid.NewGuid().ToString("N");
        private readonly HashSet<string> createdFiles = new HashSet<string>(StringComparer.Ordinal);
        private bool createdRoot, createdMarker, createdSaveDirectory;
        internal string Root { get; }
        internal string SaveDirectory { get; }
        private string Marker => Path.Combine(Root, ".fixture-owner");

        internal OwnedPlayModeSaves()
        {
            Root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "IdleGame-SaveLoad-PlayMode", owner));
            SaveDirectory = Path.Combine(Root, "Saves");
        }

        private static bool Within(string path, string parent)
        {
            string prefix = parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(path, prefix, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(prefix + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        // Inspect ONLY our temp path/ancestors, never files or metadata in player Saves.
        // Reject links/junctions rather than following them, including during cleanup.
        private void CheckPath(string path)
        {
            if (string.IsNullOrEmpty(Application.persistentDataPath))
                throw new InvalidOperationException("Cannot verify the player-save location.");
            string full = Path.GetFullPath(path);
            string forbidden = Path.GetFullPath(Path.Combine(Application.persistentDataPath, "Saves"));
            string temporary = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "IdleGame-SaveLoad-PlayMode"));
            if (!Within(Root, temporary) || !Within(full, Root) ||
                Within(Root, forbidden) || Within(forbidden, Root))
                throw new InvalidOperationException("Unsafe or overlapping fixture directory.");
            var ancestors = new Stack<string>();
            string current = full;
            while (!string.IsNullOrEmpty(current))
            {
                ancestors.Push(current);
                current = Path.GetDirectoryName(current);
            }
            // Parent BEFORE child: never stat a child through an unchecked link.
            while (ancestors.Count > 0)
            {
                current = ancestors.Pop();
                try
                {
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                        throw new InvalidOperationException("Links/junctions are not permitted in fixture paths: " + current);
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
            }
        }

        internal void Create()
        {
            CheckPath(Root);
            if (System.IO.Directory.Exists(Root))
                throw new InvalidOperationException("Refusing to reuse an existing fixture directory.");
            CheckPath(Root);
            System.IO.Directory.CreateDirectory(Root);
            createdRoot = true;
            CheckPath(Marker);
            File.WriteAllText(Marker, owner);
            createdMarker = true;
            AssertOwned();
        }

        internal void AssertOwned()
        {
            CheckPath(Marker);
            CheckPath(SaveDirectory);
            if (!createdRoot || !createdMarker || !File.Exists(Marker) || File.ReadAllText(Marker) != owner)
                throw new InvalidOperationException("Missing or incorrect fixture ownership marker.");
        }

        internal void BeforeManagerIo(Component manager)
        {
            CheckInventory();
            if (!string.Equals(PlayModeRuntime.Directory(manager), SaveDirectory, StringComparison.Ordinal))
                throw new InvalidOperationException("SaveManager no longer points at this fixture.");
            // Include existing targets in the link check before runtime reads,
            // writes, replaces or moves a slot. Tests use only these fixed slots.
            foreach (string slot in new[] { "AutoSave", "ManualSave1", "ManualSave2" })
            {
                CheckPath(Path.Combine(SaveDirectory, slot + ".json"));
                CheckPath(Path.Combine(SaveDirectory, slot + ".tmp"));
            }
        }

        internal void RecordAwakeDirectory()
        {
            AssertOwned();
            // SetUp proved this path absent immediately before activating OUR
            // configured SaveManager. Awake is allowed to create only this child.
            if (!System.IO.Directory.Exists(SaveDirectory) ||
                System.IO.Directory.GetFileSystemEntries(SaveDirectory).Length != 0)
                throw new InvalidOperationException("Awake did not create the expected empty fixture save directory.");
            createdSaveDirectory = true;
            CheckInventory();
        }

        internal void RecordSuccessfulSave(string slot)
        {
            // Called only after typed SaveGame returned true. A failed/raw call
            // does not grant ownership of any newly appearing file or leftover tmp.
            string name = slot + ".json";
            string path = FilePath(name);
            if (!File.Exists(path) || (File.GetAttributes(path) & FileAttributes.Directory) != 0)
                throw new InvalidOperationException("A successful save did not produce its fixed snapshot file.");
            createdFiles.Add(name);
            CheckInventory();
        }

        private string FilePath(string name)
        {
            AssertOwned();
            if (name != "ManualSave1.json" && name != "ManualSave2.json" &&
                name != "ManualSave1.tmp" && name != "ManualSave2.tmp" && name != "AutoSave.json" && name != "AutoSave.tmp")
                throw new ArgumentException("Only fixture slot files are allowed.", nameof(name));
            string path = Path.Combine(SaveDirectory, name);
            CheckPath(path);
            return path;
        }

        internal byte[] Read(string name)
        {
            CheckInventory();
            if (!createdFiles.Contains(name))
                throw new InvalidOperationException("The file was not created by this fixture.");
            return File.ReadAllBytes(FilePath(name));
        }

        internal void Write(string name, string data)
        {
            CheckInventory();
            string path = FilePath(name);
            File.WriteAllText(path, data);
            createdFiles.Add(name); // Failed/partial writes stay unowned for inspection.
        }

        internal bool Exists(string name)
        {
            CheckInventory();
            return File.Exists(FilePath(name));
        }
        internal bool SaveDirectoryExists()
        {
            AssertOwned();
            return System.IO.Directory.Exists(SaveDirectory);
        }

        internal bool DirectoryExistsUnderRoot(string path)
        {
            AssertOwned();
            CheckPath(path);
            return System.IO.Directory.Exists(path);
        }

        internal void CheckInventory()
        {
            AssertOwned();
            // Only two levels, no recursion. An unexpected file, subdirectory or
            // fixed-name file not in our creation ledger aborts before deletion.
            foreach (string entry in System.IO.Directory.GetFileSystemEntries(Root))
            {
                CheckPath(entry);
                bool directory = (File.GetAttributes(entry) & FileAttributes.Directory) != 0;
                if (string.Equals(entry, Marker, StringComparison.Ordinal) && !directory) continue;
                if (string.Equals(entry, SaveDirectory, StringComparison.Ordinal) && directory && createdSaveDirectory) continue;
                throw new InvalidOperationException("Unexpected fixture entry; leaving it intact: " + entry);
            }
            if (!createdSaveDirectory) return;
            if (!System.IO.Directory.Exists(SaveDirectory))
                throw new InvalidOperationException("The owned save directory disappeared; leaving the root intact.");
            foreach (string entry in System.IO.Directory.GetFileSystemEntries(SaveDirectory))
            {
                CheckPath(entry);
                string name = Path.GetFileName(entry);
                if ((File.GetAttributes(entry) & FileAttributes.Directory) != 0 || !createdFiles.Contains(name))
                    throw new InvalidOperationException("Unmanaged fixture entry; leaving it intact: " + entry);
                // Independently enforce the six fixed filenames, even for ledger entries.
                if (!string.Equals(FilePath(name), entry, StringComparison.Ordinal))
                    throw new InvalidOperationException("A managed file escaped its fixed path.");
            }
        }

        internal void DeleteCreatedFilesAndEmptyDirectories()
        {
            // Validate the complete, flat inventory BEFORE deleting anything.
            // Never adopt or delete unexpected entries, and never recurse.
            CheckInventory();
            var names = new List<string>(createdFiles);
            names.Sort(StringComparer.Ordinal);
            foreach (string name in names)
            {
                CheckInventory();
                string path = FilePath(name);
                if (File.Exists(path)) File.Delete(path);
                createdFiles.Remove(name);
            }
            CheckInventory();
            if (createdSaveDirectory)
            {
                CheckPath(SaveDirectory);
                if (System.IO.Directory.GetFileSystemEntries(SaveDirectory).Length != 0)
                    throw new InvalidOperationException("The save directory is not empty; preserving it.");
                System.IO.Directory.Delete(SaveDirectory, false);
                createdSaveDirectory = false;
            }
            AssertOwned();
            string[] remaining = System.IO.Directory.GetFileSystemEntries(Root);
            if (remaining.Length != 1 || !string.Equals(remaining[0], Marker, StringComparison.Ordinal))
                throw new InvalidOperationException("The root has unexpected entries; preserving marker and root.");
            CheckPath(Marker);
            File.Delete(Marker);
            createdMarker = false;
            CheckPath(Root);
            // Non-recursive: a newly appearing entry makes deletion fail.
            System.IO.Directory.Delete(Root, false);
            createdRoot = false;
        }
    }

    public abstract class SaveLoadPlayModeFixture
    {
        private readonly List<ScriptableObject> entities = new List<ScriptableObject>();
        private Scene fixtureScene, previousScene;
        private GameObject managerOwner;
        internal OwnedPlayModeSaves Files { get; private set; }
        protected Component Manager { get; private set; }

        public static void RequireNoSaveOrGameManager()
        {
            foreach (string name in new[] { "SaveManager", "GameManager" })
                foreach (UnityEngine.Object value in Resources.FindObjectsOfTypeAll(PlayModeRuntime.Type(name)))
                {
                    var component = value as Component;
                    if (component != null && component.gameObject.scene.IsValid())
                        throw new InvalidOperationException("An existing " + name + " prevents an isolated test launch.");
                }
        }

        private static bool IsTestRunnerComponent(Component component)
        {
            string assembly = component.GetType().Assembly.GetName().Name;
            return assembly == "UnityEngine.TestRunner" || assembly == "UnityEditor.TestRunner";
        }

        private static void RequireCleanTestStart()
        {
            RequireNoSaveOrGameManager();
            if (PlayModeRuntime.Instance != null || SceneManager.sceneCount < 1)
                throw new InvalidOperationException("An existing singleton or missing scene prevents isolated fixture I/O.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || !string.IsNullOrEmpty(scene.path))
                    throw new InvalidOperationException("A saved/unloaded scene prevents isolated fixture I/O.");
                // The only non-empty roots allowed belong to Unity's Test Runner.
                // A name such as 'TestRunner' alone is NOT accepted as evidence.
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    bool runner = false;
                    foreach (Component component in root.GetComponentsInChildren<Component>(true))
                    {
                        if (component == null) throw new InvalidOperationException("A missing script prevents fixture I/O.");
                        if (component is Transform) continue;
                        if (!IsTestRunnerComponent(component))
                            throw new InvalidOperationException("A non-TestRunner scene component prevents fixture I/O.");
                        runner = true;
                    }
                    if (!runner) throw new InvalidOperationException("An unexpected scene root prevents fixture I/O.");
                }
            }
            // Also inspect DontDestroyOnLoad behaviours, which sceneCount omits.
            foreach (UnityEngine.Object value in Resources.FindObjectsOfTypeAll(typeof(MonoBehaviour)))
            {
                var component = value as MonoBehaviour;
                if (component != null && component.gameObject.scene.IsValid() && !IsTestRunnerComponent(component))
                    throw new InvalidOperationException("An existing runtime behaviour prevents fixture I/O.");
            }
        }

        [UnitySetUp]
        public IEnumerator SetUpIsolatedSaveManager()
        {
#if !UNITY_EDITOR
            throw new InvalidOperationException("W1.8a supports Editor Play Mode only; player tests are not approved.");
#endif
            Assert.That(Application.isPlaying, Is.True);
            RequireCleanTestStart(); // Direct, fresh validation BEFORE any fixture I/O.
            previousScene = SceneManager.GetActiveScene();
            fixtureScene = SceneManager.CreateScene("W1.8a-" + Guid.NewGuid().ToString("N"));
            Assert.That(fixtureScene.rootCount, Is.EqualTo(0));
            Assert.That(SceneManager.SetActiveScene(fixtureScene), Is.True);

            Files = new OwnedPlayModeSaves();
            Files.Create();
            managerOwner = new GameObject("W1.8a isolated SaveManager");
            managerOwner.SetActive(false); // BEFORE AddComponent: Awake cannot run yet.
            SceneManager.MoveGameObjectToScene(managerOwner, fixtureScene);
            Manager = managerOwner.AddComponent(PlayModeRuntime.Type("SaveManager"));
            Assert.That(PlayModeRuntime.Instance == null, Is.True);
            Assert.That(PlayModeRuntime.Directory(Manager), Is.Null);
            Assert.That(Files.SaveDirectoryExists(), Is.False);
            Files.AssertOwned();
            PlayModeRuntime.Call(Manager, "ConfigureSaveDirectoryBeforeAwake", Files.SaveDirectory);
            Assert.That(Files.SaveDirectoryExists(), Is.False, "Configuration must not create directories.");
            Files.BeforeManagerIo(Manager); // Validate both path and marker BEFORE Awake I/O.
            managerOwner.SetActive(true);
            Assert.That(PlayModeRuntime.Instance, Is.SameAs(Manager));
            Files.RecordAwakeDirectory();
            Files.BeforeManagerIo(Manager);
            Assert.That(Files.SaveDirectoryExists(), Is.True);
            yield return null; // Real Unity frame with the configured, active component.
        }

        protected object ManagerCall(string method, params object[] arguments)
        {
            if ((method != "SaveGame" && method != "LoadGame") || arguments.Length != 2)
                throw new InvalidOperationException("Only approved two-argument Save/Load calls are allowed.");
            string slot = arguments[0] is string ? (string)arguments[0] : arguments[1].ToString();
            if (slot != "AutoSave" && slot != "ManualSave1" && slot != "ManualSave2")
                throw new InvalidOperationException("Only fixed fixture slots are allowed.");
            Files.BeforeManagerIo(Manager);
            object result = PlayModeRuntime.Call(Manager, method, arguments);
            if (method == "SaveGame" && !(arguments[0] is string) && result is bool && (bool)result)
                Files.RecordSuccessfulSave(slot);
            else
                Files.CheckInventory(); // Failed/raw calls cannot grant file ownership.
            return result;
        }

        protected ScriptableObject Hero(string json = SnapshotJson)
        {
            var hero = ScriptableObject.CreateInstance(PlayModeRuntime.Type("EntityDataSO"));
            hero.hideFlags = HideFlags.HideAndDontSave;
            entities.Add(hero);
            JsonUtility.FromJsonOverwrite(json, hero);
            return hero;
        }

        protected static IList List(object value, string field) => (IList)PlayModeRuntime.Get(value, field);
        protected static object Sword(object hero) => List(hero, "equipmentItems")[0];

        [UnityTearDown]
        public IEnumerator TearDownIsolatedSaveManager()
        {
            // Awake used DontDestroyOnLoad, so explicitly destroy before unloading
            // the otherwise empty fixture scene. No GameManager/quit autosave exists.
            if (managerOwner != null) UnityEngine.Object.Destroy(managerOwner);
            foreach (var hero in entities) if (hero != null) UnityEngine.Object.Destroy(hero);
            entities.Clear();
            yield return null;
            Manager = null;
            managerOwner = null;
            if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
            if (fixtureScene.IsValid() && fixtureScene.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(fixtureScene);
                if (unload != null) yield return unload;
            }
            if (Files != null) Files.DeleteCreatedFilesAndEmptyDirectories();
            Files = null;
        }

        // Independent, fixed authoring data; no gameplay assets or random rolls.
        protected const string SnapshotJson = @"{
  ""entityName"":""W1.8a Hero"", ""currentLevel"":7, ""currentExp"":73, ""expToNextLevel"":232,
  ""baseHealth"":496, ""baseDamage"":16, ""baseAttackSpeed"":1.03,
  ""race"":0, ""gender"":1, ""spiritRoots"":[3], ""rootTiers"":[4], ""rootWeights"":[1.0],
  ""statPoints"":2, ""addedHealth"":12, ""addedDamage"":2, ""savedAttackMode"":1,
  ""balanceVersion"":1, ""originHealth"":400, ""meleeGrowthThroughLevel"":7, ""meleeGrowthBonus"":450,
  ""combatStatsVersion"":1, ""characterTags"":[""origin:test""],
  ""combatStatModifiers"":[{""sourceId"":""trait:origin:test"",""stat"":1,""flat"":3}],
  ""inventoryVersion"":1, ""inventory"":[{""itemId"":""material:refinement"",""quantity"":12}],
  ""equippedItemId"":""sample:health_charm"", ""testItemsGranted"":true,
  ""equipmentVersion"":3, ""demoSwordGranted"":true, ""gold"":5000000123,
  ""professionProgress"":[{""profession"":0,""level"":3,""experience"":27}],
  ""learnedRecipes"":[{""recipeId"":""forge:iron"",""learnedRank"":2}],
  ""learnedSupportSkills"":[{""skillId"":""buff:armor"",""level"":3}],
  ""equipmentItems"": [{
    ""instanceId"":""w18a-sword-001"", ""templateId"":""sample:iron_sword"",
    ""level"":21, ""rarity"":1, ""refineLevel"":4,
    ""affixSchemaVersion"":1, ""itemElement"":0, ""potentialRarity"":5, ""affixSlotCount"":2,
    ""naturalRefineLimit"":15, ""highestRefineLevel"":6, ""hasBeenRefined"":true,
    ""forgeSchemaVersion"":2, ""forgeStageProgress"":2, ""forgeStageBasePercent"":20,
    ""forgeStageStartRarity"":1, ""forgeStageTargetRarity"":2, ""specialForgeCount"":2,
    ""powerBaseLevel"":21, ""powerBaseRefine"":4, ""powerBaseRarity"":1,
    ""forgeHistory"":[{""checkpointVersion"":2,""previousRarity"":0,""stageStart"":0,
      ""stageTarget"":1,""stageProgress"":3,""stageBasePercent"":10,""naturalLimit"":15}],
    ""mainStats"":[{""stat"":1,""flat"":10,""requiredTags"":[""stance:melee""]}],
    ""extraStats"":[
      {""stat"":8,""flat"":0.05,""revealed"":false,""requiredItemLevel"":1,
       ""requiredRefineLevel"":0,""requiredRarity"":2,""requiredTags"":[""stance:melee""]},
      {""stat"":1,""flat"":4,""revealed"":true,""requiredItemLevel"":1,
       ""requiredRefineLevel"":0,""requiredRarity"":0,""requiredTags"":[""stance:magic""]}],
    ""isEquipped"":true, ""equippedSlot"":0}],
  ""mapProgressVersion"":1, ""mapNumber"":1, ""mapVisits"":6, ""completedWavesInMap"":3, ""difficulty"":0,
  ""worldMapVersion"":1, ""worldMapId"":""TV-20"", ""worldMapWaveProgress"":3, ""worldMapLoopWave"":4,
  ""worldMapVisited"":[""TV-20"",""TL-16""], ""worldMapCleared"":[""TL-16""], ""isDirty"":false
}";
    }
}
