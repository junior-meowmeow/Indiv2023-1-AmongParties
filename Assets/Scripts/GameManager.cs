using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public enum GameState {MENU, LOBBY, INGAME};
public enum GameMode { COOP, PVP };

public class GameManager : NetworkBehaviour
{
    [SerializeField] private GameState gameState;
    [SerializeField] private GameMode gameMode;
    [SerializeField] private Objective currentObjective;
    [SerializeField] private ushort doneScore = 0;
    [SerializeField] private ushort failScore = 0;
    [SerializeField] private ushort winTargetScore = 3;
    [SerializeField] private ushort loseTargetScore = 2;
    [SerializeField] private TMP_Text mainObjectiveText;
    [SerializeField] private GameObject winText;
    [SerializeField] private GameObject loseText;

    [Header ("Find Object")]
    [SerializeField] private float timer;
    [SerializeField] private bool isRelax;

    [SerializeField] private List<PlayerData> playerList;
    public PlayerController localPlayer;
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
        SoundManager.Instance.PlayTheme("menu");
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

    public void StartGame(GameMode gameMode)
    {
        if (!IsServer) return;
        this.gameMode = gameMode;
        if (gameMode == GameMode.COOP)
        {
            StartCOOPGameServerRPC();
            SetTimer(10f, true);
        }
        else if(gameMode == GameMode.PVP)
        {
            StartPVPGameServerRPC();
            SetTimer(10f, true);
        }

    }

    [ServerRpc]
    void StartCOOPGameServerRPC()
    {
        if (!ObjectPool.instance.isPoolInitialized)
        {
            ObjectPool.instance.PrewarmSpawn();
        }
        StartCOOPGameClientRPC(winTargetScore, loseTargetScore);
    }

    [ClientRpc]
    void StartCOOPGameClientRPC(ushort winScore, ushort loseScore)
    {
        winText.SetActive(false);
        loseText.SetActive(false);
        gameState = GameState.INGAME;
        doneScore = 0;
        failScore = 0;
        winTargetScore = winScore;
        loseTargetScore = loseScore;
        SoundManager.Instance.PlayTheme("coop");
        mainObjectiveText.text = "Complete " + winScore + " Objective to Win(You have " + loseScore + " Chances to Fail)";
        NetworkManagerUI.instance.ResetObjectives();
        ObjectPool.instance.RecallAllObjects();
        UpdateUI();
        if (IsServer)
        {
            foreach (PlayerData ps in playerList)
            {
                ps.player.WarpServerRPC(GetGameplaySpawnPosition());
            }
        }
    }

    [ServerRpc]
    void StartPVPGameServerRPC()
    {
        StartPVPGameClientRPC();
        ObjectPool.instance.PrewarmSpawn();
    }

    [ClientRpc]
    void StartPVPGameClientRPC()
    {
        gameState = GameState.INGAME;
        foreach (PlayerData ps in playerList)
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
        return lobbyLocation.position + new Vector3(Random.Range(-3f, 1.25f), 0, Random.Range(-2.5f, 1.25f));
    }

    public Vector3 GetGameplaySpawnPosition()
    {
        if(gameMode == GameMode.COOP)
        {
            return coopGameplayLocation.position + new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-8f, 8f));
        }
        return lobbyLocation.position;
    }

    public void UpdateCOOPGameScore(bool isDone)
    {
        if(isDone)
        {
            ++doneScore;
            SoundManager.Instance.Play("get_point");
            if (doneScore >= winTargetScore)
            {
                EndCOOPGame(true);
            }
        }
        else
        {
            ++failScore;
            SoundManager.Instance.Play("failed");
            if (failScore >= loseTargetScore)
            {
                EndCOOPGame(false);
            }
        }
    }

    public void EndCOOPGame(bool isWin)
    {
        gameState = GameState.LOBBY;
        SoundManager.Instance.PlayTheme("lobby");
        NetworkManagerUI.instance.UpdateCanvas(gameState);
        if (isWin)
        {
            Debug.Log("YOU WIN");
            winText.SetActive(true);
            SoundManager.Instance.Play("win");
        }
        else
        {
            Debug.Log("YOU LOSE");
            loseText.SetActive(true);
            SoundManager.Instance.Play("lose");
        }
        if (IsServer)
        {
            foreach (PlayerData ps in playerList)
            {
                ps.player.WarpClientRPC(GetLobbySpawnPosition());
            }
        }
    }

    [ClientRpc]
    void EndCOOPGameClientRPC(bool isWin)
    {
        EndCOOPGame(isWin);
    }

    public bool GetObject(PickableObject obj, string location)
    {
        if (!IsServer) return false;
        if (isRelax) return false;
        
        if (currentObjective.ScoreObject(obj, location))
        {
            UpdateObjectiveClientRPC(currentObjective.score, currentObjective.targetScore);
            if(currentObjective.isComplete)
            {
                if (doneScore+1 >= winTargetScore)
                {
                    EndCOOPGameClientRPC(true);
                }
            }
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
        UpdateGameStateClientRPC(gameState);
        UpdateObjectiveClientRPC(currentObjective.score, currentObjective.targetScore);
        SetTimerClientRPC(timer, isRelax);
    }

    [ClientRpc]
    void UpdateGameStateClientRPC(GameState state)
    {
        if (IsServer) return;
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
        currentObjective.Update(score, targetScore);
        NetworkManagerUI.instance.UpdateObjective(currentObjective);
    }

    void NextObjective()
    {
        if (!IsServer) return;

        currentObjective = new Objective();
        currentObjective.SetUp();

        NextObjectiveClientRPC(currentObjective.GetID(), currentObjective.score, currentObjective.targetScore);
        SetTimerClientRPC(currentObjective.duration, false);
        ObjectSpawner.instance.StartObjective(currentObjective.GetID(), currentObjective.targetScore);
    }

    [ClientRpc]
    void NextObjectiveClientRPC(int id, ushort score, ushort targetScore)
    {
        currentObjective.Update(id, score, targetScore);

        NetworkManagerUI.instance.StartObjective(currentObjective);
    }

    [ClientRpc]
    void EndObjectiveClientRPC()
    {
        NetworkManagerUI.instance.EndObjective(currentObjective);
        UpdateCOOPGameScore(currentObjective.isComplete);
    }

    void UpdateUI()
    {
        NetworkManagerUI.instance.UpdateTimer(timer, isRelax);
        NetworkManagerUI.instance.UpdateObjective(currentObjective);
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

    public Objective GetCurrentObjective()
    {
        return currentObjective;
    }

    public void SetCurrentObjective(Objective objective)
    {
        currentObjective = objective;
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
