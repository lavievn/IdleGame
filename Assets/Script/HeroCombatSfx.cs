using UnityEngine;
using TuTienCore;

// Presentation bus shared by registered Heroes, including B2. No combat timer,
// damage roll, projectile or animation event is driven by this component.
public sealed class HeroCombatSfx : MonoBehaviour
{
    public const int VoiceCapacity=4;
    [Range(0f,1f)] public float sfxVolume=.8f;
    [Range(0f,1f)] public float swordGain=.5f;
    [Range(0f,1f)] public float shootGain=1f;
    [Range(0f,1f)] public float magicGain=1f;
    [Range(0f,.03f)] public float pitchVariation=0f;

    private sealed class Voice
    {
        public AudioSource source;
        public HeroCombatState actor;
        public AttackMode mode;
        public int life,spawn;
        public bool paused;
    }
    private readonly Voice[] voices=new Voice[VoiceCapacity];
    private readonly AudioClip[][] groups=new AudioClip[3][];
    private readonly int[] previous={-1,-1,-1};
    // Cosmetic selection must not consume Unity Random used by damage/AI.
    private readonly System.Random random=new System.Random();
    private CombatManager host;
    private GameManager game;
    private bool initialized;
    public int StartedCount { get; private set; }
    public int DroppedCount { get; private set; }
    public AudioClip LastStartedClip { get; private set; }
    public AttackMode LastStartedMode { get; private set; }
    public int ActiveVoiceCount
    {
        get {int count=0;foreach(var voice in voices)if(voice!=null && voice.actor!=null)count++;return count;}
    }

    public static HeroCombatSfx Install(CombatManager owner)
    {
        var bus=owner.GetComponent<HeroCombatSfx>();
        if(bus==null)bus=owner.gameObject.AddComponent<HeroCombatSfx>();
        bus.Initialize(owner);return bus;
    }
    private void Initialize(CombatManager owner)
    {
        if(initialized)return;
        initialized=true;host=owner;game=owner.GetComponent<GameManager>();
        groups[0]=Resources.LoadAll<AudioClip>("CombatSfx/Sword");
        groups[1]=Resources.LoadAll<AudioClip>("CombatSfx/Shoot");
        groups[2]=Resources.LoadAll<AudioClip>("CombatSfx/Magic");
        for(int i=0;i<voices.Length;i++)
        {
            var source=gameObject.AddComponent<AudioSource>();
            source.playOnAwake=false;source.loop=false;source.spatialBlend=0f;
            source.volume=0f;source.pitch=1f;
            voices[i]=new Voice {source=source};
        }
    }
    private static int Group(AttackMode mode)
    {
        return mode==AttackMode.Melee?0:mode==AttackMode.RangedPhysical?1:mode==AttackMode.RangedMagic?2:-1;
    }
    private float Gain(AttackMode mode)
    {
        return mode==AttackMode.Melee?swordGain:mode==AttackMode.RangedPhysical?shootGain:magicGain;
    }
    public bool TryPlay(HeroCombatState actor,AttackMode mode)
    {
        if(!initialized || !isActiveAndEnabled || host==null || !host.isActiveAndEnabled ||
            Time.timeScale<=0f || actor==null || !actor.CanFight ||
            host.StateFor(actor.Controller)!=actor || actor.Controller.attackMode!=mode ||
            (game!=null && !game.IsGameplayHUDVisible))return false;
        int group=Group(mode);
        if(group<0 || groups[group]==null || groups[group].Length==0)return false;
        RefreshVoices();
        Voice available=null;
        foreach(var voice in voices)if(voice.actor==null) {available=voice;break;}
        // Do not steal/truncate another actor's transient or allocate a new
        // source. A saturated bus drops this sound, never the gameplay action.
        if(available==null) {DroppedCount++;return false;}
        var clips=groups[group];
        int index=random.Next(clips.Length>1 && previous[group]>=0?clips.Length-1:clips.Length);
        if(clips.Length>1 && previous[group]>=0 && index>=previous[group])index++;
        previous[group]=index;
        if(clips[index]==null)return false;
        available.actor=actor;available.life=actor.LifeVersion;
        available.spawn=actor.Controller.SpawnVersion;available.mode=mode;available.paused=false;
        available.source.clip=clips[index];
        float spread=Mathf.Clamp(pitchVariation,0f,.03f);
        available.source.pitch=1f+(float)(random.NextDouble()*2-1)*spread;
        ApplyVolume(available);available.source.Play();
        StartedCount++;LastStartedClip=clips[index];LastStartedMode=mode;
        return true;
    }
    private void ApplyVolume(Voice voice)
    {
        // Fixed per-voice headroom: total gain never exceeds the configured bus
        // volume, even with simultaneous B2 attacks. No per-frame normalization.
        voice.source.volume=Mathf.Clamp01(sfxVolume)*AudioVolumeSettings.Get(GameAudioChannel.Sfx)*
            Mathf.Clamp01(Gain(voice.mode))/VoiceCapacity;
    }
    private static void Stop(Voice voice)
    {
        if(voice.source!=null) {voice.source.Stop();voice.source.clip=null;voice.source.volume=0f;}
        voice.actor=null;voice.paused=false;
    }
    private void RefreshVoices()
    {
        foreach(var voice in voices)
        {
            if(voice==null || voice.actor==null)continue;
            var actor=voice.actor;
            if(host==null || !host.isActiveAndEnabled || !actor.CanFight ||
                host.StateFor(actor.Controller)!=actor || actor.LifeVersion!=voice.life ||
                actor.Controller.SpawnVersion!=voice.spawn || actor.Controller.attackMode!=voice.mode ||
                (game!=null && (game.IsEnteringGameplay ||
                    (game.preGameUI!=null && game.preGameUI.activeInHierarchy))))
            {Stop(voice);continue;}
            if(Time.timeScale<=0f)
            {
                if(!voice.paused) {voice.source.Pause();voice.paused=true;}
            }
            else
            {
                if(voice.paused) {voice.source.UnPause();voice.paused=false;}
                if(!voice.source.isPlaying) {Stop(voice);continue;}
            }
            ApplyVolume(voice);
        }
    }
    public void StopHero(HeroController hero)
    {
        foreach(var voice in voices)
            if(voice!=null && voice.actor!=null && voice.actor.Controller==hero)Stop(voice);
    }
    public void StopAll()
    {
        foreach(var voice in voices)if(voice!=null)Stop(voice);
    }
    private void Update() {RefreshVoices();}
    private void OnDisable() {StopAll();}
}
