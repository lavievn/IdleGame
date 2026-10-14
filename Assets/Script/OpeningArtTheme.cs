using UnityEngine;

// Create once in Assets/Resources/OpeningArtTheme.asset.
// Art slots intentionally have no default assets: the opening is playable
// with colored labels until the developer supplies licensed clean sprites.
[CreateAssetMenu(fileName = "OpeningArtTheme", menuName = "Tu Tien/UI/Opening Art Theme")]
public sealed class OpeningArtTheme : ScriptableObject
{
    public Sprite sky;
    public Sprite farMountains;
    public Sprite nearMountains;
    public Sprite backClouds;
    public Sprite leftClouds;
    public Sprite rightClouds;
    public Sprite splashLeft;
    public Sprite splashRight;
    public Sprite gameTitle;
}
