using UnityEngine;
using UnityEngine.UI;

public class PauseUIManager : MonoBehaviour
{

    [SerializeField] private GameObject pauseCanvas;

    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Button backToMenuBtn;
    [SerializeField] private Button quitGameBtn;

    private void OnEnable()
    {
        MainUIManager.updateGameStateUI += GameStateChanged;
    }

    private void OnDisable()
    {
        MainUIManager.updateGameStateUI -= GameStateChanged;
    }

    private void Awake()
    {
        InitButton();
        InitSlider();
    }

    private void InitButton()
    {
        backToMenuBtn.onClick.AddListener(() => {
            SoundManager.Play("select");
            MainUIManager.Instance.BackToMenu();
        });
        quitGameBtn.onClick.AddListener(() => {
            Application.Quit();
        });
    }

    private void InitSlider()
    {
        sfxSlider.value = SoundManager.GetSfxVolume();
        musicSlider.value = SoundManager.GetMusicVolume();
        sfxSlider.onValueChanged.AddListener(delegate { SfxVolumeChanged(); });
        musicSlider.onValueChanged.AddListener(delegate { MusicVolumeChanged(); });
    }

    private void GameStateChanged(GameState gameState)
    {
        if (!pauseCanvas.activeSelf || gameState != GameState.INGAME)
        {
            pauseCanvas.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            if (GameDataManager.Instance.GetGameState() != GameState.MENU)
            {
                TogglePause();
            }
        }
    }

    private void TogglePause()
    {
        if (pauseCanvas.activeSelf)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
        pauseCanvas.SetActive(!pauseCanvas.gameObject.activeSelf);
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
