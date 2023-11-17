using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum GameState {MENU, LOBBY, INGAME};
public enum Objective {FIND_OBJECT}

public class GameManager : NetworkBehaviour
{
    [SerializeField] private GameState gameState;
    [SerializeField] private Objective objective;
    [SerializeField] private int score;
    [SerializeField] private int targetScore;
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
        
    }

    public void StartGame()
    {
        if (!IsServer) return;

        StartGameServerRPC();
        SetTimer(20f, true);
        NextObjective();
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
            if (isRelax) NextObjective();
            else SetTimer(5f, true);
        }
        
    }

    public void SetTimer(float time, bool isRelax)
    {
        if (!IsServer) return;

        SetTimerClientRPC(time, isRelax);
    }

    [ClientRpc]
    void SetTimerClientRPC(float time, bool isRelax)
    {
        timer = time;
        this.isRelax = isRelax;
        NetworkManagerUI.instance.UpdateTimer(time, isRelax);
    }

    public void GetScore(int a)
    {
        if (!IsServer) return;
        score += a;
        SetScoreClientRPC(score);
    }

    [ClientRpc]
    void SetScoreClientRPC(int score)
    {
        this.score = score;
        NetworkManagerUI.instance.UpdateObjective(score, targetScore);
    }

    void NextObjective()
    {
        if (!IsServer) return;
        NextObjectiveClientRPC(
            (Objective)Random.Range(0, 1),
            Random.Range(3, 5)
        );
    }

    [ClientRpc]
    void NextObjectiveClientRPC(Objective objective, int targetScore)
    {
        this.objective = objective;
        this.targetScore = targetScore;
        score = 0;
        isRelax = false;
        SetTimer(Random.Range(20f, 25f), isRelax);

        NetworkManagerUI.instance.UpdateObjective(score, targetScore);
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
