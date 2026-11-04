using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace IdleGame.Tests.Presentation.PlayMode
{
    // Own objects only, no scene loads, managers, inventory or save I/O.
    public sealed class LootDropPresentationTests
    {
        const BindingFlags All = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static Type Runtime(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            { var type = assembly.GetType(name, false); if (type != null) return type; }
            throw new TypeLoadException(name);
        }
        static object Call(object target, string name, params object[] args)
        { return target.GetType().GetMethod(name, All).Invoke(target, args); }
        static object Get(object target, string name)
        { return target.GetType().GetProperty(name, All).GetValue(target, null); }
        sealed class Fixture : IDisposable
        {
            public readonly GameObject Owner, CanvasRoot;
            public readonly RectTransform Area;
            public readonly Component Visual;
            public int Count => (int)Get(Visual, "Count");
            public RectTransform First => (RectTransform)((IList)Get(Visual, "Roots"))[0];
            public Fixture(float width = 800f)
            {
                Assert.That(Application.isPlaying, Is.True);
                // Never touch managers/registries in a user's running game.
                foreach (string name in new[] { "SaveManager", "GameManager", "CombatManager", "EnvironmentManager", "HeroController", "MonsterController" })
                    Assert.That(UnityEngine.Object.FindObjectsByType(Runtime(name), FindObjectsInactive.Include,
                        FindObjectsSortMode.None).Length, Is.EqualTo(0), "Run in an empty scene: " + name);
                CanvasRoot = new GameObject("Loot native fixture", typeof(RectTransform), typeof(Canvas));
                var canvas = CanvasRoot.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.scaleFactor = width / 1920f;
                Area = new GameObject("Test BattleArea", typeof(RectTransform)).GetComponent<RectTransform>();
                Area.SetParent(CanvasRoot.transform, false);
                Area.sizeDelta = new Vector2(1920f, 300f);
                Area.pivot = new Vector2(.5f, 0f);
                Owner = new GameObject("Loot native owner");
                Visual = Owner.AddComponent(Runtime("LootDropPresentation"));
                Canvas.ForceUpdateCanvases();
            }
            public void Show(string id = "native-item", string template = "sample:iron_sword")
            { Call(Visual, "Show", Area, new Vector2(123f, 42f), template, id, null); }
            public void Advance(float dt) { Call(Visual, "Advance", dt, null); }
            public void Dispose()
            {
                if (Owner != null) UnityEngine.Object.DestroyImmediate(Owner);
                if (CanvasRoot != null) UnityEngine.Object.DestroyImmediate(CanvasRoot);
            }
        }

        [TestCase(600f)] [TestCase(800f)] [TestCase(1150f)]
        public void SpriteArcsRestsHalfSecondThenShrinksAndFliesToScreenEdge(float width)
        {
            using (var f = new Fixture(width))
            {
                f.Show(); Assert.That(f.Count, Is.EqualTo(1), "Sprite must import through Resources.");
                var foot = f.First;
                var icon = (RectTransform)foot.GetChild(0);
                var image = icon.GetComponent(Runtime("UnityEngine.UI.Image"));
                Assert.That(Get(image, "sprite"), Is.SameAs(Resources.Load<Sprite>("Loot/iron_sword")));
                Assert.That(Get(image, "preserveAspect"), Is.EqualTo(true));
                Assert.That(Get(image, "raycastTarget"), Is.EqualTo(false));
                Assert.That(icon.rect.width * f.CanvasRoot.GetComponent<Canvas>().scaleFactor,
                    Is.EqualTo(40f).Within(.001f), "Icon keeps a readable screen size.");
                Assert.That(Vector2.Distance(f.Area.InverseTransformPoint(foot.position), new Vector2(123f, 42f)), Is.LessThan(.001f));
                float startY = icon.anchoredPosition.y;
                f.Advance(0f); Assert.That(icon.anchoredPosition.y, Is.EqualTo(startY));
                f.Advance(.3f); Assert.That(icon.anchoredPosition.y, Is.GreaterThan(startY));
                f.Advance(.3f); Assert.That(icon.anchoredPosition.y, Is.EqualTo(8f).Within(.001f));
                Assert.That(icon.localEulerAngles.z, Is.EqualTo(330f).Within(.001f));
                Call(f.Visual, "Pan", -67f);
                Assert.That(Vector2.Distance(f.Area.InverseTransformPoint(foot.position), new Vector2(56f, 42f)), Is.LessThan(.001f));
                f.Advance(.49f);
                Assert.That(icon.localScale.x, Is.EqualTo(1f));
                Assert.That(foot.anchoredPosition.x, Is.EqualTo(56f));
                f.Advance(.16f); // 0.15s into the collection flight.
                Assert.That(icon.localScale.x, Is.LessThan(1f));
                Assert.That(foot.anchoredPosition.x, Is.GreaterThan(56f));
                Assert.That(((ICollection)Get(f.Visual, "Roots")).Count, Is.EqualTo(0), "Flight leaves ground depth sorting.");
                Vector2 flightPosition = foot.anchoredPosition;
                Call(f.Visual, "Pan", -200f);
                Assert.That(foot.anchoredPosition, Is.EqualTo(flightPosition), "Flight is not dragged by the ground.");
                f.Advance(0f); Assert.That(foot.anchoredPosition, Is.EqualTo(flightPosition), "Paused animation stays still.");
                f.Advance(.38f); // 0.53s into the 0.55s flight, almost at screen edge.
                var screen = RectTransformUtility.WorldToScreenPoint(null, foot.position);
                Assert.That(screen.x, Is.GreaterThan(Screen.width - 150f));
                Assert.That(icon.localScale.x, Is.LessThan(.2f));
                f.Advance(.03f); Assert.That(f.Count, Is.EqualTo(0));
            }
        }

        [UnityTest]
        public IEnumerator VisualBudgetFlightCompletionAndDisableRemoveOnlyOwnedGraphics()
        {
            using (var f = new Fixture())
            {
                for (int i = 0; i < 60; i++) f.Show("item-" + i);
                Assert.That(f.Count, Is.EqualTo(48));
                yield return null; // Real Unity delayed Destroy for the evicted roots.
                Assert.That(f.Area.childCount, Is.EqualTo(48));
                f.Advance(2f);
                Assert.That(f.Count, Is.EqualTo(0));
                yield return null; Assert.That(f.Area.childCount, Is.EqualTo(0));
                f.Show(); ((Behaviour)f.Visual).enabled = false;
                Assert.That(f.Count, Is.EqualTo(0));
                f.Show(); Assert.That(f.Count, Is.EqualTo(0), "Disabled component must not spawn graphics.");
                yield return null; Assert.That(f.Area.childCount, Is.EqualTo(0));
            }
        }

        [Test]
        public void UnsupportedTemplatesAndEmptyIdsProduceNoGraphicAndNoCombatRandomRoll()
        {
            using (var f = new Fixture())
            {
                var random = UnityEngine.Random.state;
                try
                {
                    UnityEngine.Random.InitState(811);
                    float expected = UnityEngine.Random.value;
                    UnityEngine.Random.InitState(811);
                    f.Show("bow", "sample:basic_bow"); f.Show("");
                    Assert.That(f.Count, Is.EqualTo(0));
                    f.Show(); f.Advance(1f);
                    Assert.That(UnityEngine.Random.value, Is.EqualTo(expected));
                    Call(f.Visual, "Clear"); Call(f.Visual, "Clear");
                    Assert.That(f.Count, Is.EqualTo(0));
                }
                finally { UnityEngine.Random.state = random; }
            }
        }
    }
}
