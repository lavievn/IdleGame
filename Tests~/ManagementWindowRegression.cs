using System;

partial class MotionRegression
{
    static void ManagementWindowRegressionTests()
    {
        Run("0.0.52 expanded window keeps 800x450 battle projection unchanged",()=>{
            ManagementWindowLayout.SetCompact(800,450);
            float before=ManagementWindowLayout.BattleCanvasScale(800,1920f);
            Check(ManagementWindowLayout.Expand(1920,1080),
                "1920 desktop fits right-side dock");
            Check(ManagementWindowLayout.TargetWidth==1360 &&
                  ManagementWindowLayout.TargetHeight==680,
                "management preset grows right and upward");
            Check(ManagementWindowLayout.CombatPixels(1360)==800 &&
                  ManagementWindowLayout.CombatHeight(680)==450,
                "logical battle viewport stays exactly compact");
            Near(ManagementWindowLayout.BattleCanvasScale(1360,1920f),
                before,"battle scale does not change during expansion");
            float physicalCenter=1360f*.5f+
                ManagementWindowLayout.BattleLocalOffsetX(1360,1920f)*before;
            Near(physicalCenter,400f,
                "world canvas is shifted to center of original 800px region");
            Check(ManagementWindowLayout.SidePanelPixels(1360)==560,
                "all extra pixels belong to management UI only");
            ManagementWindowLayout.Collapse();
            Check(ManagementWindowLayout.CollapsePending,
                "freeze battle projection until async resize is finished");
            Check(ManagementWindowLayout.CombatPixels(1360)==800,
                "closing while window still wide does not stretch combat");
            ManagementWindowLayout.ResolveResize(1360,680);
            Check(ManagementWindowLayout.CollapsePending,
                "intermediate Windows resize cannot erase compact preset");
            ManagementWindowLayout.ResolveResize(800,450);
            Check(!ManagementWindowLayout.CollapsePending &&
                  ManagementWindowLayout.CombatPixels(800)==800,
                "transition completes when original size returns");
            Check(ManagementWindowLayout.CompactWidth==800 &&
                  ManagementWindowLayout.CompactHeight==450,
                "preset survives opening and closing");
        });

        Run("0.0.52 600 and 1150 presets preserve battle; narrow displays fall back",()=>{
            ManagementWindowLayout.SetCompact(600,338);
            Check(ManagementWindowLayout.Expand(1920,1080),
                "small combat window fits a management dock");
            Check(ManagementWindowLayout.CombatPixels(
                ManagementWindowLayout.TargetWidth)==600,
                "600px combat width is invariant");
            Check(ManagementWindowLayout.CombatHeight(
                ManagementWindowLayout.TargetHeight)==338,
                "338px combat height is invariant");
            ManagementWindowLayout.Collapse();
            ManagementWindowLayout.ResolveResize(600,338);
            ManagementWindowLayout.SetCompact(1150,647);
            Check(!ManagementWindowLayout.Expand(1400,900),
                "not enough monitor space -> compact overlay instead");
            Check(!ManagementWindowLayout.Expanded &&
                ManagementWindowLayout.TargetWidth==1150,
                "fallback never changes outer window dimensions");
            Check(ManagementWindowLayout.Expand(1920,1080),
                "1920 desktop does fit expanded 1150 combat preset");
            Check(ManagementWindowLayout.CombatPixels(
                ManagementWindowLayout.TargetWidth)==1150,
                "larger preset combat still unchanged");
            ManagementWindowLayout.Collapse();
            ManagementWindowLayout.ResolveResize(1150,647);
        });

        Run("0.0.52 explicit window-size change must not relearn stale dimensions",()=>{
            ManagementWindowLayout.SetCompact(800,450);
            ManagementWindowLayout.RequestCompact(1150,647);
            Check(ManagementWindowLayout.CompactWidth==1150 &&
                  ManagementWindowLayout.CompactHeight==647,
                  "new combat preset requested");
            Check(ManagementWindowLayout.CompactResizePending,
                  "old Screen size is temporarily stale");
            ManagementWindowLayout.ResolveResize(800,450);
            Check(ManagementWindowLayout.CompactResizePending,
                  "stale frame cannot discard the requested preset");
            Check(!ManagementWindowLayout.Expand(1920,1080),
                  "management dock must wait for compact resize completion");
            ManagementWindowLayout.ResolveResize(1150,647);
            Check(!ManagementWindowLayout.CompactResizePending,
                  "new desktop preset finished");
            Check(ManagementWindowLayout.Expand(1920,1080),
                  "management can expand from new 1150 width");
            Check(ManagementWindowLayout.CombatPixels(
                ManagementWindowLayout.TargetWidth)==1150,
                "new compact size maintained as world projection width");
            ManagementWindowLayout.Collapse();
            ManagementWindowLayout.ResolveResize(1150,647);
        });

        Run("0.0.52 zero-width or impossible desktop dimensions never produce invalid battle scale",()=>{
            ManagementWindowLayout.SetCompact(800,450);
            Check(!ManagementWindowLayout.Expand(1120,900),
                "minimum dock width enforced");
            Check(ManagementWindowLayout.TargetWidth==800,
                "rejected request leaves compact mode unchanged");
            Check(ManagementWindowLayout.CombatPixels(0)==1,
                "guard zero-size transitional frame");
            Check(ManagementWindowLayout.BattleCanvasScale(0,1920f)>0f,
                "battle canvas scale never divides by zero");
        });
    }
}
