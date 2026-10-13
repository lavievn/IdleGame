using System;

// Fixed geometry contract shared by the battle projection, HUD and desktop
// window controller. Never feed the expanded window width into combat math.
public static class ManagementWindowLayout
{
    public const int MinimumSidePanel = 420;
    public const int PreferredSidePanel = 560;
    public const int MaximumSidePanel = 650;
    public const int MinimumWindowWidth = 320;
    public const int MinimumWindowHeight = 240;

    public static int CompactWidth { get; private set; }
    public static int CompactHeight { get; private set; }
    public static bool Expanded { get; private set; }
    public static int TargetWidth { get; private set; }

    public static void SetCompact(int width,int height)
    {
        if(Expanded)return;
        CompactWidth=Math.Max(MinimumWindowWidth,width);
        CompactHeight=Math.Max(MinimumWindowHeight,height);
        TargetWidth=CompactWidth;
    }
    public static bool Expand(int maxDesktopWidth)
    {
        if(Expanded)return true;
        if(CompactWidth<=0 || CompactHeight<=0)return false;
        int available=Math.Max(0,maxDesktopWidth-CompactWidth);
        if(available<MinimumSidePanel)return false;
        int extra=Math.Min(MaximumSidePanel,Math.Min(PreferredSidePanel,available));
        Expanded=true;
        TargetWidth=CompactWidth+extra;
        return true;
    }
    public static void Collapse()
    {
        Expanded=false;
        TargetWidth=CompactWidth;
    }
    public static int CombatPixels(int actualWindowWidth)
    {
        return Expanded ? Math.Min(CompactWidth,Math.Max(1,actualWindowWidth))
            : Math.Max(1,actualWindowWidth);
    }
    public static int CombatHeight(int actualWindowHeight)
    {
        return Expanded ? Math.Min(CompactHeight,Math.Max(1,actualWindowHeight))
            : Math.Max(1,actualWindowHeight);
    }
    public static float BattleCanvasScale(int actualWindowWidth,float logicalBattleWidth)
    {
        return CombatPixels(actualWindowWidth)/Math.Max(1f,logicalBattleWidth);
    }
    // Battle canvas overlay has its origin at the *center of the entire*
    // expanded Unity window. Shift only the SCREEN projection left so its
    // center stays at compactWidth / 2, independent of item UI width.
    public static float BattleLocalOffsetX(int actualWindowWidth,float logicalBattleWidth)
    {
        float scale=BattleCanvasScale(actualWindowWidth,logicalBattleWidth);
        return (CombatPixels(actualWindowWidth)-actualWindowWidth)/(2f*scale);
    }
    public static int SidePanelPixels(int actualWindowWidth)
    {
        return Expanded ? Math.Max(0,actualWindowWidth-CombatPixels(actualWindowWidth)) : 0;
    }
}
