using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [Header ("Menu")]
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private Camera menuCam;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField inputField;
    private string address = "127.0.0.1";

    [Header ("Lobby")]
    [SerializeField] private Canvas lobbyCanvas;
    [SerializeField] private Button startGameBtn;
    [SerializeField] private GameObject lobbyWaitText;
    [SerializeField] private GameObject colorButtonList;
    [SerializeField] private Color[] colorList;
    [SerializeField] private TMP_Text playerListText;

    [Header ("Ingame")]
    [SerializeField] private Canvas ingameCanvas;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private GameObject objectiveDoneText;
    [SerializeField] private GameObject objectiveFailText;

    public static NetworkManagerUI instance;

    void Awake()
    {
        InitButton();
    }

    void Start()
    {
        instance = this;
        UpdateCanvas(GameManager.instance.GetGameState());
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(delegate { AddressChanged(inputField); });
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (Cursor.lockState == CursorLockMode.Locked) Cursor.lockState = CursorLockMode.None;
            else Cursor.lockState = CursorLockMode.Locked;
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    void InitButton()
    {
        hostBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            GameManager.instance.JoinLobby();
        });
        clientBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            GameManager.instance.JoinLobby();
        });
        startGameBtn.onClick.AddListener(() => {
            GameManager.instance.StartGame();
        });

        Button[] btnList = colorButtonList.GetComponentsInChildren<Button>();
        for(int idx = 0; idx < btnList.Length; idx++)
        {
            btnList[idx].GetComponent<PlayerColorButtonUI>().Init(colorList[idx]);
        }
    }

    public void UpdateTimer(float time, bool isRelax)
    {
        if (isRelax) timerText.color = new Color(1, 0, 0);
        else timerText.color = new Color(1, 1, 1);
        
        timerText.text = ((int)time/60).ToString() + ":" + ((int)time%60).ToString();
    }

    public void UpdateObjective(Objective objective)
    {
        objectiveText.text = "Objective : \n" + objective.GetObjectiveDescription()
            + "\n" + objective.score.ToString() + "/" + objective.targetScore.ToString();

        objectiveDoneText.SetActive(objective.isComplete);
        objectiveFailText.SetActive(false);
        // objectiveFailText.SetActive(!objective.isComplete);
    }

    public void UpdatePlayerList()
    {
        List<PlayerData> players = GameManager.instance.GetPlayerList();

        playerListText.text = "";

        foreach(PlayerData player in players)
        {
            playerListText.text += player.name + "\n";
        }
    }

    public void UpdateCanvas(GameState gameState)
    {
        menuCanvas.gameObject.SetActive(gameState == GameState.MENU);
        menuCam.gameObject.SetActive(gameState == GameState.MENU);
        startGameBtn.gameObject.SetActive(GameManager.instance.IsPlayerHost());
        lobbyWaitText.gameObject.SetActive(!GameManager.instance.IsPlayerHost());
        
        lobbyCanvas.gameObject.SetActive(gameState == GameState.LOBBY);

        ingameCanvas.gameObject.SetActive(gameState == GameState.INGAME);
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
}
