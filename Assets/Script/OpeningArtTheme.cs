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

    // Pixel-equivalent UI sizes at the 800 x 450 reference window.
    // At other game sizes OpeningCinematicUI scales each axis independently,
    // retaining the pre-existing responsive layout. No changes to positions.
    [Header("Mountain and Cloud Sizes (reference: 800 x 450)")]
    [Tooltip("Far mountains: width / height in UI units for an 800 x 450 window.")]
    public Vector2 farMountainsReferenceSize = new Vector2(944f,279f);
    [Tooltip("Near mountains: width / height in UI units for an 800 x 450 window.")]
    public Vector2 nearMountainsReferenceSize = new Vector2(992f,153f);
    [Tooltip("Back clouds: width / height in UI units for an 800 x 450 window.")]
    public Vector2 backCloudsReferenceSize = new Vector2(960f,198f);
    [Tooltip("Front left clouds: width / height in UI units for an 800 x 450 window.")]
    public Vector2 leftCloudsReferenceSize = new Vector2(536f,409.5f);
    [Tooltip("Front right clouds: width / height in UI units for an 800 x 450 window.")]
    public Vector2 rightCloudsReferenceSize = new Vector2(536f,409.5f);

    [Header("Cloud / Mountain Edge")]
    [Range(0f,80f)] public float edgeFeatherPixels = 48f;
}
