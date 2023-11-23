using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [Header ("Menu")]
    [SerializeField] private Button serverBtn;
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private Camera menuCam;
    [SerializeField] private TMP_InputField usernameInput;

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
        serverBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartServer();
            GameManager.instance.JoinLobby();
            UpdateCanvas(GameManager.instance.GetGameState());
        });
        hostBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            GameManager.instance.JoinLobby();
            UpdateCanvas(GameManager.instance.GetGameState());
        });
        clientBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            GameManager.instance.JoinLobby();
            UpdateCanvas(GameManager.instance.GetGameState());
        });
        startGameBtn.onClick.AddListener(() => {
            GameManager.instance.StartGame();
            UpdateCanvas(GameManager.instance.GetGameState());
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
        string objectName = "";
        if (objective.id == 0)
        {
            if (objective.type == ObjectType.Box) objectName = "Box";
            if (objective.type == ObjectType.Sphere) objectName = "Sphere";
        }
        else
        {
            if (objective.color == ObjectColor.RED) objectName = "Red object";
            if (objective.color == ObjectColor.BLUE) objectName = "Blue object";
        }
        objectiveText.text = "Objective : Deliver " + objectName + " " + objective.score.ToString() + "/" + objective.targetScore.ToString();

        objectiveDoneText.SetActive(objective.isComplete);
        objectiveFailText.SetActive(false);
        // objectiveFailText.SetActive(!objective.isComplete);
    }

    public void UpdatePlayerList()
    {
        List<PlayerSetting> players = GameManager.instance.GetPlayerList();

        playerListText.text = "";

        foreach(PlayerSetting player in players)
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
}
