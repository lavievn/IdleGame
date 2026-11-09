using UnityEngine;
using TuTienCore;

// Runtime music controller. Two AudioSources cover title, regional, and trial crossfades.
// The gameplay does not depend on the catalog: missing groups stay silent.
public sealed class MapBgmController : MonoBehaviour
{
    private const float TransitionSeconds = 1.25f;
    private const float PlaylistFadeSeconds = 10f;
    private const int TrialGroup = -3;
    private readonly AudioSource[] sources = new AudioSource[2];
    private MapBgmCatalog catalog;
    private GameManager gameManager;
    private int activeIndex;
    private int currentGroup = -2;
    private AudioClip previousTrack;
    private bool fading;
    private float elapsed, fadeDuration;
    private bool missingCatalogWarned;
    private AudioSource trialSource;
    private AudioClip[] trialTracks;
    // Choosing music must not advance Unity's combat random sequence.
    private System.Random trialTrackRandom = new System.Random();
    private bool trialMusicActive;
    private AudioClip trialClip;
    private readonly System.Random playlistRandom = new System.Random();

    public static MapBgmController Instance { get; private set; }
    [Range(0f, 1f)] public float musicVolume = .6f;

    // The volume is global for opening and all map playlists. Keep it outside
    // save slots so New Game / Load Game do not silently reset audio settings.
    public static float CurrentVolume => AudioVolumeSettings.Get(GameAudioChannel.Bgm);

    // Update continuously when the user drags, but flush preferences only
    // on release. Avoid disk I/O on every input frame.
    public static void PreviewVolume(float value)
    {
        value = Mathf.Clamp01(value);
        AudioVolumeSettings.Preview(GameAudioChannel.Bgm,value);
        if (Instance != null) Instance.musicVolume = CurrentVolume;
    }

    public static void SaveVolumePreference()
    {
        AudioVolumeSettings.Save();
    }

    public static void SetVolume(float value)
    {
        PreviewVolume(value);
        SaveVolumePreference();
    }

    public static void Install(GameManager host)
    {
        if (host == null || host.GetComponentInChildren<MapBgmController>() != null) return;
        var obj = new GameObject("MapBgmController");
        obj.transform.SetParent(host.transform, false);
        obj.AddComponent<MapBgmController>().gameManager = host;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        musicVolume = CurrentVolumeFromPreferences();
        for (int i = 0; i < sources.Length; i++)
        {
            sources[i] = gameObject.AddComponent<AudioSource>();
            sources[i].playOnAwake = false;
            sources[i].loop = false;
            sources[i].spatialBlend = 0f;
            sources[i].volume = 0f;
        }
    }

    private static float CurrentVolumeFromPreferences()
    {
        return CurrentVolume;
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
        // Keep the regional theme during entry, confirmation and Retry.
        if (gameManager.HeroData == null)
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
        int selected = playlistRandom.Next(choices);
        for (int i = 0; i < tracks.Length; i++)
        {
            if (tracks[i] == null || (valid > 1 && tracks[i] == previousTrack)) continue;
            if (selected-- == 0) return tracks[i];
        }
        return tracks[0];
    }

    private void CrossfadeToNext(float duration)
    {
        int nextIndex = 1 - activeIndex;
        var incoming = sources[nextIndex]; // Previous outgoing channel was cleared after its fade.
        incoming.clip = currentGroup == TrialGroup ? trialClip : PickTrack();
        incoming.loop = currentGroup == -1 || currentGroup == TrialGroup;
        incoming.volume = 0f;
        if (currentGroup == TrialGroup) trialSource = incoming;
        if (incoming.clip != null)
        {
            incoming.Play();
            previousTrack = incoming.clip;
        }
        fading = incoming.clip != null || sources[activeIndex].isPlaying;
        activeIndex = nextIndex;
        elapsed = 0f;
        fadeDuration = duration;
    }

    private void Update()
    {
        musicVolume = CurrentVolume;
        var combat = CombatManager.Instance;
        bool inTrial = gameManager != null && gameManager.HasTribulationLiability &&
            combat != null && combat.HeavenlyGateActive;
        if (inTrial && !trialMusicActive) trialClip = ChooseTrialMusic();
        trialMusicActive = inTrial;
        if (catalog == null && !missingCatalogWarned)
        {
            catalog = Resources.Load<MapBgmCatalog>("MapBgmCatalog");
            if (catalog == null) { missingCatalogWarned = true; Debug.LogWarning("[BGM] MapBgmCatalog missing."); }
        }

        int desired = inTrial ? TrialGroup : DesiredGroup();
        // Finish the current fade with both sources intact. On completion
        // observe the latest state, so obsolete requests are never replayed.
        if (fading)
        {
            if (desired != currentGroup && fadeDuration > TransitionSeconds)
            {
                elapsed *= TransitionSeconds / fadeDuration;
                fadeDuration = TransitionSeconds;
            }
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            var outgoing = sources[1 - activeIndex];
            outgoing.volume = (1f - t) * musicVolume;
            sources[activeIndex].volume = t * musicVolume;
            if (t < 1f) return;
            outgoing.Stop();
            outgoing.clip = null;
            outgoing.volume = 0f;
            fading = false;
        }

        if (desired != currentGroup)
        {
            currentGroup = desired;
            previousTrack = null;
            CrossfadeToNext(TransitionSeconds);
            return;
        }
        var current = sources[activeIndex];
        current.volume = current.isPlaying ? musicVolume : 0f;
        if (currentGroup > 0 && current.clip != null &&
            (!current.isPlaying || current.clip.length - current.time <= PlaylistFadeSeconds))
            CrossfadeToNext(PlaylistFadeSeconds);
    }

    private AudioClip ChooseTrialMusic()
    {
        if (trialTracks == null)
            trialTracks = new[] {
                Resources.Load<AudioClip>("Environment/Tribulation/HeavenlyGateTheme"),
                Resources.Load<AudioClip>("Environment/Tribulation/HeavenlyGateThemeAlternate")
            };
        return trialTracks[trialTrackRandom.Next(trialTracks.Length)] ?? trialTracks[0] ?? trialTracks[1];
    }

    private void OnDisable()
    {
        foreach (var source in sources)
        {
            if (source == null) continue;
            source.Stop();
            source.clip = null;
            source.volume = 0f;
        }
        currentGroup = -2;
        trialMusicActive = fading = false;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
