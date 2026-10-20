using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEngine;

namespace IdleGame.Tests.SaveLoad
{
    // Runtime is currently Assembly-CSharp. An asmdef cannot reference that
    // predefined assembly; resolve its real types without copying runtime code.
    internal static class RuntimeData
    {
        public static Type Type(string name)
        {
            return System.Type.GetType(name + ", Assembly-CSharp", true);
        }

        public static T Get<T>(object value, string field)
        {
            var member = value.GetType().GetField(field, BindingFlags.Public | BindingFlags.Instance);
            if (member == null) throw new MissingFieldException(value.GetType().FullName, field);
            return (T)member.GetValue(value);
        }

        public static void Set(object value, string field, object fieldValue)
        {
            var member = value.GetType().GetField(field, BindingFlags.Public | BindingFlags.Instance);
            if (member == null) throw new MissingFieldException(value.GetType().FullName, field);
            if (member.FieldType.IsEnum && fieldValue is int)
                fieldValue = Enum.ToObject(member.FieldType, fieldValue);
            member.SetValue(value, fieldValue);
        }

        public static int Constant(string type, string field)
        {
            return (int)Type(type).GetField(field, BindingFlags.Public | BindingFlags.Static)
                .GetRawConstantValue();
        }

        public static object Static(string type, string method, params object[] arguments)
        {
            return Invoke(Type(type), null, method, arguments);
        }

        public static object Call(object value, string method, params object[] arguments)
        {
            return Invoke(value.GetType(), value, method, arguments);
        }

        private static object Invoke(Type type, object target, string name, object[] arguments)
        {
            var parameterTypes = Array.ConvertAll(arguments, argument => argument.GetType());
            var method = type.GetMethod(name, BindingFlags.Public |
                (target == null ? BindingFlags.Static : BindingFlags.Instance),
                null, parameterTypes, null);
            if (method == null) throw new MissingMethodException(type.FullName, name);
            try { return method.Invoke(target, arguments); }
            catch (TargetInvocationException error) when (error.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }
    }

    internal sealed class TemporarySaveDirectory : IDisposable
    {
        private readonly string owner = Guid.NewGuid().ToString("N");
        public string DirectoryPath { get; }

        public TemporarySaveDirectory()
        {
            DirectoryPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(),
                "IdleGame-Unity-SaveLoad", owner));
            CheckLocation();
            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllText(Path.Combine(DirectoryPath, ".fixture-owner"), owner);
        }

        private void CheckLocation()
        {
            if (string.IsNullOrEmpty(Application.persistentDataPath))
                throw new InvalidOperationException("Cannot verify the player-save location.");
            string playerSaves = Path.GetFullPath(Path.Combine(Application.persistentDataPath, "Saves"));
            string testRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "IdleGame-Unity-SaveLoad"));
            if (!IsWithin(DirectoryPath, testRoot) || IsWithin(DirectoryPath, playerSaves) ||
                IsWithin(playerSaves, DirectoryPath))
                throw new InvalidOperationException("The fixture directory overlaps player saves.");
        }

        private static bool IsWithin(string path, string parent)
        {
            string prefix = parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(path, prefix, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(prefix + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        public string WriteSnapshot(string json)
        {
            CheckLocation();
            string path = Path.Combine(DirectoryPath, "ManualSave1.json");
            File.WriteAllText(path, json);
            return path;
        }

        public void Dispose()
        {
            CheckLocation();
            if (!Directory.Exists(DirectoryPath)) return;
            string marker = Path.Combine(DirectoryPath, ".fixture-owner");
            if (!File.Exists(marker) || File.ReadAllText(marker) != owner)
                throw new InvalidOperationException("Refusing to delete an unowned directory.");
            Directory.Delete(DirectoryPath, true);
        }
    }

    public abstract class SaveLoadFixture
    {
        private readonly List<ScriptableObject> entities = new List<ScriptableObject>();
        private object previousHeroProfile, previousMonsterProfile;
        private TemporarySaveDirectory temporary;
        protected string TemporaryDirectory => temporary.DirectoryPath;

        // Fixed authoring data, not output generated by the methods under test.
        // No random item generator, Scene, MonoBehaviour or SaveManager is used.
        protected const string SnapshotJson = @"{
  ""entityName"": ""Kiểm thử — Hỏa Linh"",
  ""currentLevel"": 7, ""currentExp"": 73, ""expToNextLevel"": 232,
  ""baseHealth"": 496, ""baseDamage"": 16, ""baseAttackSpeed"": 1.03,
  ""race"": 0, ""gender"": 1, ""spiritRoots"": [3],
  ""rootTiers"": [4], ""rootWeights"": [1.0],
  ""statPoints"": 2, ""addedHealth"": 12, ""addedDamage"": 2,
  ""savedAttackMode"": 1,
  ""balanceVersion"": 1, ""originHealth"": 400,
  ""meleeGrowthThroughLevel"": 7, ""meleeGrowthBonus"": 450,
  ""combatStatsVersion"": 1, ""characterTags"": [""origin:test""],
  ""combatStatModifiers"": [
    {""sourceId"":""trait:origin:test"",""stat"":1,""flat"":3},
    {""sourceId"":""equipment:primary-trinket"",""stat"":0,""flat"":999},
    {""sourceId"":""equipment:instance:stale:m:0"",""stat"":1,""flat"":999}
  ],
  ""inventoryVersion"": 1,
  ""inventory"": [{""itemId"":""material:refinement"",""quantity"":12}],
  ""equippedItemId"": ""sample:health_charm"", ""testItemsGranted"": true,
  ""equipmentVersion"": 3, ""demoSwordGranted"": true, ""gold"": 5000000123,
  ""professionProgress"": [{""profession"":0,""level"":3,""experience"":27}],
  ""learnedRecipes"": [{""recipeId"":""forge:iron"",""learnedRank"":2}],
  ""learnedSupportSkills"": [{""skillId"":""buff:armor"",""level"":3}],
  ""equipmentItems"": [{
    ""instanceId"":""audit-sword-001"", ""templateId"":""sample:iron_sword"",
    ""level"":21, ""rarity"":1, ""refineLevel"":4,
    ""affixSchemaVersion"":1, ""itemElement"":0, ""potentialRarity"":5,
    ""affixSlotCount"":2, ""naturalRefineLimit"":15,
    ""highestRefineLevel"":6, ""hasBeenRefined"":true,
    ""forgeSchemaVersion"":2, ""forgeStageProgress"":2, ""forgeStageBasePercent"":20,
    ""forgeStageStartRarity"":1, ""forgeStageTargetRarity"":2, ""specialForgeCount"":2,
    ""powerBaseLevel"":21, ""powerBaseRefine"":4, ""powerBaseRarity"":1,
    ""forgeHistory"": [{""checkpointVersion"":2,""previousRarity"":0,
      ""stageStart"":0,""stageTarget"":1,""stageProgress"":3,
      ""stageBasePercent"":10,""naturalLimit"":15}],
    ""mainStats"": [{""stat"":1,""flat"":10,""requiredTags"":[""stance:melee""]}],
    ""extraStats"": [
      {""stat"":8,""flat"":0.05,""revealed"":false,""requiredItemLevel"":1,
       ""requiredRefineLevel"":0,""requiredRarity"":2,""requiredTags"":[""stance:melee""]},
      {""stat"":1,""flat"":4,""revealed"":true,""requiredItemLevel"":1,
       ""requiredRefineLevel"":0,""requiredRarity"":0,""requiredTags"":[""stance:magic""]}
    ],
    ""isEquipped"":true, ""equippedSlot"":0
  }],
  ""mapProgressVersion"":1, ""mapNumber"":1, ""mapVisits"":6,
  ""completedWavesInMap"":3, ""difficulty"":0,
  ""worldMapVersion"":1, ""worldMapId"":""TV-20"",
  ""worldMapWaveProgress"":3, ""worldMapLoopWave"":4,
  ""worldMapVisited"":[""TV-20"",""TL-16""], ""worldMapCleared"":[""TL-16""],
  ""isDirty"":false
}";

        [SetUp]
        public void SetUpSaveData()
        {
            var balance = RuntimeData.Type("CombatBalance");
            previousHeroProfile = balance.GetProperty("HeroDev").GetValue(null);
            previousMonsterProfile = balance.GetProperty("MonsterDev").GetValue(null);
            balance.GetMethod("ResetDevProfiles").Invoke(null, null);
            try { temporary = new TemporarySaveDirectory(); }
            catch { RestoreProfiles(); throw; }
        }

        [TearDown]
        public void TearDownSaveData()
        {
            try
            {
                foreach (var entity in entities)
                    if (entity != null) UnityEngine.Object.DestroyImmediate(entity);
                entities.Clear();
            }
            finally
            {
                try { RestoreProfiles(); }
                finally { temporary?.Dispose(); temporary = null; }
            }
        }

        private void RestoreProfiles()
        {
            var set = RuntimeData.Type("CombatBalance").GetMethod("SetDevProfile");
            set.Invoke(null, new[] { (object)false, previousHeroProfile });
            set.Invoke(null, new[] { (object)true, previousMonsterProfile });
        }

        protected ScriptableObject NewEntity()
        {
            var entity = ScriptableObject.CreateInstance(RuntimeData.Type("EntityDataSO"));
            entity.hideFlags = HideFlags.HideAndDontSave;
            entities.Add(entity);
            return entity;
        }

        protected ScriptableObject Deserialize(string json = SnapshotJson)
        {
            var entity = NewEntity();
            JsonUtility.FromJsonOverwrite(json, entity);
            return entity;
        }

        protected string WriteSnapshot(string json) => temporary.WriteSnapshot(json);
        protected static IList List(object value, string field) => RuntimeData.Get<IList>(value, field);
        protected static object Sword(object hero) => List(hero, "equipmentItems")[0];

        // Only data normalization, not SaveManager.LoadGame validation or UI deployment.
        protected static void NormalizeEquipment(object hero)
        {
            RuntimeData.Static("EquipmentInventory", "Normalize", hero);
            RuntimeData.Static("EquipmentRuntime", "Normalize", hero);
        }
    }
}
