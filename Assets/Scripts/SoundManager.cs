using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;
    public static SoundManager Instance => instance;

    public List<Sound> sounds;
    public Transform localPlayer;
    public float maxDistance = 12f;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        foreach (Sound sound in sounds)
        {
            sound.source = gameObject.AddComponent<AudioSource>();
            sound.source.clip = sound.clip;

            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;
            sound.source.loop = sound.isLoop;
            sound.source.spatialBlend = 0;
        }
    }

    public void Play(string name)
    {
        foreach (Sound sound in sounds)
        {
            if (sound.name == name)
            {
                sound.source.volume = sound.volume;
                sound.source.Play();
                return;
            }
        }
    }

    public void Play(string name, Vector3 sourceLocation)
    {
        if (GameManager.instance.GetGameState() == GameState.MENU || localPlayer == null)
        {
            Play(name);
            return;
        }
        float distance = Vector3.Distance(localPlayer.transform.position, sourceLocation);
        if (distance >= maxDistance) return;
        float scale = (maxDistance - distance)/maxDistance;
        foreach (Sound sound in sounds)
        {
            if (sound.name == name)
            {
                sound.source.volume = sound.volume * scale;
                sound.source.Play();
                return;
            }
        }
    }

    public void Stop(string name)
    {
        foreach (Sound sound in sounds)
        {
            if (sound.name == name)
            {
                sound.source.Stop();
                return;
            }
        }
    }
}
