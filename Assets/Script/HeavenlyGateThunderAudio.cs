using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class HeavenlyGateThunderAudio
{
    private readonly Queue<float> due = new Queue<float>();
    private AudioSource source;
    private AudioClip defaultClip;
    private bool resolved;
    private float clock;
    private long lastStrike;
    private Guid encounter;
    internal int Played { get; private set; }
    internal int Pending => due.Count;

    internal void QueueStrike(GameObject owner, Guid id, long strike, float delay, float volume, AudioClip clip)
    {
        if (id == Guid.Empty) return;
        if (encounter != id)
        {
            Clear();
            encounter = id;
        }
        if (strike <= lastStrike) return;
        lastStrike = strike;
        due.Enqueue(clock + Mathf.Max(0f, delay));
        Advance(owner, 0f, volume, clip);
    }

    internal void Advance(GameObject owner, float dt, float volume, AudioClip clip)
    {
        clock += Mathf.Max(0f, dt);
        while (due.Count > 0 && due.Peek() <= clock)
        {
            due.Dequeue();
            float gain = Mathf.Clamp01(volume) * AudioVolumeSettings.Get(GameAudioChannel.Environment);
            if (gain <= 0f) continue;
            if (clip == null && !resolved)
            {
                defaultClip = Resources.Load<AudioClip>("IntegratedAmbience/GateThunder");
                resolved = true;
            }
            var sound = clip != null ? clip : defaultClip;
            if (sound == null) continue;
            if (source == null)
            {
                source = owner.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
            }
            source.PlayOneShot(sound, gain);
            Played++;
        }
    }

    internal void Clear()
    {
        if (source != null) source.Stop();
        due.Clear();
        clock = 0f;
        lastStrike = -1;
        encounter = Guid.Empty;
    }
}
