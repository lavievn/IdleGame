using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace IdleGame.Tests.Presentation.PlayMode
{
    public sealed class MonsterAwarenessTests
    {
        const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        static Type Runtime(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            { var t = assembly.GetType(name, false); if (t != null) return t; }
            throw new TypeLoadException(name);
        }
        static object Get(object o, string name)
        {
            var field = o.GetType().GetField(name, All);
            return field != null ? field.GetValue(o) : o.GetType().GetProperty(name, All).GetValue(o);
        }
        static void Set(object o, string name, object value) { o.GetType().GetField(name, All).SetValue(o, value); }
        static object Call(object o, string name, params object[] args)
        {
            var type = o as Type ?? o.GetType();
            foreach (var m in type.GetMethods(All))
                if (m.Name == name && m.GetParameters().Length == args.Length)
                    return m.Invoke(o is Type ? null : o, args);
            throw new MissingMethodException(type.Name, name);
        }
        sealed class Fixture : IDisposable
        {
            readonly List<GameObject> objects = new List<GameObject>();
            readonly List<ScriptableObject> data = new List<ScriptableObject>();
            readonly UnityEngine.Random.State random;
            public readonly Canvas Canvas;
            public readonly RectTransform Area, HeroRect;
            public readonly Component Hero, Combat, Space, Spawner;
            public readonly List<Component> Monsters = new List<Component>();
            public IList Enemies => (IList)Get(Combat, "activeMonsters");
            GameObject Root(string name, params Type[] types)
            { var go = new GameObject(name, types); objects.Add(go); return go; }
            public Fixture(float width = 800f)
            {
                Assert.That(Application.isPlaying, Is.True);
                foreach (string name in new[] { "SaveManager", "GameManager", "CombatManager", "EnvironmentManager", "HeroController", "MonsterController" })
                    Assert.That(UnityEngine.Object.FindObjectsByType(Runtime(name), FindObjectsInactive.Include, FindObjectsSortMode.None),
                        Is.Empty, "Use an empty scene; existing objects are not owned by this fixture.");
                random = UnityEngine.Random.state;
                try
                {
                    Canvas = Root("Owned awareness Canvas", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
                    Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    Area = Root("Owned awareness Ground", typeof(RectTransform)).GetComponent<RectTransform>();
                    Area.sizeDelta = new Vector2(1000, 200); Area.pivot = new Vector2(.5f, 0f);
                    var environment = Root("Owned awareness environment"); environment.SetActive(false);
                    Space = environment.AddComponent(Runtime("EnvironmentManager")); ((Behaviour)Space).enabled = false;
                    Set(Space, "battleArea", Area); environment.SetActive(true);
                    Area.SetParent(Canvas.transform, false);
                    Area.anchorMin = Area.anchorMax = new Vector2(.5f, 0f);
                    Canvas.scaleFactor = width / 1000f;
                    HeroRect = Root("Owned awareness Hero body", typeof(RectTransform), Runtime("UnityEngine.UI.Image")).GetComponent<RectTransform>();
                    HeroRect.SetParent(Area, false); HeroRect.sizeDelta = new Vector2(50, 100); HeroRect.pivot = new Vector2(.5f, 0f);
                    var hero = Root("Owned awareness Hero"); hero.SetActive(false);
                    Hero = hero.AddComponent(Runtime("HeroController")); Set(Hero, "heroRect", HeroRect);
                    hero.SetActive(true); Call(Hero, "SpawnHero", true, (Vector2?)new Vector2(400, 0));
                    var combat = Root("Owned awareness combat"); combat.SetActive(false);
                    Combat = combat.AddComponent(Runtime("CombatManager")); ((Behaviour)Combat).enabled = false;
                    combat.SetActive(true); Call(Combat, "Start");
                    var h = ScriptableObject.CreateInstance(Runtime("EntityDataSO")); data.Add(h);
                    Call(h, "ApplyHeroBalance");
                    Call(Runtime("EquipmentInventory"), "InitializeNewRun", h);
                    Call(Runtime("EquipmentRuntime"), "InitializeNewRun", h);
                    Call(Combat, "SetupHeroInfo", h);
                    var prefab = Root("Owned awareness inactive monster", typeof(RectTransform), Runtime("UnityEngine.UI.Image")); prefab.SetActive(false);
                    prefab.AddComponent(Runtime("MonsterController"));
                    var spawn = Root("Owned awareness spawner"); spawn.SetActive(false);
                    Spawner = spawn.AddComponent(Runtime("MonsterSpawner"));
                    Set(Spawner, "monsterPrefab", prefab); Set(Spawner, "monsterContainer", Area);
                    spawn.SetActive(true); Set(Combat, "monsterSpawner", Spawner);
                    Canvas.ForceUpdateCanvases();
                }
                catch { Dispose(); throw; }
            }
            public Component Spawn(float x, float y = 0f)
            {
                var go = (GameObject)Call(Spawner, "SpawnMonster");
                var m = go.GetComponent(Runtime("MonsterController")); Monsters.Add(m);
                Place((RectTransform)go.transform, x, y); return m;
            }
            public void Battle()
            {
                var template = ScriptableObject.CreateInstance(Runtime("EntityDataSO")); data.Add(template);
                var list = new List<GameObject>(); foreach (var m in Monsters) list.Add(m.gameObject);
                Call(Combat, "StartBattle", list, template);
                foreach (var e in Enemies) data.Add((ScriptableObject)Get(e, "data"));
            }
            public void Place(RectTransform rect, float x, float y = 0f) { Call(Space, "SetPosition", rect, new Vector2(x, y)); }
            public Vector2 Position(Component m) => (Vector2)Call(Space, "Position", Get(m, "Rect"));
            public void Look(Component m, bool right) { Call(m, "SetFacing", right); Set(m, "nextLook", 5f); }
            public void Tick(float dt) { Call(Combat, "TickMonsterAwareness", Space, dt); }
            public void Hit(int index, int damage) { Call(Combat, "DealDamageToMonster", Enemies[index], damage, null); }
            public void Dispose()
            {
                for (int i = objects.Count - 1; i >= 0; i--)
                    if (objects[i] != null) UnityEngine.Object.DestroyImmediate(objects[i]);
                foreach (var d in data) if (d != null) UnityEngine.Object.DestroyImmediate(d);
                UnityEngine.Random.state = random;
            }
        }

        [TestCase(false, 0, 550f)] [TestCase(true, 0, 550f)]
        [TestCase(false, 1, 800f)] [TestCase(true, 2, 800f)]
        public void SightRequiresCorrectFacingAndInclusiveModeSpecificDistance(bool right, int mode, float range)
        {
            using (var f = new Fixture())
            {
                var m = f.Spawn(0); f.Battle(); f.Look(m, right);
                Set(m, "attackMode", Enum.ToObject(Runtime("TuTienCore.AttackMode"), mode));
                float sign = right ? 1 : -1;
                foreach (var p in new[] { new Vector2(-sign * 50, 0), new Vector2(sign * (range + 1f), 0), new Vector2(sign * range, 90) })
                {
                    f.Place(f.HeroRect, p.x, p.y); f.Tick(0f);
                    Assert.That(Get(m, "HasDetectedHero"), Is.EqualTo(false), $"Mode {mode}, Hero at {p}");
                }
                f.Place(f.HeroRect, sign * range); f.Tick(0f);
                Assert.That(Get(m, "Awareness").ToString(), Is.EqualTo("Suspicious"));
            }
        }
        [TestCase(600f)] [TestCase(800f)] [TestCase(1150f)]
        public void GroupQuestionIsYellowAndReadableThenTurnsRedAfterHalfSecond(float width)
        {
            using (var f = new Fixture(width))
            {
                var a = f.Spawn(0); var b = f.Spawn(-900); f.Battle(); f.Look(a, true); f.Look(b, false);
                f.Place(f.HeroRect, 300); f.Tick(.1f);
                foreach (var m in f.Monsters)
                {
                    var label = (Component)Get(m, "awarenessLabel");
                    Assert.That(Get(label, "text"), Is.EqualTo("?"));
                    var color = (Color)Get(label, "color"); Assert.That(color.r, Is.GreaterThan(.9f)); Assert.That(color.g, Is.GreaterThan(.7f));
                    Assert.That((float)Get(label, "fontSize") * f.Canvas.scaleFactor, Is.EqualTo(24f).Within(.01f));
                    Vector2 p = f.Position(m); Call(m, "TickMovement", f.Space, .2f, -37f);
                    Vector2 moved = f.Position(m) - p - new Vector2(-37f, 0f);
                    Assert.That(moved.magnitude, Is.EqualTo(50f * .2f).Within(.02f),
                        "Suspicious monsters still wander at idle speed relative to the ground.");
                    Assert.That(Get(m, "IsAggressive"), Is.EqualTo(false));
                    Assert.That(Call(m, "CanAttack", f.Hero, f.Space), Is.EqualTo(false));
                }
                f.Tick(.49f); Assert.That(Get(a, "IsAggressive"), Is.EqualTo(false));
                f.Tick(.01f);
                foreach (var m in f.Monsters)
                {
                    Assert.That(Get(m, "IsAggressive"), Is.EqualTo(true));
                    var label = Get(m, "awarenessLabel"); Assert.That(Get(label, "text"), Is.EqualTo("!"));
                    Assert.That(((Color)Get(label, "color")).g, Is.LessThan(.2f));
                }
            }
        }
        [Test]
        public void SurpriseDoublesFinalDamageOnceAndAlliesCounterImmediatelyEvenOnLethalHit()
        {
            using (var f = new Fixture())
            {
                var a = f.Spawn(0); var b = f.Spawn(-900); f.Battle();
                Set(f.Enemies[0], "currentHP", 1000);
                f.Hit(0, 17); Assert.That(Get(f.Enemies[0], "currentHP"), Is.EqualTo(966));
                Assert.That(Get(a, "IsAggressive"), Is.EqualTo(true)); Assert.That(Get(b, "IsAggressive"), Is.EqualTo(true));
                f.Hit(0, 17); Assert.That(Get(f.Enemies[0], "currentHP"), Is.EqualTo(949));
            }
            using (var f = new Fixture())
            {
                f.Spawn(0); var survivor = f.Spawn(-900); f.Battle(); f.Hit(0, int.MaxValue);
                Assert.That(f.Enemies.Count, Is.EqualTo(1)); Assert.That(Get(survivor, "IsAggressive"), Is.EqualTo(true));
            }
        }
        [Test]
        public void HitDuringQuestionDoesNormalDamageAndEndsGroupDelayImmediately()
        {
            using (var f = new Fixture())
            {
                var a = f.Spawn(0); var b = f.Spawn(-900); f.Battle(); f.Look(a, true); f.Place(f.HeroRect, 100); f.Tick(0f);
                Set(f.Enemies[0], "currentHP", 1000); f.Hit(0, 17);
                Assert.That(Get(f.Enemies[0], "currentHP"), Is.EqualTo(983));
                Assert.That(Get(a, "IsAggressive"), Is.EqualTo(true)); Assert.That(Get(b, "IsAggressive"), Is.EqualTo(true));
            }
        }
        [Test]
        public void NativePoolReuseResetsMarkerFacingTimerAndSurpriseEligibility()
        {
            using (var f = new Fixture())
            {
                var m = f.Spawn(0); Call(m, "ReceiveAlert", f.Hero, true);
                var label = (Component)Get(m, "awarenessLabel"); int spawn = (int)Get(m, "SpawnVersion");
                Call(f.Spawner, "DespawnMonster", m.gameObject);
                Assert.That(label.gameObject.activeSelf, Is.False);
                var reused = f.Spawn(0);
                Assert.That(reused, Is.SameAs(m)); Assert.That(Get(m, "SpawnVersion"), Is.EqualTo(spawn + 1));
                Assert.That(Get(m, "HasDetectedHero"), Is.EqualTo(false)); Assert.That(label.gameObject.activeSelf, Is.False);
                Assert.That((float)Get(m, "nextLook"), Is.InRange(1.5f, 5f));
            }
        }
        [UnityTest]
        public IEnumerator PauseStopsQuestionAndFacingWithoutLeakingObjects()
        {
            float original = Time.timeScale;
            try
            {
                using (var f = new Fixture())
                {
                    var m = f.Spawn(0); f.Battle(); f.Look(m, true); f.Place(f.HeroRect, 100); f.Tick(0f); f.Tick(.2f);
                    Time.timeScale = 0f; yield return new WaitForSecondsRealtime(.1f); f.Tick(5f);
                    Assert.That(Get(m, "IsAggressive"), Is.EqualTo(false));
                    Time.timeScale = original; f.Tick(.29f); Assert.That(Get(m, "IsAggressive"), Is.EqualTo(false));
                    f.Tick(.01f); Assert.That(Get(m, "IsAggressive"), Is.EqualTo(true));
                }
                yield return null;
                Assert.That(UnityEngine.Object.FindObjectsByType(Runtime("MonsterController"), FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);
            }
            finally { Time.timeScale = original; }
        }
    }
}
