using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public partial class GroundPresentation
{
    private Color worldSkyTint = Color.white;
    private Color worldLandTint = Color.white;
    private WorldClimateAssetAdapter climateAdapter;
    private RawImage projectedSky;
    private bool rainRunning;
    private float stormHaze;
    private bool missingAssetsReported;
    private bool invalidSkyReported;

    private Color GroundTint => (UsesTerrainArtwork ? Color.white : terrainTint) * worldLandTint;
    private Color NearTint => (UsesTerrainArtwork ? Color.white : farTint) * worldLandTint;
    private Color DistantTint
    {
        get
        {
            Color tint = (UsesTerrainArtwork ? Color.white : distantTint) * worldLandTint;
            tint.a *= 1f - stormHaze;
            return tint;
        }
    }

    internal void ApplyWorldClimate(WorldClimateFrame frame, bool lighting,
        bool hdrSky, bool rain2D, WorldClimateAssetAdapter adapter, WorldWeatherVisual visual = default)
    {
        Color skyTint = lighting ? frame.SkyTint : Color.white;
        SetClimateTint(Color.Lerp(skyTint, Color.white, visual.Flash), lighting ? frame.LandTint : Color.white);
        SetStormHaze(visual.Haze);
        if (climateAdapter != adapter)
        {
            StopClimateRain();
            climateAdapter = adapter;
            invalidSkyReported = false;
        }

        if (adapter == null || !adapter.isActiveAndEnabled)
        {
            if (!missingAssetsReported && (hdrSky || rain2D))
            {
                missingAssetsReported = true;
                Debug.Log("[WorldClimate] Asset adapter absent or inactive. Keeping legacy sky and skipping Rain Maker.");
            }
            SetProjectedSky(null, default);
            StopClimateRain();
            return;
        }

        Rect viewport = ClimateViewport();
        adapter.SetWeatherVisual(visual, climateGroundScroll, Mathf.Clamp(grassY, space.BattleArea.rect.yMin, space.BattleArea.rect.yMax));
        Texture sky = null;
        if (hdrSky && viewport.width > 0f && viewport.height > 0f)
        {
            adapter.SetSkyScroll(projectedSkyScroll);
            if (!adapter.TryGetProjectedSky(frame, viewport, out sky) ||
                sky == null || sky.dimension != TextureDimension.Tex2D ||
                (sky is RenderTexture target && !target.IsCreated()))
            {
                sky = null;
                if (!invalidSkyReported)
                {
                    invalidSkyReported = true;
                    Debug.LogWarning("[WorldClimate] Projected HDR sky unavailable. Keeping legacy sky.");
                }
            }
            else invalidSkyReported = false;
        }
        SetProjectedSky(sky, viewport);

        if (rain2D && frame.Rain > 0f && viewport.width > 0f && viewport.height > 0f)
        {
            adapter.SetRain2D(frame.Rain, space.BattleArea, viewport);
            rainRunning = true;
        }
        else StopClimateRain();
    }

    internal void ClearWorldClimate()
    {
        SetClimateTint(Color.white, Color.white);
        SetStormHaze(0f);
        SetProjectedSky(null, default);
        StopClimateRain();
    }

    private void StopClimateRain()
    {
        if (!rainRunning) return;
        rainRunning = false;
        if (climateAdapter != null) climateAdapter.StopRain2D();
    }

    private Rect ClimateViewport()
    {
        Rect viewport = space.BattleArea.rect;
        viewport.height = viewport.width * ManagementWindowLayout.CombatHeight(Screen.height) /
            Mathf.Max(1, ManagementWindowLayout.CombatPixels(Screen.width));
        return viewport;
    }

    private void SetProjectedSky(Texture texture, Rect viewport)
    {
        if (texture != null && projectedSky == null)
        {
            var root = new GameObject("Projected HDR Sky", typeof(RectTransform), typeof(RawImage));
            root.transform.SetParent(space.BattleArea, false);
            root.transform.SetAsFirstSibling();
            projectedSky = root.GetComponent<RawImage>();
            projectedSky.raycastTarget = false;
            projectedSky.rectTransform.anchorMin = projectedSky.rectTransform.anchorMax = space.BattleArea.pivot;
            projectedSky.rectTransform.pivot = Vector2.zero;
            previousDepthOrder.Clear();
        }
        if (projectedSky == null) return;

        bool visible = texture != null;
        if (visible)
        {
            projectedSky.rectTransform.anchoredPosition = viewport.position;
            projectedSky.rectTransform.sizeDelta = viewport.size;
        }
        if (projectedSky.texture != texture) projectedSky.texture = texture;
        if (projectedSky.gameObject.activeSelf != visible)
            projectedSky.gameObject.SetActive(visible);
        SetLegacySkyVisibility();
    }

    private void SetLegacySkyVisibility()
    {
        bool visible = UsesTerrainArtwork && (projectedSky == null || !projectedSky.gameObject.activeSelf);
        foreach (var rect in skyTiles)
            if (rect.gameObject.activeSelf != visible) rect.gameObject.SetActive(visible);
    }

    private void SetClimateTint(Color sky, Color land)
    {
        if (sky == worldSkyTint && land == worldLandTint) return;
        worldSkyTint = sky;
        worldLandTint = land;
        TintTiles(skyTiles, worldSkyTint);
        ApplyTerrainColors();
    }

    private void SetStormHaze(float value)
    {
        if (Mathf.Approximately(stormHaze, value)) return;
        stormHaze = value;
        TintTiles(distantTiles, DistantTint);
    }

    private void ApplyTerrainColors()
    {
        TintTiles(tiles, GroundTint);
        TintTiles(farTiles, NearTint);
        TintTiles(distantTiles, DistantTint);
    }

    private static void TintTiles(System.Collections.Generic.List<RectTransform> layer, Color tint)
    {
        foreach (var rect in layer)
            if (rect != null) rect.GetComponent<Image>().color = tint;
    }
}
