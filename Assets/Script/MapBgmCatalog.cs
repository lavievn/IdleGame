using UnityEngine;

// Editor-generated catalog. Keeps files in Assets/BGM while ensuring Unity
// includes referenced clips in Windows builds via Resources/MapBgmCatalog.asset.
public sealed class MapBgmCatalog : ScriptableObject
{
    public AudioClip opening;
    public AudioClip[] bgm1 = new AudioClip[0]; // Đồng bằng
    public AudioClip[] bgm2 = new AudioClip[0]; // Cao nguyên
    public AudioClip[] bgm3 = new AudioClip[0]; // Đầm lầy
    public AudioClip[] bgm4 = new AudioClip[0]; // Rừng rậm
    public AudioClip[] bgm5 = new AudioClip[0]; // Sa mạc

    public AudioClip[] GetTracks(int group)
    {
        switch (group)
        {
            case 1: return bgm1;
            case 2: return bgm2;
            case 3: return bgm3;
            case 4: return bgm4;
            case 5: return bgm5;
            default: return new AudioClip[0];
        }
    }
}
