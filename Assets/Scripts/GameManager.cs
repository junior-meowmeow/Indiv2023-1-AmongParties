using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum GameState {MENU, LOBBY, INGAME};
public enum GameMode { COOP, PVP };

public class GameManager : NetworkBehaviour
{
    [SerializeField] private GameState gameState;
    [SerializeField] private GameMode gameMode;
    [SerializeField] private Objective objective;

    [Header ("Find Object")]
    [SerializeField] private float timer;
    [SerializeField] private bool isRelax;

    [SerializeField] private List<PlayerData> playerList;
    public string localPlayerName;
    public bool isLocalPlayerEnableUI = true;
    public Transform lobbyLocation;
    public Transform coopGameplayLocation;
    public Transform pvpGameplayLocation;

    public static GameManager instance;

    void Awake()
    {
        instance = this;
    }

    public void JoinLobby(string username)
    {
        gameState = GameState.LOBBY;
        UpdateGameState(gameState);
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
        /*
        if(count <= 1)
        {
            return validUsername;
        }
        validUsername = username + " 1";
        count = 2;
        foreach (PlayerData ps in playerList)
        {
            if (ps.playerName == validUsername)
            {
                validUsername = username + " " + count;
                count++;
            }
        }
        return validUsername;
        */
    }

    public void StartGame()
    {
        if (!IsServer) return;

        gameMode = GameMode.COOP;
        StartGameServerRPC();
        SetTimer(10f, true);
    }

    [ServerRpc]
    void StartGameServerRPC()
    {
        StartGameClientRPC();
        ObjectPool.instance.PrewarmSpawn();
    }

    [ClientRpc]
    void StartGameClientRPC()
    {
        gameState = GameState.INGAME;
        foreach(PlayerData ps in playerList)
        {
            ps.player.rb.transform.position = GetGameplaySpawnPosition();
        }
        UpdateUI();
    }

    void Update()
    {
        TimerCounting();
    }

    void TimerCounting()
    {
        if (gameState != GameState.INGAME) return;
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            NetworkManagerUI.instance.UpdateTimer(timer, isRelax);
        }
        else
        {
            if (isRelax) NextObjective();
            else
            {
                EndObjectiveClientRPC();
                SetTimer(5f, true);
            }
        }
        
    }

    public Vector3 GetPlayerSpawnPosition()
    {
        if(gameState != GameState.INGAME)
        {
            return GetLobbySpawnPosition();
        }
        return GetGameplaySpawnPosition();
    }

    public Vector3 GetLobbySpawnPosition()
    {
        return lobbyLocation.position + new Vector3(Random.Range(-1.3f, 4f), 0, Random.Range(-2.5f, 3.5f));
    }

    public Vector3 GetGameplaySpawnPosition()
    {
        if(gameMode == GameMode.COOP)
        {
            return coopGameplayLocation.position + new Vector3(Random.Range(-8f, 8f), 0, Random.Range(-3.5f, 3.5f));
        }
        return lobbyLocation.position;
    }

    public bool GetObject(PickableObject obj, string location)
    {
        if (!IsServer) return false;
        if (isRelax) return false;
        
        if (objective.ScoreObject(obj, location))
        {
            UpdateObjectiveClientRPC(objective.score, objective.targetScore);
            return true;
        }
        return false;
    }

    public void SetTimer(float time, bool isRelax)
    {
        if (!IsServer) return;

        SetTimerClientRPC(time, isRelax);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateGameStateServerRPC()
    {
        Debug.Log("SendGameState");
        UpdateGameStateClientRPC(gameState);
        UpdateObjectiveClientRPC(objective.score, objective.targetScore);
        SetTimerClientRPC(timer, isRelax);
    }

    [ClientRpc]
    void UpdateGameStateClientRPC(GameState state)
    {
        if (IsServer) return;
        Debug.Log("yes");
        Debug.Log(state);
        UpdateGameState(state);
    }

    private void UpdateGameState(GameState state)
    {
        gameState = state;
        UpdateUI();
    }

    [ClientRpc]
    void SetTimerClientRPC(float time, bool isRelax)
    {
        timer = time;
        this.isRelax = isRelax;
        NetworkManagerUI.instance.UpdateTimer(time, isRelax);
    }

    [ClientRpc]
    void UpdateObjectiveClientRPC(ushort score, ushort targetScore)
    {
        objective.Update(score, targetScore);
        NetworkManagerUI.instance.UpdateObjective(objective);
    }

    void NextObjective()
    {
        if (!IsServer) return;

        objective = new Objective();
        objective.SetUp();

        NextObjectiveClientRPC(objective.GetID(), objective.score, objective.targetScore);
        SetTimerClientRPC(objective.duration, false);
        ObjectSpawner.instance.StartObjective(objective.GetID(), objective.targetScore);
    }

    [ClientRpc]
    void NextObjectiveClientRPC(int id, ushort score, ushort targetScore)
    {
        objective.Update(id, score, targetScore);

        NetworkManagerUI.instance.StartObjective(objective);
    }

    [ClientRpc]
    void EndObjectiveClientRPC()
    {
        NetworkManagerUI.instance.EndObjective(objective);
    }

    void UpdateUI()
    {
        NetworkManagerUI.instance.UpdateTimer(timer, isRelax);
        NetworkManagerUI.instance.UpdateObjective(objective);
        NetworkManagerUI.instance.UpdateCanvas(gameState);
    }

    public GameState GetGameState()
    {
        return gameState;
    }

    public List<PlayerData> GetPlayerList()
    {
        return playerList;
    }

    public void AddNewPlayer(PlayerData player)
    {
        playerList.Add(player);
        // AddNewPlayerServerRpc(player);
    }

    public void RemovePlayer(PlayerData player)
    {
        playerList.Remove(player);
        // AddNewPlayerServerRpc(player);
    }

    public bool IsPlayerHost()
    {
        return IsServer;
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
