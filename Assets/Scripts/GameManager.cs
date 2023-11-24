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

    [SerializeField] private List<PlayerSetting> playerList;
    
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
    }

    [ClientRpc]
    void StartGameClientRPC()
    {
        gameState = GameState.INGAME;
        UpdateUI();
    }

    void Update()
    {
        TimerCounting();
    }

    void TimerCounting()
    {
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

    public void GetObject(PickableObject obj)
    {
        if (!IsServer) return;
        if (isRelax) return;
        
        if (objective.ScoreObject(obj))
        {
            UpdateObjectiveClientRPC(objective.GetID(), objective.score, objective.targetScore);
        }
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
        UpdateObjectiveClientRPC(objective.GetID(), objective.score, objective.targetScore);
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
    void UpdateObjectiveClientRPC(int id, int score, int targetScore)
    {
        objective.Update(id, score, targetScore);
        NetworkManagerUI.instance.UpdateObjective(objective);
    }

    void NextObjective()
    {
        if (!IsServer) return;

        objective = new Objective();
        objective.SetUp();

        NextObjectiveClientRPC(objective.GetID(), objective.score, objective.targetScore);
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

    public List<PlayerSetting> GetPlayerList()
    {
        return playerList;
    }

    public void AddNewPlayer(PlayerSetting player)
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
