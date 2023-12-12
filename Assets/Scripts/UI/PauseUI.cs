using UnityEngine;
using UnityEngine.UI;

public class PauseUI : MonoBehaviour
{
    [Header("Pause")]
    [SerializeField] private Canvas pauseCanvas;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Button backToMenuBtn;
    [SerializeField] private Button quitGameBtn;

    private void Awake()
    {
        InitButton();
        InitSlider();
    }

    void InitButton()
    {
        backToMenuBtn.onClick.AddListener(() => {
            SoundManager.Play("select");
            NetworkManagerUI.instance.BackToMenu();
        });
        quitGameBtn.onClick.AddListener(() => {
            Application.Quit();
            //Subscribed OnApplicationQuit
        });
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            if (GameManager.instance.GetGameState() != GameState.MENU)
            {
                TogglePause();
            }
        }
    }

    private void TogglePause()
    {
        if (pauseCanvas.gameObject.activeSelf)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
        pauseCanvas.gameObject.SetActive(!pauseCanvas.gameObject.activeSelf);
    }

    public void InitSlider()
    {
        sfxSlider.value = SoundManager.GetSfxVolume();
        musicSlider.value = SoundManager.GetMusicVolume();
        sfxSlider.onValueChanged.AddListener(delegate { SfxVolumeChanged(); });
        musicSlider.onValueChanged.AddListener(delegate { MusicVolumeChanged(); });
    }

    private void SfxVolumeChanged()
    {
        SoundManager.SetSfxVolume(sfxSlider.value);
    }

    private void MusicVolumeChanged()
    {
        SoundManager.SetMusicVolume(musicSlider.value);
    }
}
