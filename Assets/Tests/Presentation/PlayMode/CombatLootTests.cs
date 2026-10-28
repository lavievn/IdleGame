using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace IdleGame.Tests.Presentation.PlayMode
{
    // No scene loads, SaveManager instances, player saves or file I/O.
    [TestFixture]
    public sealed class CombatLootTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
        static Type Runtime(string name)
        {
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                var type=assembly.GetType(name,false);if(type!=null)return type;
            }
            throw new TypeLoadException(name);
        }
        static object Call(object target,string name,params object[] args)
        {
            var type=target as Type ?? target.GetType();
            foreach(var method in type.GetMethods(All))
                if(method.Name==name && method.GetParameters().Length==args.Length)
                    return method.Invoke(target is Type?null:target,args);
            throw new MissingMethodException(type.Name,name);
        }
        static object Get(object o,string name)
        {
            var f=o.GetType().GetField(name,All);
            return f!=null?f.GetValue(o):o.GetType().GetProperty(name,All).GetValue(o,null);
        }
        static void Set(object o,string name,object value) {o.GetType().GetField(name,All).SetValue(o,value);}
        static ScriptableObject Hero()
        {
            var h=ScriptableObject.CreateInstance(Runtime("EntityDataSO"));
            Call(h,"ApplyHeroBalance");Call(Runtime("EquipmentInventory"),"InitializeNewRun",h);
            Call(Runtime("EquipmentRuntime"),"InitializeNewRun",h);return h;
        }
        static string Complete(object service,object key,object h,bool on=true)
        {object[] a={key,h,on,null,null};return Call(service,"Complete",a).ToString();}
        static int Count(object h) {return ((IList)Get(h,"equipmentItems")).Count;}
        [TestCase(false)] [TestCase(true)]
        public void ProfileOnlyGrantsOneUnequippedCommonSwordWithoutCombatRng(bool on)
        {
            var h=Hero();var state=UnityEngine.Random.state;
            try {
                var service=Activator.CreateInstance(Runtime("CombatLootRewards"));Call(service,"BeginEncounter",h);
                var key=Call(service,"Register",1,1);Set(h,"isDirty",false);
                UnityEngine.Random.InitState(719);float expected=UnityEngine.Random.value;UnityEngine.Random.InitState(719);
                Assert.That(Call(service,"TryClaim",key,h),Is.EqualTo(true));
                Assert.That(Complete(service,key,h,on),Is.EqualTo(on?"Awarded":"NoDrop"));
                Assert.That(UnityEngine.Random.value,Is.EqualTo(expected));
                Assert.That(Count(h),Is.EqualTo(on?1:0));Assert.That(Get(h,"isDirty"),Is.EqualTo(on));
                Assert.That(Call(service,"TryClaim",key,h),Is.EqualTo(false));
                Assert.That(Complete(service,key,h,true),Is.EqualTo("Rejected"));
                if(on) {
                    var item=((IList)Get(h,"equipmentItems"))[0];
                    Assert.That(Get(item,"templateId"),Is.EqualTo("sample:iron_sword"));
                    Assert.That(Get(item,"rarity").ToString(),Is.EqualTo("Common"));
                    Assert.That(Get(item,"level"),Is.EqualTo(1));Assert.That(Get(item,"isEquipped"),Is.EqualTo(false));
                }
            } finally {UnityEngine.Random.state=state;UnityEngine.Object.DestroyImmediate(h);}
        }
        [Test]
        public void JsonSnapshotsRetainExactItemsAndInvalidateOldRunOnReplacement()
        {
            var h=Hero();var loaded=Hero();
            try {
                var service=Activator.CreateInstance(Runtime("CombatLootRewards"));Call(service,"BeginEncounter",h);
                string older=JsonUtility.ToJson(h);var key=Call(service,"Register",1,1);
                Call(service,"TryClaim",key,h);Assert.That(Complete(service,key,h),Is.EqualTo("Awarded"));
                string saved=JsonUtility.ToJson(h);JsonUtility.FromJsonOverwrite(saved,loaded);
                Call(Runtime("EquipmentInventory"),"Normalize",loaded);Call(Runtime("EquipmentRuntime"),"Normalize",loaded);
                Assert.That(Count(loaded),Is.EqualTo(1));
                Assert.That(Get(((IList)Get(loaded,"equipmentItems"))[0],"instanceId"),
                    Is.EqualTo(Get(((IList)Get(h,"equipmentItems"))[0],"instanceId")));
                Assert.That(Call(service,"TryClaim",key,loaded),Is.EqualTo(false));
                Call(service,"BeginEncounter",loaded);Assert.That(Complete(service,key,loaded),Is.EqualTo("Rejected"));
                JsonUtility.FromJsonOverwrite(older,loaded);Assert.That(Count(loaded),Is.EqualTo(0));
                Call(service,"BeginEncounter",loaded);var fresh=Call(service,"Register",1,1);
                Call(service,"TryClaim",fresh,loaded);Assert.That(Complete(service,fresh,loaded),Is.EqualTo("Awarded"));
            } finally {UnityEngine.Object.DestroyImmediate(h);UnityEngine.Object.DestroyImmediate(loaded);}
        }
        [Test]
        public void FutureJsonProbeRejectsWithoutPublishingOrInvalidatingLiveContext()
        {
            var h=Hero();var future=Hero();
            try {
                var service=Activator.CreateInstance(Runtime("CombatLootRewards"));Call(service,"BeginEncounter",h);
                var key=Call(service,"Register",1,1);string before=JsonUtility.ToJson(h);
                Set(future,"equipmentVersion",999);object[] a={JsonUtility.ToJson(future),null};
                Assert.That(Call(Runtime("SaveManager"),"TryValidateEquipmentJson",a),Is.EqualTo(false));
                Assert.That(JsonUtility.ToJson(h),Is.EqualTo(before));
                Assert.That(Call(service,"IsCurrent",key,h),Is.EqualTo(true));
            } finally {UnityEngine.Object.DestroyImmediate(h);UnityEngine.Object.DestroyImmediate(future);}
        }
        [Test]
        public void FailureIsTerminalNoRerollAndNoMutation()
        {
            var h=Hero();
            try {
                var service=Activator.CreateInstance(Runtime("CombatLootRewards"));Call(service,"BeginEncounter",h);
                Set(h,"equipmentItems",null);Set(h,"isDirty",false);var key=Call(service,"Register",1,1);
                Call(service,"TryClaim",key,h);Assert.That(Complete(service,key,h),Is.EqualTo("Failed"));
                Assert.That(Get(service,"Faulted"),Is.EqualTo(true));Assert.That(Get(h,"isDirty"),Is.EqualTo(false));
                Assert.That(Complete(service,key,h),Is.EqualTo("Rejected"));
                Call(service,"BeginEncounter",h);key=Call(service,"Register",2,1);Call(service,"TryClaim",key,h);
                Assert.That(Complete(service,key,h),Is.EqualTo("NoDrop"));
            } finally {UnityEngine.Object.DestroyImmediate(h);}
        }
        [Test]
        public void NativePoolActivationChangesSpawnAndOldTokenCannotReward()
        {
            foreach(string name in new[]{"GameManager","CombatManager","EnvironmentManager","MonsterController","SaveManager"})
                Assert.That(UnityEngine.Object.FindObjectsByType(Runtime(name),FindObjectsInactive.Include,FindObjectsSortMode.None),Is.Empty,
                    "Use an empty test scene; do not touch active gameplay.");
            var h=Hero();var state=UnityEngine.Random.state;
            var root=new GameObject("Owned loot pool life",typeof(RectTransform));root.SetActive(false);
            try {
                var monster=root.AddComponent(Runtime("MonsterController"));root.SetActive(true);
                int spawn=(int)Get(monster,"SpawnVersion");
                var service=Activator.CreateInstance(Runtime("CombatLootRewards"));Call(service,"BeginEncounter",h);
                var old=Call(service,"Register",1,spawn);Call(service,"TryClaim",old,h);Complete(service,old,h);
                root.SetActive(false);Call(service,"Invalidate");root.SetActive(true);
                Assert.That((int)Get(monster,"SpawnVersion"),Is.GreaterThan(spawn));
                Call(service,"BeginEncounter",h);var fresh=Call(service,"Register",2,Get(monster,"SpawnVersion"));
                Assert.That(Complete(service,old,h),Is.EqualTo("Rejected"));
                Assert.That(Call(service,"TryClaim",fresh,h),Is.EqualTo(true));Complete(service,fresh,h);
                Assert.That(Count(h),Is.EqualTo(2));Assert.That(Get(service,"ReceiptCount"),Is.EqualTo(1));
            } finally {UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(h);UnityEngine.Random.state=state;}
        }
    }
}
