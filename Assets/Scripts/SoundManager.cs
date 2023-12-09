using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;
    public static SoundManager Instance => instance;

    public List<Sound> sounds;
    public Transform localPlayerPosition;
    public List<string> themeList;
    public string currentTheme;
    public float maxDistance = 20f;

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

    private void Start()
    {
        Play("menu");
        currentTheme = "menu";
    }

    public void Play(string name)
    {
        foreach (Sound sound in sounds)
        {
            if (sound.name == name)
            {
                Debug.Log("found " + name);
                sound.source.volume = sound.volume;
                sound.source.Play();
                return;
            }
        }
    }

    public void Play(string name, Vector3 sourceLocation)
    {
        if (GameManager.instance.GetGameState() == GameState.MENU || localPlayerPosition == null)
        {
            Play(name);
            return;
        }
        float distance = Vector3.Distance(localPlayerPosition.position, sourceLocation);
        if (distance >= maxDistance) return;
        float scale = (maxDistance - distance)/maxDistance;
        foreach (Sound sound in sounds)
        {
            if (sound.name == name)
            {
                Debug.Log("found " + name);
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
                Debug.Log("found " + name);
                sound.source.Stop();
                return;
            }
        }
    }

    public void PlayTheme(string name)
    {
        ushort count = 0;
        foreach (Sound sound in sounds)
        {
            if (sound.name == currentTheme)
            {
                Debug.Log("found " + currentTheme);
                sound.source.Stop();
                count++;
            }
            if (sound.name == name)
            {
                Debug.Log("found " + name);
                sound.source.Play();
                count++;
            }
            if (count == 2)
            {
                break;
            }
        }
        currentTheme = name;
    }
}
