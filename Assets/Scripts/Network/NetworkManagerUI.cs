using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [Header ("Lobby")]
    [SerializeField] private Button serverBtn;
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;
    [SerializeField] private Canvas lobbyCanvas;
    [SerializeField] private Camera lobbyCam;

    [Header ("Ingame")]
    [SerializeField] private Canvas ingameCanvas;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text objectiveText;

    public static NetworkManagerUI instance;

    void Awake()
    {
        serverBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartServer();
            ToggleLobbyCanvas(false);
            GameManager.instance.GameStart();
        });
        hostBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            ToggleLobbyCanvas(false);
            GameManager.instance.GameStart();
        });
        clientBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            ToggleLobbyCanvas(false);
            GameManager.instance.GameStart();
        });
    }

    void Start()
    {
        instance = this;
        ToggleLobbyCanvas(true);
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

    public void UpdateTimer(float time, bool isRelax)
    {
        if (isRelax) timerText.color = new Color(1, 0, 0);
        else timerText.color = new Color(1, 1, 1);
        
        timerText.text = ((int)time/60).ToString() + ":" + ((int)time%60).ToString();
    }

    public void UpdateObjective(int score, int targetScore)
    {
        objectiveText.text = "Objective : " + score.ToString() + "/" + targetScore.ToString();
    }

    void ToggleLobbyCanvas(bool isLobby)
    {
        lobbyCanvas.gameObject.SetActive(isLobby);
        lobbyCam.gameObject.SetActive(isLobby);
        ingameCanvas.gameObject.SetActive(!isLobby);
    }
}
