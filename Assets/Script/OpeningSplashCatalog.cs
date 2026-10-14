using UnityEngine;

// Contains paths, not heavyweight direct Sprite/VideoClip references.
// The editor refreshes this index when Assets/Resources/OpeningSplashArt changes.
public sealed class OpeningSplashCatalog : ScriptableObject
{
    public string[] images = new string[0];
    public string[] videos = new string[0];
}
