using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;
    //public static SoundManager Instance => instance;

    public List<Sound> soundList;

    [Header("Sound Settings")]

    [Tooltip("Max Distance Between Sound and Inspecting Player to Play Sound Using [PlayNew]")]
    [SerializeField] private float maxDistance = 40f;

    [Tooltip("Make Sound Feel More Distance.")]
    [SerializeField] private float distanceMultiplier = 2f;

    [Tooltip("Make Sound Feel More 3D. (Value is between 0 and 1)")]
    [SerializeField] private float blendValue = 0.75f;

    [Header("Debug Properties")] // Add [SerializeField] to Debug
    [SerializeField] private float sfxVolume = 1f;
    [SerializeField] private float musicVolume = 1f;
    [SerializeField] private Transform inspectingPlayerPosition;
    [SerializeField] private bool isSfxEnable = true;
    private Transform soundParent;
    private string currentMusicName;
    private Sound currentMusic;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        InitSoundList();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void InitSoundList()
    {
        // Create Sound Parent
        if (soundParent == null)
        {
            soundParent = new GameObject().transform;
            soundParent.parent = transform;
            soundParent.position = transform.position;
            soundParent.gameObject.name = "Sounds";
        }

        // Create Audio Source from SoundList
        foreach (Sound sound in soundList)
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("SoundManager: OnSceneLoaded");
        if (scene.buildIndex == 0)
        {
            PlayMusicPrivate("menu");
        }
    }

    private Sound DuplicateSound(Sound sound, string newName)
    {
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

        soundList.Add(newSound);

        return newSound;
    }

    private Sound FindSound(string name)
    {
        foreach (Sound sound in soundList)
        {
            if (sound.name == name)
            {
                return sound;
            }
        }
        return null;
    }

    private static bool CheckInstanceIsNull()
    {
        if (instance == null)
        {
            Debug.Log("No SoundManager Instance Yet.");
            return true;
        }
        return false;
    }

    private void PlaySound(Sound sound, float volumeScale)
    {
        sound.source.transform.localPosition = Vector3.zero;
        sound.source.volume = sound.volume * volumeScale;
        sound.source.spatialBlend = 0;
        //sound.source.loop = false;
        sound.source.Play();
    }

    private void PlaySound3D(Sound sound, float volumeScale, Vector3 relativePosition)
    {
        sound.source.transform.localPosition = relativePosition * distanceMultiplier;
        sound.source.volume = sound.volume * volumeScale;
        sound.source.spatialBlend = blendValue;
        //sound.source.loop = false;
        sound.source.Play();
    }

    public static void Play(string name)
    {
        if (CheckInstanceIsNull()) return;
        if (!instance.isSfxEnable) return;
        instance.PlayPrivate(name);
    }

    public void PlayPrivate(string name)
    {
        Sound sound = FindSound(name);
        if (sound != null)
        {
            PlaySound(sound, sfxVolume);
        }
    }

    public static void Play(string name, Vector3 sourceLocation)
    {
        if (CheckInstanceIsNull()) return;
        if (!instance.isSfxEnable) return;
        instance.PlayPrivate(name, sourceLocation);
    }

    public void PlayPrivate(string name, Vector3 sourceLocation)
    {
        if (GameDataManager.Instance.GetGameState() == GameState.MENU || inspectingPlayerPosition == null)
        {
            Play(name);
            return;
        }

        float distance = Vector3.Distance(inspectingPlayerPosition.position, sourceLocation);
        if (distance >= maxDistance) return;
        Vector3 displacement = sourceLocation - inspectingPlayerPosition.position;

        Sound sound = FindSound(name);
        if (sound != null)
        {
            PlaySound3D(sound, sfxVolume, displacement);
        }
    }

    public static void PlayNew(string name, Vector3 sourceLocation)
    {
        if (CheckInstanceIsNull()) return;
        if (!instance.isSfxEnable) return;
        instance.PlayNewPrivate(name, 1f, sourceLocation);
    }

    public static void PlayNew(string name, float scale, Vector3 sourceLocation)
    {
        if (CheckInstanceIsNull()) return;
        if (!instance.isSfxEnable) return;
        instance.PlayNewPrivate(name, scale, sourceLocation);
    }

    private void PlayNewPrivate(string name, float scale, Vector3 sourceLocation)
    {
        if (GameDataManager.Instance.GetGameState() == GameState.MENU || inspectingPlayerPosition == null)
        {
            Play(name);
            return;
        }

        float distance = Vector3.Distance(inspectingPlayerPosition.position, sourceLocation);
        if (distance >= maxDistance) return;
        Vector3 displacement = sourceLocation - inspectingPlayerPosition.position;

        Sound sound = FindSound(name);
        if (sound != null)
        {
            if (sound.source.isPlaying)
            {
                PlayNewPrivate(sound, sound.name, 1, scale, displacement);
                return;
            }
            PlaySound3D(sound, sfxVolume * scale, displacement);
        }
    }

    private void PlayNewPrivate(Sound sound, string name, int count, float scale, Vector3 displacement)
    {
        string newName = name + count;

        Sound s = FindSound(newName);
        if (s != null)
        {
            if (s.source.isPlaying)
            {
                PlayNewPrivate(s, name, count + 1, scale, displacement);
                return;
            }
            PlaySound3D(s, sfxVolume * scale, displacement);
        }
        else
        {
            Sound newSound = DuplicateSound(sound, newName);
            PlaySound3D(newSound, sfxVolume * scale, displacement);
        }
    }

    public static void Stop(string name)
    {
        if (CheckInstanceIsNull()) return;
        instance.StopPrivate(name);
    }

    private void StopPrivate(string name)
    {
        Sound sound = FindSound(name);
        if (sound != null)
        {
            sound.source.Stop();
        }
    }

    public static void SetSfxEnable(bool isEnable)
    {
        if (CheckInstanceIsNull()) return;
        instance.isSfxEnable = isEnable;
    }

    public static void PlayMusic(string name)
    {
        if (CheckInstanceIsNull()) return;
        instance.PlayMusicPrivate(name);
    }

    private void PlayMusicPrivate(string name)
    {
        if (name == currentMusicName) return;
        byte count = 0;
        foreach (Sound sound in soundList)
        {
            if (sound.name == currentMusicName)
            {
                sound.source.Stop();
                count++;
            }
            if (sound.name == name)
            {
                PlaySound(sound, musicVolume);
                currentMusic = sound;
                count++;
            }
            if (count == 2)
            {
                break;
            }
        }
        currentMusicName = name;
    }

    public static string GetCurrentMusicName()
    {
        if (CheckInstanceIsNull()) return string.Empty;
        return instance.currentMusicName;
    }

    public static void SetSoundAngle(float angleY)
    {
        if (CheckInstanceIsNull()) return;
        instance.soundParent.rotation = Quaternion.Euler(0, angleY, 0);
    }

    public static void SetInspectingPlayer(Transform playerModelTransform)
    {
        if (CheckInstanceIsNull()) return;
        instance.inspectingPlayerPosition = playerModelTransform;
    }

    public static void SetSfxVolume(float sfxVolume)
    {
        if (CheckInstanceIsNull()) return;
        instance.sfxVolume = sfxVolume;
    }

    public static void SetMusicVolume(float musicVolume)
    {
        if (CheckInstanceIsNull()) return;
        instance.musicVolume = musicVolume;
        instance.currentMusic.source.volume = instance.currentMusic.volume * musicVolume;
    }

    public static float GetSfxVolume()
    {
        if (CheckInstanceIsNull()) return 1f;
        return instance.sfxVolume;
    }

    public static float GetMusicVolume()
    {
        if (CheckInstanceIsNull()) return 1f;
        return instance.musicVolume;
    }

}
