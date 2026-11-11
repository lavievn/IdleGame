using UnityEngine;

// Project-local bridge for licensed assets. The base game can build without a
// vendor assembly. Implementations should reuse output textures and emitters.
public abstract class WorldClimateAssetAdapter : MonoBehaviour
{
    internal virtual void SetSkyScroll(float viewportWidths) { }

    // Supply a view-projected 2D texture, including phase/weather treatment.
    // Return false until the texture can replace the legacy sky completely.
    public abstract bool TryGetProjectedSky(WorldClimateFrame frame, Rect viewport, out Texture texture);

    // Bounds are BattleArea-local. Disable audio and use non-interactive visuals.
    public abstract void SetRain2D(float intensity, RectTransform battleArea, Rect viewport);
    public abstract void StopRain2D();
}
