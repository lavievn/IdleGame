using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace IdleGame.Tests.Presentation.PlayMode
{
    // Native JsonUtility + pure runtime services. No scene loads, SaveManager or save I/O.
    [TestFixture]
    public sealed class WeaponBasicAttackTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
        static Type Runtime(string name)
        {
            foreach(var a in AppDomain.CurrentDomain.GetAssemblies()) {
                var t=a.GetType(name,false);if(t!=null)return t;
            }
            throw new TypeLoadException(name);
        }
        static object Call(object target,string name,params object[] args)
        {
            var type=target as Type ?? target.GetType();
            foreach(var m in type.GetMethods(All))
                if(m.Name==name && m.GetParameters().Length==args.Length)
                    return m.Invoke(target is Type?null:target,args);
            throw new MissingMethodException(type.Name,name);
        }
        static object Get(object o,string name) {return o.GetType().GetField(name,All).GetValue(o);}
        static void Set(object o,string name,object value) {o.GetType().GetField(name,All).SetValue(o,value);}
        static object Slot {get {return Enum.Parse(Runtime("EquipmentSlot"),"Weapon");}}
        static object Stat(string name) {return Enum.Parse(Runtime("CombatStatId"),name);}
        static object Mode(string name) {return Enum.Parse(Runtime("TuTienCore.AttackMode"),name);}
        static ScriptableObject Hero()
        {
            var h=ScriptableObject.CreateInstance(Runtime("EntityDataSO"));Call(h,"ApplyHeroBalance");
            Call(Runtime("EquipmentInventory"),"InitializeNewRun",h);Call(Runtime("EquipmentRuntime"),"InitializeNewRun",h);return h;
        }
        static object Add(ScriptableObject hero,string family)
        {
            var item=Call(Runtime("EquipmentFoundation"),"CreateDemo"+family);
            Assert.That(Call(Runtime("EquipmentRuntime"),"AddInstance",hero,item),Is.True);return item;
        }
        static void Equip(ScriptableObject h,object item)
        {Assert.That(Call(Runtime("EquipmentRuntime"),"Equip",h,Get(item,"instanceId"),Slot),Is.True);}
        [TestCase("Sword","Melee",20,0)]
        [TestCase("Bow","RangedPhysical",20,0)]
        [TestCase("Staff","RangedMagic",10,10)]
        public void TemplateFamilyResolvesBothAttackSources(string family,string mode,int physical,int magical)
        {
            var h=Hero();try {
                var item=Add(h,family);Equip(h,item);
                Assert.That(Call(Runtime("WeaponBasicAttack"),"Resolve",h).ToString(),Is.EqualTo(mode));
                Assert.That(Call(Runtime("WeaponBasicAttack"),"Physical",h),Is.EqualTo(physical));
                Assert.That(Call(Runtime("WeaponBasicAttack"),"Magical",h),Is.EqualTo(magical));
                Assert.That(Call(Runtime("EquipmentRuntime"),"Unequip",h,Slot),Is.True);
                Assert.That(Call(Runtime("WeaponBasicAttack"),"Resolve",h).ToString(),Is.EqualTo("Melee"));
                Assert.That(Call(Runtime("WeaponBasicAttack"),"Magical",h),Is.EqualTo(0));
            } finally {UnityEngine.Object.DestroyImmediate(h);}
        }
        [Test]
        public void NativeJsonRetainsWeaponAndMagicalStatWithoutCachedModeAuthority()
        {
            var h=Hero();var loaded=Hero();try {
                var staff=Add(h,"Staff");Equip(h,staff);Set(h,"savedAttackMode",Mode("Melee"));
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(h),loaded);
                Call(Runtime("EquipmentInventory"),"Normalize",loaded);Call(Runtime("EquipmentRuntime"),"Normalize",loaded);
                Assert.That(Call(Runtime("WeaponBasicAttack"),"Resolve",loaded).ToString(),Is.EqualTo("RangedMagic"));
                Assert.That(Call(Runtime("WeaponBasicAttack"),"Identity",loaded),Is.EqualTo(Get(staff,"instanceId")));
                Assert.That(Call(Runtime("WeaponBasicAttack"),"Magical",loaded),Is.EqualTo(10));
                Assert.That(Get(loaded,"savedAttackMode").ToString(),Is.EqualTo("Melee"));
                var items=(IList)Get(loaded,"equipmentItems");
                Assert.That(Get(((IList)Get(items[0],"mainStats"))[0],"stat"),Is.EqualTo(Stat("MagicalAttack")));
            } finally {UnityEngine.Object.DestroyImmediate(h);UnityEngine.Object.DestroyImmediate(loaded);}
        }
        [Test]
        public void NativeComparisonIsPureAndMatchesTheActualStaffSwap()
        {
            var h=Hero();try {
                Equip(h,Add(h,"Sword"));var staff=Add(h,"Staff");Set(h,"isDirty",false);
                string before=JsonUtility.ToJson(h);object[] args={h,Get(staff,"instanceId"),Slot,Mode("RangedPhysical"),null};
                Assert.That(Call(Runtime("EquipmentStatPreview"),"TryEquip",args),Is.True);
                var c=args[4];Assert.That(Get(c,"currentMode").ToString(),Is.EqualTo("Melee"));
                Assert.That(Get(c,"mode").ToString(),Is.EqualTo("RangedMagic"));
                Assert.That(JsonUtility.ToJson(h),Is.EqualTo(before));Assert.That(Get(h,"isDirty"),Is.False);
                var element=Enum.Parse(Runtime("TuTienCore.ElementType"),"Vo");
                var physical=Call(c,"Find",Stat("Attack"),element);var magical=Call(c,"Find",Stat("MagicalAttack"),element);
                Assert.That(Get(physical,"current"),Is.EqualTo(20f));Assert.That(Get(magical,"current"),Is.EqualTo(0f));
                Equip(h,staff);
                Assert.That(Convert.ToSingle(Call(Runtime("WeaponBasicAttack"),"Physical",h)),Is.EqualTo(Get(physical,"preview")));
                Assert.That(Convert.ToSingle(Call(Runtime("WeaponBasicAttack"),"Magical",h)),Is.EqualTo(Get(magical,"preview")));
            } finally {UnityEngine.Object.DestroyImmediate(h);}
        }
        [Test]
        public void LegacySwordNeedsNoMetadataMutationOrStatEnumRenumbering()
        {
            var h=Hero();try {
                var sword=Add(h,"Sword");Equip(h,sword);Set(h,"savedAttackMode",Mode("RangedMagic"));Set(h,"isDirty",false);
                string before=JsonUtility.ToJson(h);
                Assert.That(Call(Runtime("WeaponBasicAttack"),"Resolve",h).ToString(),Is.EqualTo("Melee"));
                Assert.That(JsonUtility.ToJson(h),Is.EqualTo(before));
                string[] old={"MaxHealth","Attack","Armor","MagicResistance","ElementResistance","PhysicalDamagePercent",
                    "MagicalDamagePercent","ElementDamagePercent","AttackSpeed","MovementSpeed","CastSpeed",
                    "CooldownReductionPercent","HealthRegeneration","ManaRegeneration","KillHealBonus"};
                for(int i=0;i<old.Length;i++)Assert.That(Convert.ToInt32(Stat(old[i])),Is.EqualTo(i));
                Assert.That(Convert.ToInt32(Stat("MagicalAttack")),Is.EqualTo(15));
            } finally {UnityEngine.Object.DestroyImmediate(h);}
        }
    }
}
