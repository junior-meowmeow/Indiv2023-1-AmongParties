using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum GameState {MENU, LOBBY, INGAME};

public class GameManager : NetworkBehaviour
{
    [SerializeField] private GameState gameState;
    [SerializeField] private Objective objective;

    [Header ("Find Object")]
    [SerializeField] private float timer;
    [SerializeField] private bool isRelax;

    [SerializeField] private List<PlayerData> playerList;
    public Transform lobbyLocation;
    public Transform gameplayLocation;

    public static GameManager instance;

    void Awake()
    {
        instance = this;
    }

    public void JoinLobby()
    {
        gameState += 1;

        UpdateGameStateServerRPC();
        UpdateUI();
    }

    public void StartGame()
    {
        if (!IsServer) return;

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
            ps.player.rb.transform.position = gameplayLocation.position;
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
            NetworkManagerUI.instance.UpdateObjective(objective);
            if (isRelax) NextObjective();
            else SetTimer(5f, true);
        }
        
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

    [ServerRpc]
    void UpdateGameStateServerRPC()
    {
        Debug.Log("Server");
        UpdateGameStateClientRPC((int)gameState);
    }

    [ClientRpc]
    void UpdateGameStateClientRPC(int state)
    {
        Debug.Log("Client");
        gameState = (GameState)state;
        UpdateObjectiveClientRPC(objective.score, objective.targetScore);
        SetTimerClientRPC(timer, isRelax);
    }

    [ClientRpc]
    void SetTimerClientRPC(float time, bool isRelax)
    {
        timer = time;
        this.isRelax = isRelax;
        NetworkManagerUI.instance.UpdateTimer(time, isRelax);
    }

    [ClientRpc]
    void UpdateObjectiveClientRPC(int score, int targetScore)
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
        ObjectSpawner.instance.StartObjectiveServerRPC(objective);
        SetTimerClientRPC(objective.duration, false);
    }

    [ClientRpc]
    void NextObjectiveClientRPC(int id, int score, int targetScore)
    {
        objective.Update(id, score, targetScore);

        NetworkManagerUI.instance.UpdateObjective(objective);
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
