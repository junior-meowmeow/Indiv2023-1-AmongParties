using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using TMPro;

public class MainUIManager : NetworkBehaviour
{

    private static MainUIManager instance;
    public static MainUIManager Instance => instance;
    
    public delegate void GameStateDelegate(GameState gameState);
    public static GameStateDelegate updateGameStateUI;

    public delegate void GameModeDelegate(GameMode gameMode);
    public static GameModeDelegate updateGameModeUI;

    public static event Action syncUIState;

    [Header("Essential Canvases")]
    [SerializeField] private GameObject loadingCanvas;
    [SerializeField] private GameObject pauseCanvas;
    [SerializeField] private GameObject ingameCanvas;

    [Header("Texts")]
    [SerializeField] private TMP_Text playerListText;
    [SerializeField] private TMP_Text loadingText;

    private void OnEnable()
    {
        updateGameStateUI += GameStateChanged;
    }

    private void OnDisable()
    {
        updateGameStateUI -= GameStateChanged;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        updateGameStateUI(GameManager.instance.GetGameState());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (GameManager.instance.GetGameState() == GameState.INGAME)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                ToggleCursor();
            }
        }
        if (Input.GetMouseButtonDown(0))
        {
            if (GameManager.instance.GetGameState() == GameState.INGAME && !pauseCanvas.activeSelf)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    private void ToggleCursor()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public static void SyncUIState()
    {
        syncUIState();
    }

    public void UpdatePlayerList()
    {
        List<PlayerData> players = GameManager.instance.GetPlayerList();

        playerListText.text = "Player List:\n";

        int count = 1;
        foreach (PlayerData player in players)
        {
            playerListText.text += (count++) + ". " + player.playerName + "\n";
        }
    }

    public void ToggleLoading(bool isLoading)
    {
        loadingCanvas.SetActive(isLoading);
    }

    public void ToggleLoading(bool isLoading, string text)
    {
        loadingCanvas.SetActive(isLoading);
        loadingText.text = text;
    }

    private void GameStateChanged(GameState gameState)
    {
        ingameCanvas.SetActive(gameState == GameState.INGAME);
        if (gameState == GameState.MENU)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void OnApplicationQuit()
    {
        if (IsHost)
        {
            AfterHostEndedClientRPC();
        }
    }

    public void BackToMenu()
    {
        if (IsHost)
        {
            AfterHostEndedClientRPC();
        }
        else
        {
            DisconnectToMenu();
        }
    }

    [ClientRpc]
    private void AfterHostEndedClientRPC()
    {
        DisconnectToMenu();
    }

    private void DisconnectToMenu()
    {
        GameObject networkManager = NetworkManager.Singleton.gameObject;
        NetworkManager.Singleton.Shutdown();
        Destroy(networkManager);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    }

}
