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
    [SerializeField] private TMP_InputField addressInput;
    private string address = "127.0.0.1";
    private string username = "Player";

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
    [SerializeField] private GameObject ObjectiveUIPrefab;
    [SerializeField] private List<ObjectiveUI> ObjectiveList;
    [SerializeField] private Transform ObjectiveParent;

    public static NetworkManagerUI instance;

    void Awake()
    {
        InitButton();
    }

    void Start()
    {
        instance = this;
        UpdateCanvas(GameManager.instance.GetGameState());
        if (addressInput != null)
        {
            addressInput.onEndEdit.AddListener(delegate { AddressChanged(addressInput); });
        }
        if (usernameInput != null)
        {
            usernameInput.onEndEdit.AddListener(delegate {UsernameChanged(usernameInput); });
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if(GameManager.instance.GetGameState() == GameState.INGAME)
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
        }
        if(Input.GetMouseButtonDown(0))
        {
            if (GameManager.instance.GetGameState() == GameState.INGAME)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    void InitButton()
    {
        hostBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            GameManager.instance.JoinLobby(username);
        });
        clientBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            GameManager.instance.JoinLobby(username);
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
        if (isRelax) timerText.color = new Color(1, 0.185f, 0.185f);
        else timerText.color = new Color(1, 1, 1);
        string secondText = ((int)time % 60).ToString("00");
        timerText.text = ((int)time/60).ToString() + ":" + secondText;
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
        foreach(PlayerData player in players)
        {
            playerListText.text += (count++) + ". " + player.playerName + "\n";
        }
    }

    public void UpdateCanvas(GameState gameState)
    {
        menuCanvas.gameObject.SetActive(gameState == GameState.MENU);
        menuCam.gameObject.SetActive(gameState == GameState.MENU);
        startGameBtn.gameObject.SetActive(GameManager.instance.IsPlayerHost());
        lobbyWaitText.SetActive(!GameManager.instance.IsPlayerHost());
        
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

    void UsernameChanged(TMP_InputField input)
    {
        username = input.text;
        if(username == string.Empty)
        {
            username = "Player";
        }
        Debug.Log("New Username : " + username);
    }
}
