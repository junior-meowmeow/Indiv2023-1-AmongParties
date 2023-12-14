using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class GameplayManager : NetworkBehaviour
{

    private static GameplayManager instance;
    public static GameplayManager Instance => instance;

    [SerializeField] private List<GameModeManager> gameModeManagerList;
    [SerializeField] private GameModeManager currentGameModeManager;

    [SerializeField] private TMP_Text mainObjectiveText;
    [SerializeField] private TMP_Text winText;
    [SerializeField] private TMP_Text loseText;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public void StartGame(GameMode gameMode)
    {
        if (!IsServer) return;
        if (GameDataManager.Instance.GetGameState() == GameState.INGAME) return;
        foreach (GameModeManager gm in gameModeManagerList)
        {
            if(gm.GetGameMode() == gameMode)
            {
                currentGameModeManager = gm;
                GameDataManager.Instance.SetGameMode(gameMode);
                GameDataManager.Instance.SetGameState(GameState.INGAME);

                SetUpGameModeClientRPC(gameMode);
                gm.StartGameServer();
                return;
            }
        }
    }

    [ClientRpc]
    private void SetUpGameModeClientRPC(GameMode gameMode)
    {
        if (IsServer) return;

        foreach (GameModeManager gm in gameModeManagerList)
        {
            if (gm.GetGameMode() == gameMode)
            {
                currentGameModeManager = gm;
                GameDataManager.Instance.SetGameMode(gameMode);
                GameDataManager.Instance.SetGameState(GameState.INGAME);
                return;
            }
        }
    }

    private void Update()
    {
        if(GameDataManager.Instance.GetGameState() == GameState.INGAME)
        {
            currentGameModeManager.UpdateGameMode();
        }
    }

    public void GameModeEnd()
    {
        GameDataManager.Instance.SetGameState(GameState.LOBBY);
        SoundManager.PlayMusic("lobby");
    }

    public void SetWinText(string text, bool isActive)
    {
        winText.gameObject.SetActive(isActive);
        winText.text = text;
    }

    public void SetLoseText(string text, bool isActive)
    {
        loseText.gameObject.SetActive(isActive);
        loseText.text = text;
    }

    public void SetMainObjectiveText(string text)
    {
        mainObjectiveText.text = text;
    }

    public bool GetObject(PickableObject obj, string locationName)
    {
        return currentGameModeManager.GetObject(obj, locationName);
    }

    public GameModeManager GetCurrentGameModeManager()
    {
        return currentGameModeManager;
    }

    public void RequestGameModeManagerUpdate(ClientRpcParams clientRpcParams = default)
    {
        if (currentGameModeManager != null)
        {
            currentGameModeManager.RequestGameModeUpdate(clientRpcParams);
        }
    }

}
