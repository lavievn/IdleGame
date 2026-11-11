using UnityEngine;

public enum GameAudioChannel { Bgm, Sfx, Environment }

// One persisted source of truth for both UIs and both runtime buses. Reuse
// the original BGM key/value verbatim; old saves and playlists are unaffected.
public static class AudioVolumeSettings
{
    public const float DefaultBgm=.6f,DefaultSfx=1f,DefaultEnvironment=1f;
    public static string Key(GameAudioChannel channel)
    {
        switch (channel)
        {
            case GameAudioChannel.Bgm: return "IdleGame.BgmVolume";
            case GameAudioChannel.Environment: return "IdleGame.EnvironmentVolume";
            default: return "IdleGame.SfxVolume";
        }
    }
    public static float Get(GameAudioChannel channel)
    {
        float fallback=channel==GameAudioChannel.Bgm?DefaultBgm:
            channel==GameAudioChannel.Environment?DefaultEnvironment:DefaultSfx;
        float value=PlayerPrefs.GetFloat(Key(channel),fallback);
        return float.IsNaN(value)||float.IsInfinity(value)?fallback:Mathf.Clamp01(value);
    }
    public static void Preview(GameAudioChannel channel,float value)
    {
        if(float.IsNaN(value)||float.IsInfinity(value))return;
        PlayerPrefs.SetFloat(Key(channel),Mathf.Clamp01(value));
    }
    public static void Set(GameAudioChannel channel,float value) {Preview(channel,value);Save();}
    public static void Save() {PlayerPrefs.Save();}
    public static void RestoreDefaults()
    {
        Preview(GameAudioChannel.Bgm,DefaultBgm);
        Preview(GameAudioChannel.Sfx,DefaultSfx);
        Preview(GameAudioChannel.Environment,DefaultEnvironment);
        Save();
    }
}
