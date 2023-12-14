using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

public enum GameState { MENU, LOBBY, INGAME };
public enum GameMode { COOP, PVP };

public class GameDataManager : NetworkBehaviour
{

    private static GameDataManager instance;
    public static GameDataManager Instance => instance;

    [SerializeField] private GameState gameState;
    [SerializeField] private GameMode gameMode;

    [SerializeField] private Transform lobbyLocation;

    [SerializeField] private List<PlayerData> playerList;
    public PlayerController localPlayer;
    public string localPlayerName;
    public bool isLocalPlayerEnableUI = true;


    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public void OnJoinLobby(string username)
    {
        SetGameState(GameState.LOBBY);
        SoundManager.PlayMusic("lobby");
        localPlayerName = username;
    }

    public string GetValidUsername(string username)
    {
        string validUsername = username;
        byte count = 1;
        foreach (PlayerData ps in playerList)
        {
            if (ps.playerName == validUsername)
            {
                validUsername = username + " " + count;
                count++;
            }
        }
        return validUsername;
    }

    public Vector3 GetPlayerSpawnPosition()
    {
        if (gameState != GameState.INGAME)
        {
            return GetLobbySpawnPosition();
        }
        return GameplayManager.Instance.GetCurrentGameModeManager().GetSpawnPosition();
    }

    public Vector3 GetLobbySpawnPosition()
    {
        return lobbyLocation.position + new Vector3(Random.Range(-3f, 1.25f), 0, Random.Range(-2.5f, 1.25f));
    }

    public GameState GetGameState()
    {
        return gameState;
    }

    public void SetGameState(GameState gameState)
    {
        this.gameState = gameState;
        MainUIManager.updateGameStateUI(gameState);
    }

    public GameMode GetGameMode()
    {
        return gameMode;
    }

    public void SetGameMode(GameMode gameMode)
    {
        this.gameMode = gameMode;
    }

    public List<PlayerData> GetPlayerList()
    {
        return playerList;
    }

    public void AddNewPlayer(PlayerData player)
    {
        player.indexInPlayerList = playerList.Count;
        playerList.Add(player);
        // AddNewPlayerServerRpc(player);
    }

    public void RemovePlayer(PlayerData player)
    {
        playerList.Remove(player);
        for (int i = 0; i < playerList.Count; i++)
        {
            playerList[i].indexInPlayerList = i;
        }
        // AddNewPlayerServerRpc(player);
    }

    public void ReorderPlayerList()
    {
        playerList = playerList.OrderBy(player => player.indexInPlayerList).ToList();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestGameDataServerRPC(ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        if (!NetworkManager.ConnectedClients.ContainsKey(clientId)) return;
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
        SendGameDataClientRPC(gameState, gameMode,clientRpcParams);
        UpdateMusicClientRPC(SoundManager.GetCurrentMusicName(),clientRpcParams);
        GameplayManager.Instance.RequestGameModeManagerUpdate(clientRpcParams);
        for (byte i = 0;i < playerList.Count;i++)
        {
            playerList[i].SetIndexClientRPC(i);
        }
    }

    [ClientRpc]
    private void SendGameDataClientRPC(GameState gameState, GameMode gameMode, ClientRpcParams clientRpcParams = default)
    {
        if (IsServer) return;
        this.gameMode = gameMode;
        this.gameState = gameState;
        MainUIManager.updateGameStateUI(this.gameState);
    }

    [ClientRpc]
    private void UpdateMusicClientRPC(string name, ClientRpcParams clientRpcParams = default)
    {
        if (IsServer) return;
        SoundManager.PlayMusic(name);
    }

    // [ServerRpc]
    // void AddNewPlayerServerRpc(PlayerSetting player)
    // {
    //     AddNewPlayerClientRpc(player);
    // }

    // [ClientRpc]
    // void AddNewPlayerClientRpc(PlayerSetting player)
    // {
    //     playerList.Add(player);
    // }

}
