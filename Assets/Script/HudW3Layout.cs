using System;
using UnityEngine;

// Screen pixels on the readable Canvas. Does not change battle coordinates.
[Serializable]
public sealed class HudW3Layout
{
    [Min(4)] public float margin = 8f;
    [Min(2)] public float gap = 4f;
    [Min(120)] public float heroCardWidth = 138f;
    [Min(72)] public float heroCardHeight = 76f;
    [Min(64)] public float systemButtonWidth = 78f;
    [Min(26)] public float systemButtonHeight = 28f;
    [Min(90)] public float systemTop = 100f;
    [Min(64)] public float mapWidth = 80f;
    [Min(36)] public float mapTop = 48f;
    [Min(100)] public float mapHeight = 166f;
    [Range(11,18)] public float textSize = 12f;
    [Min(32)] public float logHeight = 46f;

    public int Revision
    {
        get
        {
            unchecked
            {
                int h=margin.GetHashCode();
                h=h*31+gap.GetHashCode(); h=h*31+heroCardWidth.GetHashCode();
                h=h*31+heroCardHeight.GetHashCode(); h=h*31+systemButtonWidth.GetHashCode();
                h=h*31+systemButtonHeight.GetHashCode(); h=h*31+systemTop.GetHashCode();
                h=h*31+mapWidth.GetHashCode(); h=h*31+mapTop.GetHashCode();
                h=h*31+mapHeight.GetHashCode(); h=h*31+textSize.GetHashCode();
                return h*31+logHeight.GetHashCode();
            }
        }
    }
}
