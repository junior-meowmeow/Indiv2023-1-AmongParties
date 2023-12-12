using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using HSVPicker;

public class LobbyUIManager : NetworkBehaviour
{

    [SerializeField] private GameMode selectedGameMode = GameMode.COOP;

    [Header("Lobby")]
    [SerializeField] private GameObject lobbyCanvas;

    [SerializeField] private List<GameModeButtonUI> gameModeBtnList;

    [SerializeField] private GameObject lobbyWaitText;
    [SerializeField] private Button startGameBtn;
    [SerializeField] private Button changeColorBtn;
    [SerializeField] private Button confirmColorBtn;

    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private Color selectedColor = new(231, 70, 58);

    private void OnEnable()
    {
        MainUIManager.updateGameStateUI += GameStateChanged;
        MainUIManager.updateGameModeUI += ToggleGameModeUI;
        MainUIManager.syncUIState += RequestGameMode;
    }

    private void OnDisable()
    {
        MainUIManager.updateGameStateUI -= GameStateChanged;
        MainUIManager.updateGameModeUI -= ToggleGameModeUI;
        MainUIManager.syncUIState -= RequestGameMode;
    }

    private void Awake()
    {
        InitButton();
    }

    private void InitButton()
    {
        startGameBtn.onClick.AddListener(() => {
            GameManager.instance.StartGame(selectedGameMode);
            SoundManager.Play("select");
        });

        foreach (GameModeButtonUI btn in gameModeBtnList)
        {
            btn.Init();
        }

        changeColorBtn.onClick.AddListener(() => {
            SoundManager.Play("select");
            colorPicker.gameObject.SetActive(true);
            confirmColorBtn.gameObject.SetActive(true);
            changeColorBtn.gameObject.SetActive(false);
        });

        confirmColorBtn.onClick.AddListener(() =>
        {
            SoundManager.Play("select");
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

    private void GameStateChanged(GameState gameState)
    {
        startGameBtn.gameObject.SetActive(IsServer);
        lobbyWaitText.SetActive(!IsServer);
        lobbyCanvas.SetActive(gameState == GameState.LOBBY);
    }

    private void ToggleGameModeUI(GameMode gameMode)
    {
        if (!IsServer) return;
        selectedGameMode = gameMode;
        UpdateGameModeClientRPC(gameMode);
    }

    [ClientRpc]
    private void UpdateGameModeClientRPC(GameMode gameMode)
    {
        selectedGameMode = gameMode;
        UpdateGameModeButton(gameMode);
    }

    private void UpdateGameModeButton(GameMode gameMode)
    {
        foreach (GameModeButtonUI btn in gameModeBtnList)
        {
            btn.UpdateButton(gameMode, IsServer);
        }
    }

    private void RequestGameMode()
    {
        RequestGameModeServerRPC();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestGameModeServerRPC(ServerRpcParams serverRpcParams = default)
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
            SendGameModeClientRPC(selectedGameMode, clientRpcParams);
        }
    }

    [ClientRpc]
    void SendGameModeClientRPC(GameMode gameMode, ClientRpcParams clientRpcParams = default)
    {
        selectedGameMode = gameMode;
        UpdateGameModeButton(gameMode);
    }

}
