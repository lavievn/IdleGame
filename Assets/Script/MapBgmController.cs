using UnityEngine;
using TuTienCore;

// Runtime music controller. Two AudioSources provide 10-second crossfades.
// The gameplay does not depend on the catalog: missing groups stay silent.
public sealed class MapBgmController : MonoBehaviour
{
    private const float FadeSeconds = 10f;
    private readonly AudioSource[] sources = new AudioSource[2];
    private MapBgmCatalog catalog;
    private GameManager gameManager;
    private int activeIndex;
    private int currentGroup = -2;
    private AudioClip previousTrack;
    private bool fading;
    private float elapsed, oldVolume;
    private bool missingCatalogWarned;

    [Range(0f, 1f)] public float musicVolume = .6f;

    public static void Install(GameManager host)
    {
        if (host == null || host.GetComponentInChildren<MapBgmController>() != null) return;
        var obj = new GameObject("MapBgmController");
        obj.transform.SetParent(host.transform, false);
        obj.AddComponent<MapBgmController>().gameManager = host;
    }

    private void Awake()
    {
        for (int i = 0; i < sources.Length; i++)
        {
            sources[i] = gameObject.AddComponent<AudioSource>();
            sources[i].playOnAwake = false;
            sources[i].loop = false;
            sources[i].spatialBlend = 0f;
            sources[i].volume = 0f;
        }
    }

    // Audio themes are locked to a 5-map region, not each individual terrain
    // rolled by WorldNames.AssignMap within the region.
    public static int GroupForRegion(RegionTheme theme)
    {
        switch (theme)
        {
            case RegionTheme.BinhNguyen: return 1; // Đồng bằng
            case RegionTheme.CaoSon: return 2;     // Cao nguyên (provisional)
            case RegionTheme.UTrach: return 3;     // Đầm lầy
            case RegionTheme.SonLam: return 4;     // Rừng rậm
            case RegionTheme.HoangMac: return 5;   // Sa mạc
            default: return 0;                    // Hải vực: awaiting BGM8
        }
    }

    // -1: opening, 0: silence/unsupported region, 1..5: regional playlist.
    private int DesiredGroup()
    {
        if (gameManager == null) return 0;
        if (gameManager.preGameUI != null && gameManager.preGameUI.activeInHierarchy)
            return -1;
        if (!gameManager.IsGameplayHUDVisible || gameManager.HeroData == null)
            return 0;
        return GroupForRegion(gameManager.HeroData.regionTheme);
    }

    private AudioClip[] TracksFor(int group)
    {
        if (catalog == null) return new AudioClip[0];
        if (group == -1)
            return catalog.opening == null ? new AudioClip[0] : new [] { catalog.opening };
        return catalog.GetTracks(group);
    }

    private AudioClip PickTrack()
    {
        AudioClip[] tracks = TracksFor(currentGroup);
        if (tracks == null || tracks.Length == 0) return null;
        int valid = 0;
        for (int i = 0; i < tracks.Length; i++) if (tracks[i] != null) valid++;
        if (valid == 0) return null;
        // Avoid immediately repeating the same track when alternatives exist.
        int choices = 0;
        for (int i = 0; i < tracks.Length; i++)
            if (tracks[i] != null && (valid == 1 || tracks[i] != previousTrack))
                choices++;
        if (choices == 0) return tracks[0];
        int selected = Random.Range(0, choices);
        for (int i = 0; i < tracks.Length; i++)
        {
            if (tracks[i] == null || (valid > 1 && tracks[i] == previousTrack)) continue;
            if (selected-- == 0) return tracks[i];
        }
        return tracks[0];
    }

    private void CrossfadeToNext()
    {
        AudioSource outgoing = sources[activeIndex];
        int nextIndex = 1 - activeIndex;
        AudioSource incoming = sources[nextIndex];
        incoming.Stop();
        incoming.clip = PickTrack();
        incoming.loop = currentGroup == -1;
        incoming.volume = 0f;
        if (incoming.clip != null)
        {
            incoming.Play();
            previousTrack = incoming.clip;
        }

        // When transitioning during a previous fade, retain the currently
        // audible channel and smoothly replace the second channel.
        oldVolume = outgoing.volume;
        activeIndex = nextIndex;
        elapsed = 0f;
        fading = incoming.clip != null || outgoing.isPlaying;
    }

    private void Update()
    {
        if (catalog == null)
        {
            catalog = Resources.Load<MapBgmCatalog>("MapBgmCatalog");
            if (catalog == null)
            {
                if (!missingCatalogWarned)
                {
                    Debug.LogWarning("[BGM] MapBgmCatalog not found. Import OGG files into Assets/BGM and let Unity rebuild the catalog.");
                    missingCatalogWarned = true;
                }
                return;
            }
        }

        int desired = DesiredGroup();
        if (desired != currentGroup)
        {
            currentGroup = desired;
            previousTrack = null;
            CrossfadeToNext();
        }

        AudioSource current = sources[activeIndex];
        if (!fading && currentGroup > 0 && current.clip != null)
        {
            if (!current.isPlaying || current.clip.length - current.time <= FadeSeconds)
                CrossfadeToNext();
        }

        if (!fading) return;
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / FadeSeconds);
        AudioSource outgoingSource = sources[1 - activeIndex];
        AudioSource incomingSource = sources[activeIndex];
        outgoingSource.volume = Mathf.Lerp(oldVolume, 0f, t);
        incomingSource.volume = incomingSource.clip != null ? Mathf.Lerp(0f, musicVolume, t) : 0f;
        if (t < 1f) return;
        outgoingSource.Stop();
        outgoingSource.clip = null;
        outgoingSource.volume = 0f;
        fading = false;
    }

    private void OnDisable()
    {
        foreach (AudioSource source in sources)
            if (source != null) source.Stop();
    }
}
