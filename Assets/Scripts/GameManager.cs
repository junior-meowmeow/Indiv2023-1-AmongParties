using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

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
    public Transform[] pvpGameplayLocations;
    private int pvpLocationCount = 0;

    public static GameManager instance;

    void Awake()
    {
        instance = this;
    }

    public void JoinLobby(string username)
    {
        gameState = GameState.LOBBY;
        SoundManager.PlayMusic("lobby");
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
        if (gameState == GameState.INGAME) return;
        this.gameMode = gameMode;
        if (gameMode == GameMode.COOP)
        {
            StartCOOPGameServerRPC();
            SetTimer(10f, true);
        }
        else if(gameMode == GameMode.PVP)
        {
            StartPVPGameServerRPC();
            //SetTimer(10f, true);
        }

    }

    [ServerRpc]
    void StartCOOPGameServerRPC()
    {
        InitPool();
        StartCOOPGameClientRPC(winTargetScore, loseTargetScore);
    }

    [ClientRpc]
    void StartCOOPGameClientRPC(ushort winScore, ushort loseScore)
    {
        ResetValueBeforeGame();
        winTargetScore = winScore;
        loseTargetScore = loseScore;
        SoundManager.PlayMusic("coop");
        mainObjectiveText.text = "Complete " + winScore + " Objective to Win(You have " + loseScore + " Chances to Fail)";
        UpdateUI();
        if (IsServer)
        {
            foreach (PlayerData ps in playerList)
            {
                ps.player.WarpClientRPC(GetGameplaySpawnPosition(), isDropItem: true);
            }
        }
    }

    [ServerRpc]
    void StartPVPGameServerRPC()
    {
        InitPool();
        StartPVPGameClientRPC();
    }

    [ClientRpc]
    void StartPVPGameClientRPC()
    {
        ResetValueBeforeGame();
        pvpLocationCount = 0;
        Debug.Log("PVP ENDED");
        gameState = GameState.LOBBY;
        if (IsServer)
        {
            foreach (PlayerData ps in playerList)
            {
                ps.player.WarpClientRPC(GetGameplaySpawnPosition(), isDropItem: true);
                pvpLocationCount = (pvpLocationCount + 1)%pvpGameplayLocations.Length;
            }
        }
        UpdateUI();
    }

    private void InitPool()
    {
        if (!ObjectPool.Instance.CheckPoolInitialized())
        {
            ObjectPool.Instance.PrewarmSpawn();
        }
    }

    private void ResetValueBeforeGame()
    {
        winText.SetActive(false);
        loseText.SetActive(false);
        doneScore = 0;
        failScore = 0;
        NetworkManagerUI.instance.ResetObjectives();
        ObjectPool.Instance.RecallAllObjects();
        gameState = GameState.INGAME;
    }

    void Update()
    {
        TimerCounting();
    }

    void TimerCounting()
    {
        if (gameState != GameState.INGAME || gameMode != GameMode.COOP) return;
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
        if(gameMode == GameMode.PVP)
        {
            return pvpGameplayLocations[pvpLocationCount].position + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
        }
        return GetLobbySpawnPosition();
    }

    public void UpdateCOOPGameScore(bool isDone)
    {
        if(isDone)
        {
            ++doneScore;
            if (doneScore >= winTargetScore)
            {
                EndCOOPGame(true);
            }
            else
            {
                SoundManager.Play("get_point");
            }
        }
        else
        {
            ++failScore;
            if (failScore >= loseTargetScore)
            {
                EndCOOPGame(false);
            }
            else
            {
                SoundManager.Play("failed");
            }
        }
    }

    public void EndCOOPGame(bool isWin)
    {
        gameState = GameState.LOBBY;
        SoundManager.PlayMusic("lobby");
        NetworkManagerUI.instance.UpdateCanvas(gameState);
        if (isWin)
        {
            Debug.Log("YOU WIN");
            winText.SetActive(true);
            SoundManager.Play("win");
        }
        else
        {
            Debug.Log("YOU LOSE");
            loseText.SetActive(true);
            SoundManager.Play("lose");
        }
        if (IsServer)
        {
            foreach (PlayerData ps in playerList)
            {
                ps.player.WarpClientRPC(GetLobbySpawnPosition(), isDropItem: true);
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
                    return true;
                }
            }
            PlaySoundClientRPC("approve" + Random.Range(1, 4), obj.transform.position);
            return true;
        }
        PlaySoundClientRPC("deny", obj.transform.position);
        return false;
    }

    [ClientRpc]
    private void PlaySoundClientRPC(string name, Vector3 position)
    {
        SoundManager.PlayNew(name, position);
    }

    public void SetTimer(float time, bool isRelax)
    {
        if (!IsServer) return;

        SetTimerClientRPC(time, isRelax);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateGameStateServerRPC()
    {
        UpdateGameStateClientRPC(gameState, gameMode);
        UpdateGameScoreClientRPC(doneScore, failScore, winTargetScore, loseTargetScore);
        UpdateObjectiveClientRPC(currentObjective.score, currentObjective.targetScore);
        UpdateThemeClientRPC(SoundManager.GetCurrentMusicName());
        SetTimerClientRPC(timer, isRelax);
    }

    [ClientRpc]
    void UpdateGameStateClientRPC(GameState state, GameMode gameMode)
    {
        if (IsServer) return;
        UpdateGameState(state);
        this.gameMode = gameMode;
    }

    private void UpdateGameState(GameState state)
    {
        gameState = state;
        UpdateUI();
    }

    [ClientRpc]
    void UpdateGameScoreClientRPC(ushort doneScore, ushort failScore, ushort winTargetScore, ushort loseTargetScore)
    {
        if (IsServer) return;
        this.doneScore = doneScore;
        this.failScore = failScore;
        this.winTargetScore = winTargetScore;   
        this.loseTargetScore = loseTargetScore;
    }

    [ClientRpc]
    void UpdateThemeClientRPC(string name)
    {
        if (IsServer) return;
        SoundManager.PlayMusic(name);
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
