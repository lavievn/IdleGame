#if HANVU_STORY_TESTS
using System;

// Data-only entry point. Never instantiates managers or invokes SaveManager.
partial class MotionRegression
{
    static int storyTestCount;
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    static void Run(string name, Action test)
    {
        test();
        storyTestCount++;
        Console.WriteLine("PASS " + name);
    }
    static void Main()
    {
        WorldMapRegressionTests();
        StoryOriginTests();
        Console.WriteLine("PASS " + storyTestCount + " data-only groups (UnityStub; no save I/O)");
    }
}

// CombatBalance also declares an environment-facing profile helper. This runner
// must fail if any data test crosses into that unrelated presentation path.
public sealed class EnvironmentManager
{
    public float DefaultAttackRange(TuTienCore.AttackMode mode, bool hero)
    {
        throw new NotSupportedException("No environment in data-only story tests");
    }
}
#endif
