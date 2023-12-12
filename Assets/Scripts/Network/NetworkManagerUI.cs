using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using HSVPicker;
using UnityEngine.SceneManagement;

public class NetworkManagerUI : NetworkBehaviour
{
    [Header("Menu")]
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private Camera menuCam;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField addressInput;
    private string address = "127.0.0.1";
    private string username = "Player";

    [Header("Lobby")]
    [SerializeField] private GameMode selectedGameMode = GameMode.COOP;
    [SerializeField] private Canvas lobbyCanvas;
    [SerializeField] private Button startGameBtn;
    [SerializeField] private GameObject lobbyWaitText;
    //[SerializeField] private GameObject colorButtonList;
    //[SerializeField] private Color[] colorList;
    [SerializeField] private Button changeColorBtn;
    [SerializeField] private Button confirmColorBtn;
    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private Color selectedColor;
    [SerializeField] private TMP_Text playerListText;
    [SerializeField] private Toggle coopToggle;
    [SerializeField] private Toggle pvpToggle;
    [SerializeField] private List<GameModeButtonUI> gameModeBtnList;

    [Header("Ingame")]
    [SerializeField] private Canvas ingameCanvas;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject ObjectiveUIPrefab;
    [SerializeField] private List<ObjectiveUI> ObjectiveList;
    [SerializeField] private Transform ObjectiveParent;

    [Header("Pause")]
    [SerializeField] private Canvas pauseCanvas;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Button backToMenuBtn;
    [SerializeField] private Button quitGameBtn;

    [Header("Loading")]
    [SerializeField] private Canvas loadingCanvas;
    [SerializeField] private TMP_Text loadingText;

    public static NetworkManagerUI instance;

    void Awake()
    {
        InitButton();
    }

    void Start()
    {
        instance = this;
        UpdateCanvas(GameManager.instance.GetGameState());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (GameManager.instance.GetGameState() == GameState.INGAME)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            else if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            if (GameManager.instance.GetGameState() != GameState.MENU)
            {
                TogglePause();
            }
        }
        if (Input.GetMouseButtonDown(0))
        {
            if (GameManager.instance.GetGameState() == GameState.INGAME && !pauseCanvas.gameObject.activeSelf)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        ToggleGameMode(selectedGameMode);
    }

    void InitButton()
    {
        if (addressInput != null)
        {
            addressInput.onEndEdit.AddListener(delegate { AddressChanged(addressInput); });
        }
        if (usernameInput != null)
        {
            usernameInput.onEndEdit.AddListener(delegate { UsernameChanged(usernameInput); });
        }
        hostBtn.onClick.AddListener(() => {
            ToggleLoading(true, "Loading...");
            NetworkManager.Singleton.StartHost();
            GameManager.instance.JoinLobby(username);
            SoundManager.Instance.Play("select");
        });
        clientBtn.onClick.AddListener(() => {
            ToggleLoading(true, "Findind Server...");
            NetworkManager.Singleton.StartClient();
            GameManager.instance.JoinLobby(username);
            SoundManager.Instance.Play("select");
        });
        startGameBtn.onClick.AddListener(() => {
            GameManager.instance.StartGame(selectedGameMode);
            SoundManager.Instance.Play("select");
        });
        backToMenuBtn.onClick.AddListener(() => {
            SoundManager.Instance.Play("select");
            BackToMenu();
        });
        quitGameBtn.onClick.AddListener(() => {
            Application.Quit();
            //Subscribed OnApplicationQuit
        });

        foreach (GameModeButtonUI btn in gameModeBtnList)
        {
            btn.Init();
        }

        InitSlider();

        /*
        Button[] btnList = colorButtonList.GetComponentsInChildren<Button>();
        for(int idx = 0; idx < btnList.Length; idx++)
        {
            //btnList[idx].GetComponent<PlayerColorButtonUI>().Init(colorList[idx]);
        }
        */

        changeColorBtn.onClick.AddListener(() => {
            SoundManager.Instance.Play("select");
            colorPicker.gameObject.SetActive(true);
            confirmColorBtn.gameObject.SetActive(true);
            changeColorBtn.gameObject.SetActive(false);
        });

        confirmColorBtn.onClick.AddListener(() =>
        {
            SoundManager.Instance.Play("select");
            NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerData>().SetPlayerColorServerRPC(selectedColor);
            changeColorBtn.gameObject.SetActive(true);
            colorPicker.gameObject.SetActive(false);
            confirmColorBtn.gameObject.SetActive(false);
        });

        colorPicker.onValueChanged.AddListener(color =>
        {
            selectedColor = color;
            NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerData>().SetPlayerColor(color);
        });
    }

    private void OnApplicationQuit()
    {
        if (IsHost)
        {
            AfterHostEndedClientRPC();
        }
    }

    private void BackToMenu()
    {
        if(IsHost)
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

    public void UpdateTimer(float time, bool isRelax)
    {
        if (isRelax) timerText.color = new Color(1, 0.185f, 0.185f);
        else timerText.color = new Color(1, 1, 1);
        string secondText = ((int)time % 60).ToString("00");
        timerText.text = ((int)time / 60).ToString() + ":" + secondText;
    }

    public void StartObjective(Objective objective)
    {
        ObjectiveUI obj = Instantiate(ObjectiveUIPrefab, ObjectiveParent).GetComponent<ObjectiveUI>();
        obj.UpdateObjective(objective);
        ObjectiveList.Add(obj);
        obj.objectiveTitle.text = "Objective  " + ObjectiveList.Count.ToString();
    }

    public void EndObjective(Objective objective)
    {
        ObjectiveList[ObjectiveList.Count - 1].EndObjective(objective);
    }

    public void UpdateObjective(Objective objective)
    {
        if (ObjectiveList.Count == 0) return;
        ObjectiveList[ObjectiveList.Count - 1].UpdateObjective(objective);
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

    public void UpdateCanvas(GameState gameState)
    {
        if (!pauseCanvas.gameObject.activeSelf || gameState != GameState.INGAME)
        {
            pauseCanvas.gameObject.SetActive(false);
        }
        menuCanvas.gameObject.SetActive(gameState == GameState.MENU);
        menuCam.gameObject.SetActive(gameState == GameState.MENU);
        startGameBtn.gameObject.SetActive(GameManager.instance.IsPlayerHost());
        lobbyWaitText.SetActive(!GameManager.instance.IsPlayerHost());
        
        lobbyCanvas.gameObject.SetActive(gameState == GameState.LOBBY);

        ingameCanvas.gameObject.SetActive(gameState == GameState.INGAME);
        if(gameState == GameState.MENU)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void ToggleLoading(bool isLoading)
    {
        loadingCanvas.gameObject.SetActive(isLoading);
    }
    public void ToggleLoading(bool isLoading, string text)
    {
        loadingCanvas.gameObject.SetActive(isLoading);
        loadingText.text = text;
    }

    public void ResetObjectives()
    {
        foreach(ObjectiveUI obj in ObjectiveList)
        {
            Destroy(obj.gameObject);
        }
        ObjectiveList.Clear();
    }

    void AddressChanged(TMP_InputField input)
    {
        address = input.text;
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
            address,  // The IP address is a string
            (ushort)12345 // The port number is an unsigned short
            );
        Debug.Log("New Address : " + address);
    }

    public void ToggleGameMode(GameMode gameMode)
    {
        if (!IsServer) return;
        selectedGameMode = gameMode;
        UpdateGameModeClientRPC(gameMode);
    }

    [ClientRpc]
    void UpdateGameModeClientRPC(GameMode gameMode)
    {
        selectedGameMode = gameMode;
        UpdateGameModeButton(gameMode);
    }

    void UpdateGameModeButton(GameMode gameMode)
    {
        foreach (GameModeButtonUI btn in gameModeBtnList)
        {
            btn.UpdateButton(gameMode, IsServer);
        }
    }

    void SfxVolumeChanged()
    {
        SoundManager.Instance.sfxVolume = sfxSlider.value;
    }

    void MusicVolumeChanged()
    {
        SoundManager.Instance.SetMusicVolume(musicSlider.value);
    }

    void UsernameChanged(TMP_InputField input)
    {
        username = input.text;
        if(username == string.Empty)
        {
            username = "Player";
        }
        Debug.Log("New Username : " + username);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestUIStateServerRPC(ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientId))
        {
            //var client = NetworkManager.ConnectedClients[clientId];
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };
            SendUIStateClientRPC(selectedGameMode, clientRpcParams);
        }
    }

    [ClientRpc]
    void SendUIStateClientRPC(GameMode gameMode, ClientRpcParams clientRpcParams = default)
    {
        selectedGameMode = gameMode;
        UpdateGameModeButton(gameMode);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestObjectiveServerRPC(ServerRpcParams serverRpcParams = default)
    {
        if (GameManager.instance.GetGameState() != GameState.INGAME) return;
        var clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientId))
        {
            //var client = NetworkManager.ConnectedClients[clientId];
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };

            for (int i = 0; i < ObjectiveList.Count; i++)
            {
                if(ObjectiveList[i].isEnded)
                {
                    SetEndedObjectiveUIClientRPC(ObjectiveList[i].isDone, clientRpcParams);
                }
                else
                {
                    Objective objective = GameManager.instance.GetCurrentObjective();
                    int id = objective.GetID();
                    ushort score = objective.score;
                    ushort targetScore = objective.targetScore;
                    SetOngoingObjectiveUIClientRPC(id, score, targetScore, clientRpcParams);
                }
            }
        }
    }

    [ClientRpc]
    void SetEndedObjectiveUIClientRPC(bool isDone,ClientRpcParams clientRpcParams = default)
    {
        ObjectiveUI obj = Instantiate(ObjectiveUIPrefab, ObjectiveParent).GetComponent<ObjectiveUI>();
        obj.SetEndedUI(isDone);
        ObjectiveList.Add(obj);
        obj.objectiveTitle.text = "Objective  " + ObjectiveList.Count.ToString();
    }

    [ClientRpc]
    void SetOngoingObjectiveUIClientRPC(int id, ushort score, ushort targetScore,ClientRpcParams clientRpcParams = default)
    {
        Objective objective = new();
        objective.Update(id, score, targetScore);
        GameManager.instance.SetCurrentObjective(objective);
        ObjectiveUI obj = Instantiate(ObjectiveUIPrefab, ObjectiveParent).GetComponent<ObjectiveUI>();
        obj.UpdateObjective(objective);
        ObjectiveList.Add(obj);
        obj.objectiveTitle.text = "Objective  " + ObjectiveList.Count.ToString();
    }
}
