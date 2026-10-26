using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace IdleGame.Tests.Presentation.PlayMode
{
    // Native ScriptableObject and RectTransform contracts. No SaveManager,
    // scene load, player files, runtime Start/Update, or permanent data assets.
    [TestFixture]
    public sealed class InventoryComparisonTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        static Type TypeOf(string name)
        {
            foreach(var a in AppDomain.CurrentDomain.GetAssemblies()) {
                var t=a.GetType(name,false);if(t!=null)return t;
            }
            throw new TypeLoadException(name);
        }
        static object EnumOf(string type,string name) {return Enum.Parse(TypeOf(type),name);}
        static object Get(object o,string name)
        {
            var f=o.GetType().GetField(name,All);
            return f!=null?f.GetValue(o):o.GetType().GetProperty(name,All).GetValue(o,null);
        }
        static void Set(object o,string name,object value) {o.GetType().GetField(name,All).SetValue(o,value);}
        static object Invoke(object o,string method,params object[] args)
        {
            Type t=o as Type ?? o.GetType();
            foreach(var m in t.GetMethods(All))
                if(m.Name==method && m.GetParameters().Length==args.Length)
                    return m.Invoke(o is Type?null:o,args);
            throw new MissingMethodException(t.Name,method);
        }
        static ScriptableObject NewHero()
        {
            var h=ScriptableObject.CreateInstance(TypeOf("EntityDataSO"));
            Invoke(h,"ApplyHeroBalance");
            Invoke(TypeOf("EquipmentInventory"),"InitializeNewRun",h);
            Invoke(TypeOf("EquipmentRuntime"),"InitializeNewRun",h);return h;
        }
        static object Gear(ScriptableObject hero,string template,float bonus)
        {
            var g=Invoke(TypeOf("EquipmentFoundation"),"CreateCommon",template,1,bonus);
            Assert.That(Invoke(TypeOf("EquipmentRuntime"),"AddInstance",hero,g),Is.EqualTo(true));
            return Invoke(TypeOf("EquipmentRuntime"),"Find",hero,Get(g,"instanceId"));
        }
        static object Preview(ScriptableObject hero,string key,bool right=false)
        {
            object[] args={hero,key,right,null,null};
            Assert.That(Invoke(TypeOf("InventoryPanelUI"),"TryComparison",args),Is.EqualTo(true),Convert.ToString(args[4]));
            return args[3];
        }
        static void Rejected(ScriptableObject hero,string key,bool right=false)
        {
            object[] args={hero,key,right,null,null};
            Assert.That(Invoke(TypeOf("InventoryPanelUI"),"TryComparison",args),Is.EqualTo(false));
            Assert.That(args[3],Is.Null);Assert.That(Convert.ToString(args[4]),Is.Not.Empty);
        }
        static float Predicted(object compare,string stat)
        {
            var line=Invoke(compare,"Find",EnumOf("CombatStatId",stat),EnumOf("TuTienCore.ElementType","Vo"));
            return (float)Get(line,"preview");
        }
        [TestCase("sample:health_charm","MaxHealth","GetCalculatedHealth")]
        [TestCase("sample:attack_charm","Attack","GetCalculatedDamage")]
        public void LegacyPredictionMatchesEquipAndUnequipWithoutPreviewMutation(string id,string stat,string total)
        {
            var h=NewHero();
            try {
                Set(h,"isDirty",false);string before=JsonUtility.ToJson(h);
                var p=Preview(h,"stack:"+id);
                Assert.That(JsonUtility.ToJson(h),Is.EqualTo(before),"Preview must not change serialized state.");
                Assert.That(Invoke(TypeOf("EquipmentInventory"),"Equip",h,id),Is.EqualTo(true));
                Assert.That(Convert.ToSingle(Invoke(h,total,Get(h,"savedAttackMode"))),Is.EqualTo(Predicted(p,stat)));
                var off=Preview(h,"worn:"+id);
                Assert.That(Get(off,"isUnequip"),Is.EqualTo(true));
                Assert.That(Invoke(TypeOf("EquipmentInventory"),"Unequip",h),Is.EqualTo(true));
                Assert.That(Convert.ToSingle(Invoke(h,total,Get(h,"savedAttackMode"))),Is.EqualTo(Predicted(off,stat)));
            } finally {UnityEngine.Object.DestroyImmediate(h);}
        }
        [TestCase(false)] [TestCase(true)]
        public void RingDestinationAndReplacementMatchRealAction(bool explicitRight)
        {
            var h=NewHero();
            try {
                var a=Gear(h,"base:ring",3f);var b=Gear(h,"base:ring",5f);var c=Gear(h,"base:ring",9f);
                Invoke(TypeOf("EquipmentRuntime"),"Equip",h,Get(a,"instanceId"),EnumOf("EquipmentSlot","RingLeft"));
                var auto=Preview(h,"gear:"+Get(b,"instanceId"));
                Assert.That(Get(auto,"slot"),Is.EqualTo(EnumOf("EquipmentSlot","RingRight")));
                Invoke(TypeOf("EquipmentRuntime"),"Equip",h,Get(b,"instanceId"),Get(auto,"slot"));
                var p=Preview(h,"gear:"+Get(c,"instanceId"),explicitRight);
                Assert.That(Get(p,"slot"),Is.EqualTo(EnumOf("EquipmentSlot",explicitRight?"RingRight":"RingLeft")));
                Invoke(TypeOf("EquipmentRuntime"),"Equip",h,Get(c,"instanceId"),Get(p,"slot"));
                Assert.That(Convert.ToSingle(Invoke(h,"GetCalculatedDamage",Get(h,"savedAttackMode"))),Is.EqualTo(Predicted(p,"Attack")));
            } finally {UnityEngine.Object.DestroyImmediate(h);}
        }
        [Test]
        public void InvalidAndBlockedItemsHaveReasonsAndNoNumbers()
        {
            var h=NewHero();
            try {
                var a=Gear(h,"base:artifact",30f);
                Invoke(TypeOf("EquipmentInventory"),"Equip",h,"sample:health_charm");
                Rejected(h,"gear:"+Get(a,"instanceId"));
                Invoke(TypeOf("EquipmentInventory"),"Add",h,"sample:health_charm",9999);
                Rejected(h,"worn:sample:health_charm");Rejected(h,"stack:sample:attack_charm");
                Rejected(h,"gear:missing");Rejected(h,"stack:material");
                Set(a,"level",0);Rejected(h,"gear:"+Get(a,"instanceId"));
            } finally {UnityEngine.Object.DestroyImmediate(h);}
        }
        [Test]
        public void HiddenAndLockedAffixesAreExcludedWithoutMutation()
        {
            var h=NewHero();
            try {
                var sword=Invoke(TypeOf("EquipmentRefinement"),"CreateForgeDemoSword");
                Invoke(TypeOf("EquipmentRuntime"),"AddInstance",h,sword);
                sword=Invoke(TypeOf("EquipmentRuntime"),"Find",h,Get(sword,"instanceId"));
                foreach(var line in (IEnumerable)Get(sword,"extraStats")) {Set(line,"flat",9000f);Set(line,"revealed",false);}
                Set(h,"isDirty",false);string before=JsonUtility.ToJson(h);
                var hidden=Preview(h,"gear:"+Get(sword,"instanceId"));
                Assert.That(JsonUtility.ToJson(h),Is.EqualTo(before));
                foreach(var line in (IEnumerable)Get(sword,"extraStats")) {Set(line,"revealed",true);Set(line,"requiredRefineLevel",20);}
                var locked=Preview(h,"gear:"+Get(sword,"instanceId"));
                foreach(var line in (IEnumerable)Get(hidden,"lines")) {
                    var other=Invoke(locked,"Find",Get(line,"stat"),Get(line,"element"));
                    Assert.That(Get(other,"preview"),Is.EqualTo(Get(line,"preview")));
                }
            } finally {UnityEngine.Object.DestroyImmediate(h);}
        }
        [TestCase(600,337)] [TestCase(800,450)] [TestCase(1150,646)]
        public void DetailRegionDoesNotOverlapActionsAtNativeRectSizes(int width,int height)
        {
            foreach(string name in new[]{"GameManager","UIManager","SaveManager"})
                Assert.That(UnityEngine.Object.FindObjectsByType(TypeOf(name),FindObjectsInactive.Include,FindObjectsSortMode.None),
                    Is.Empty,"Use an empty test scene; never reuse gameplay managers.");
            var host=new GameObject("Owned inactive comparison UI");host.SetActive(false);
            var hud=new GameObject("Owned inactive comparison Canvas",typeof(RectTransform),typeof(Canvas));hud.SetActive(false);
            try {
                var ui=host.AddComponent(TypeOf("UIManager"));
                var p=Invoke(TypeOf("InventoryPanelUI"),"Install",ui,(RectTransform)hud.transform);
                Invoke(p,"Layout",(float)width,(float)height,(float)width,(float)height);
                var detail=(Component)Get(p,"detail");var text=(RectTransform)detail.transform;
                var nav=(RectTransform)Get(p,"detailCompareButton");var forge=(RectTransform)Get(p,"forgeButton");
                var equip=(RectTransform)Get(p,"equipButton");
                Assert.That(text.rect.height,Is.GreaterThanOrEqualTo(55f));
                Assert.That(-text.anchoredPosition.y+text.rect.height,Is.LessThanOrEqualTo(-nav.anchoredPosition.y));
                Assert.That(-nav.anchoredPosition.y+nav.rect.height,Is.LessThanOrEqualTo(-forge.anchoredPosition.y));
                Assert.That(-forge.anchoredPosition.y+forge.rect.height,Is.LessThanOrEqualTo(-equip.anchoredPosition.y));
            } finally {UnityEngine.Object.DestroyImmediate(hud);UnityEngine.Object.DestroyImmediate(host);}
        }
    }
}
