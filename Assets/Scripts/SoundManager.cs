using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;
    public static SoundManager Instance => instance;

    [SerializeField] private Transform soundParent;
    public List<Sound> sounds;
    public Transform localPlayerPosition;
    public List<string> themeList;
    public string currentTheme;
    public float maxDistance = 40f;
    public float distanceMultiplier = 1f;
    public float blendValue = 0.75f;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if(soundParent == null)
        {
            soundParent = new GameObject().transform;
            soundParent.parent = transform;
            soundParent.position = transform.position;
            soundParent.gameObject.name = "Sounds";
        }

        foreach (Sound sound in sounds)
        {
            GameObject newObject = new();
            newObject.transform.parent = soundParent;
            newObject.transform.position = transform.position;
            newObject.name = sound.name;
            sound.source = newObject.AddComponent<AudioSource>();
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

    public void SetRotation(float angleY)
    {
        soundParent.rotation = Quaternion.Euler(0, angleY, 0);
    }

    public void Play(string name)
    {
        foreach (Sound sound in sounds)
        {
            if (sound.name == name)
            {
                //Debug.Log("found " + name);
                sound.source.transform.localPosition = Vector3.zero;
                sound.source.volume = sound.volume;
                sound.source.spatialBlend = 0;
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
        //float scale = (maxDistance - distance)/maxDistance;
        Vector3 displacement = sourceLocation - localPlayerPosition.position;
        foreach (Sound sound in sounds)
        {
            if (sound.name == name)
            {
                //Debug.Log("found " + name);
                sound.source.transform.localPosition = displacement * distanceMultiplier;
                //sound.source.volume = sound.volume * scale;
                sound.source.volume = sound.volume;
                sound.source.spatialBlend = blendValue;
                sound.source.Play();
                return;
            }
        }
    }

    public void PlayNew(string name, Vector3 sourceLocation)
    {
        if (GameManager.instance.GetGameState() == GameState.MENU || localPlayerPosition == null)
        {
            Play(name);
            return;
        }
        float distance = Vector3.Distance(localPlayerPosition.position, sourceLocation);
        if (distance >= maxDistance) return;
        float scale = (maxDistance - distance) / maxDistance;
        Vector3 displacement = sourceLocation - localPlayerPosition.position;
        foreach (Sound sound in sounds)
        {
            if (sound.name == name)
            {
                if(sound.source.isPlaying)
                {
                    PlayNew(sound, 1, scale,displacement);
                    return;
                }
                //Debug.Log("found " + name);
                sound.source.transform.localPosition = displacement * distanceMultiplier;
                //sound.source.volume = sound.volume * scale;
                sound.source.volume = sound.volume;
                sound.source.spatialBlend = blendValue;
                sound.source.Play();
                return;
            }
        }
    }

    private void PlayNew(Sound sound,int count,float scale,Vector3 displacement)
    {
        string newName = sound.name + count;
        foreach (Sound s in sounds)
        {
            if (s.name == newName)
            {
                if (s.source.isPlaying)
                {
                    PlayNew(s, count+1, scale,displacement);
                    return;
                }
                //Debug.Log("found " + newName);
                sound.source.transform.localPosition = displacement * distanceMultiplier;
                //s.source.volume = s.volume * scale;
                s.source.volume = s.volume;
                sound.source.spatialBlend = blendValue;
                s.source.Play();
                return;
            }
        }
        //Debug.Log("create " + newName);
        GameObject newObject = new();
        newObject.transform.parent = soundParent;
        newObject.transform.position = transform.position;
        newObject.name = newName;
        Sound newSound = new(newName, sound.clip, sound.volume, sound.pitch, sound.isLoop)
        {
            source = newObject.AddComponent<AudioSource>()
        };
        newSound.source.clip = newSound.clip;
        newSound.source.pitch = newSound.pitch;
        newSound.source.loop = newSound.isLoop;
        newSound.source.spatialBlend = 0;

        sounds.Add(newSound);

        newSound.source.transform.localPosition = displacement * distanceMultiplier;
        //newSound.source.volume = newSound.volume * scale;
        newSound.source.volume = newSound.volume;
        sound.source.spatialBlend = blendValue;
        newSound.source.Play();
    }

    public void Stop(string name)
    {
        foreach (Sound sound in sounds)
        {
            if (sound.name == name)
            {
                //Debug.Log("found " + name);
                sound.source.Stop();
                return;
            }
        }
    }

    public void PlayTheme(string name)
    {
        if (name == currentTheme) return;
        ushort count = 0;
        foreach (Sound sound in sounds)
        {
            if (sound.name == currentTheme)
            {
                //Debug.Log("found " + currentTheme);
                sound.source.Stop();
                count++;
            }
            if (sound.name == name)
            {
                //Debug.Log("found " + name);
                sound.source.spatialBlend = 0;
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
