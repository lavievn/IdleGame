using System;
using UnityEngine;
using TuTienCore;

internal enum CombatAudioEvent { Basic, Cast, Release, Projectile, Impact, Death }

[Serializable]
public sealed class CombatAudioProfile
{
    public AudioClip basic, cast, release, projectile, impact, death;

    internal AudioClip Clip(CombatAudioEvent kind)
    {
        switch (kind)
        {
            case CombatAudioEvent.Basic: return basic;
            case CombatAudioEvent.Cast: return cast;
            case CombatAudioEvent.Release: return release;
            case CombatAudioEvent.Projectile: return projectile;
            case CombatAudioEvent.Impact: return impact;
            default: return death;
        }
    }
}

// Name retained for existing scene/tests. Heroes and monsters share a bounded
// event bus with reserved voices so a wave cannot steal Hero feedback.
public sealed class HeroCombatSfx : MonoBehaviour
{
    public const int VoiceCapacity = 4;
    public const int MonsterVoiceCapacity = 3;
    [Range(0f,1f)] public float sfxVolume=.8f;
    [Range(0f,1f)] public float swordGain=.5f;
    [Range(0f,1f)] public float shootGain=1f;
    [Range(0f,1f)] public float magicGain=1f;
    [Range(0f,.03f)] public float pitchVariation=0f;

    sealed class Voice
    {
        public AudioSource source;
        public HeroCombatState hero;
        public MonsterController monster;
        public AttackMode mode;
        public CombatAudioEvent kind;
        public int life, spawn;
        public bool active, paused;
    }

    readonly Voice[] voices = new Voice[VoiceCapacity + MonsterVoiceCapacity];
    readonly AudioClip[][] groups = new AudioClip[3][];
    readonly int[] previous = {-1,-1,-1};
    readonly System.Random random = new System.Random();
    CombatManager host;
    GameManager game;
    bool initialized;
    public int StartedCount { get; private set; }
    internal int MonsterStartedCount { get; private set; }
    public int DroppedCount { get; private set; }
    public AudioClip LastStartedClip { get; private set; }
    public AttackMode LastStartedMode { get; private set; }
    public int ActiveVoiceCount
    {
        get { int n=0; foreach (var v in voices) if (v != null && v.active) n++; return n; }
    }

    public static HeroCombatSfx Install(CombatManager owner)
    {
        var bus=owner.GetComponent<HeroCombatSfx>();
        if(bus==null)bus=owner.gameObject.AddComponent<HeroCombatSfx>();
        bus.Initialize(owner);
        return bus;
    }

    void Initialize(CombatManager owner)
    {
        if (initialized) return;
        initialized=true; host=owner; game=owner.GetComponent<GameManager>();
        groups[0]=Resources.LoadAll<AudioClip>("CombatSfx/Sword");
        groups[1]=Resources.LoadAll<AudioClip>("CombatSfx/Shoot");
        groups[2]=Resources.LoadAll<AudioClip>("CombatSfx/Magic");
        for (int i=0; i<voices.Length; i++)
        {
            var source=gameObject.AddComponent<AudioSource>();
            source.playOnAwake=false; source.loop=false; source.spatialBlend=0f; source.volume=0f;
            voices[i]=new Voice { source=source };
        }
    }

    public bool TryPlay(HeroCombatState actor, AttackMode mode) => PlayHero(actor, mode, CombatAudioEvent.Basic);

    internal bool PlayHero(HeroCombatState actor, AttackMode mode, CombatAudioEvent kind)
    {
        if (actor == null || actor.Controller == null || host == null || host.StateFor(actor.Controller)!=actor ||
            (!actor.CanFight && kind != CombatAudioEvent.Death) || actor.Controller.attackMode != mode) return false;
        return Play(actor, null, mode, kind, actor.Controller.AudioProfile);
    }

    internal bool PlayMonster(MonsterController actor, AttackMode mode, CombatAudioEvent kind)
    {
        if (actor == null || !actor.IsAlive || actor.IsHeavenlyGate) return false;
        return Play(null, actor, mode, kind, actor.AudioProfile);
    }

    bool Play(HeroCombatState hero, MonsterController monster, AttackMode mode, CombatAudioEvent kind, CombatAudioProfile profile)
    {
        if (!initialized || !isActiveAndEnabled || host == null || !host.isActiveAndEnabled ||
            Time.timeScale <= 0f || game != null && !game.IsGameplayHUDVisible) return false;
        var clip = profile?.Clip(kind);
        // Existing Hero impacts are silent unless authored. Keep the established
        // attack cue and give unconfigured monsters appropriate fallback sounds.
        if (clip == null && hero != null && (kind == CombatAudioEvent.Impact || kind == CombatAudioEvent.Death)) return false;
        if (clip == null) clip = Pick(mode);
        if (clip == null) return false;
        RefreshVoices();
        int from = monster == null ? 0 : VoiceCapacity;
        int end = monster == null ? VoiceCapacity : voices.Length;
        for (int i = from; i < end; i++)
        {
            var voice = voices[i];
            if (voice.active) continue;
            voice.active = true; voice.mode = mode; voice.kind = kind;
            // A terminal cue is a detached sound, with no callback to a pooled life.
            voice.hero = kind == CombatAudioEvent.Death ? null : hero;
            voice.monster = kind == CombatAudioEvent.Death ? null : monster;
            voice.life = hero?.LifeVersion ?? 0;
            voice.spawn = hero != null ? hero.Controller.SpawnVersion : monster.SpawnVersion;
            voice.source.clip=clip;
            float spread=Mathf.Clamp(pitchVariation,0f,.03f);
            voice.source.pitch=1f+(float)(random.NextDouble()*2-1)*spread;
            ApplyVolume(voice, i < VoiceCapacity);
            voice.source.Play();
            StartedCount++;
            if (monster != null) MonsterStartedCount++;
            LastStartedClip=clip; LastStartedMode=mode;
            return true;
        }
        DroppedCount++;
        return false;
    }

    AudioClip Pick(AttackMode mode)
    {
        int group=mode==AttackMode.Melee?0:mode==AttackMode.RangedPhysical?1:2;
        var clips=groups[group];
        if (clips == null || clips.Length == 0) return null;
        int index=random.Next(clips.Length>1 && previous[group]>=0?clips.Length-1:clips.Length);
        if(clips.Length>1 && previous[group]>=0 && index>=previous[group])index++;
        previous[group]=index;
        return clips[index];
    }

    void ApplyVolume(Voice voice, bool hero)
    {
        float gain=voice.mode==AttackMode.Melee?swordGain:voice.mode==AttackMode.RangedPhysical?shootGain:magicGain;
        float eventGain=voice.kind==CombatAudioEvent.Cast?.26f:voice.kind==CombatAudioEvent.Impact?.30f:
            voice.kind==CombatAudioEvent.Death?.45f:1f;
        voice.source.volume=Mathf.Clamp01(sfxVolume)*AudioVolumeSettings.Get(GameAudioChannel.Sfx)*
            Mathf.Clamp01(gain)*eventGain*(hero?1f/VoiceCapacity:.18f/MonsterVoiceCapacity);
    }

    static void Stop(Voice voice)
    {
        if(voice.source!=null) {voice.source.Stop();voice.source.clip=null;voice.source.volume=0f;}
        voice.hero=null;voice.monster=null;voice.active=voice.paused=false;
    }

    void RefreshVoices()
    {
        for (int i=0; i<voices.Length; i++)
        {
            var voice=voices[i];
            if (voice==null || !voice.active) continue;
            var hero=voice.hero;
            var monster=voice.monster;
            if (host == null || !host.isActiveAndEnabled) { Stop(voice); continue; }
            bool deadSource = voice.kind != CombatAudioEvent.Death &&
                (hero != null ? !hero.CanFight || host.StateFor(hero.Controller)!=hero ||
                    hero.LifeVersion!=voice.life || hero.Controller.SpawnVersion!=voice.spawn ||
                    hero.Controller.attackMode!=voice.mode :
                    monster == null || !monster.IsAlive || monster.SpawnVersion!=voice.spawn || monster.attackMode!=voice.mode);
            if (deadSource || game!=null && (game.IsEnteringGameplay ||
                    game.preGameUI!=null && game.preGameUI.activeInHierarchy))
            { Stop(voice); continue; }
            if (Time.timeScale<=0f)
            {
                if(!voice.paused) {voice.source.Pause();voice.paused=true;}
            }
            else
            {
                if(voice.paused) {voice.source.UnPause();voice.paused=false;}
                if(!voice.source.isPlaying) {Stop(voice);continue;}
            }
            ApplyVolume(voice, i < VoiceCapacity);
        }
    }

    internal void StopCast(RectTransform rect)
    {
        foreach (var voice in voices)
            if (voice != null && voice.active && voice.kind == CombatAudioEvent.Cast &&
                (voice.hero?.Controller?.heroRect == rect || voice.monster != null && voice.monster.Rect == rect))
                Stop(voice);
    }

    public void StopHero(HeroController hero)
    {
        foreach(var voice in voices)
            if(voice!=null && voice.hero!=null && voice.hero.Controller==hero)Stop(voice);
    }

    public void StopAll()
    {
        foreach(var voice in voices)if(voice!=null)Stop(voice);
    }

    void Update() { RefreshVoices(); }
    void OnDisable() { StopAll(); }
}
