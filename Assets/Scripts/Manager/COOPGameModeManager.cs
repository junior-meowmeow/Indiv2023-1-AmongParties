using UnityEngine;
using Unity.Netcode;

public class COOPGameModeManager : GameModeManager
{

    [SerializeField] private Transform gameplayLocation;

    [SerializeField] private Objective currentObjective;
    [SerializeField] private ushort doneScore = 0;
    [SerializeField] private ushort failScore = 0;
    [SerializeField] private ushort winTargetScore = 3;
    [SerializeField] private ushort loseTargetScore = 2;
    [SerializeField] private float relaxTime = 5f;

    [SerializeField] private float timer;
    [SerializeField] private bool isRelax;

    public override GameMode GetGameMode()
    {
        return GameMode.COOP;
    }

    public override void StartGameServer()
    {
        base.StartGameServer();
        SetTimerClientRPC(10f, isRelax: true);
        ObjectPool.Instance.InitPool();
        StartGameClientRPC(winTargetScore, loseTargetScore);
    }

    [ClientRpc]
    private void StartGameClientRPC(ushort winScore, ushort loseScore)
    {
        ResetValueBeforeGame();
        winTargetScore = winScore;
        loseTargetScore = loseScore;
        SoundManager.PlayMusic("coop");
        ObjectiveUIManager.Instance.SetTimerActive(true);
        UpdateObjectiveUI();
        if (IsServer)
        {
            foreach (PlayerData ps in GameDataManager.Instance.GetPlayerList())
            {
                ps.player.WarpClientRPC(GetSpawnPosition(), isDropItem: true);
            }
        }
    }

    private void ResetValueBeforeGame()
    {
        GameplayManager.Instance.SetWinText("YOU WIN", false);
        GameplayManager.Instance.SetLoseText("YOU LOSE", false);
        doneScore = 0;
        failScore = 0;
        ObjectiveUIManager.Instance.ResetObjectives();
    }

    private void UpdateObjectiveUI()
    {
        ObjectiveUIManager.Instance.UpdateTimer(timer, isRelax);
        ObjectiveUIManager.Instance.UpdateObjective(currentObjective);
        GameplayManager.Instance.SetMainObjectiveText("Complete " + winTargetScore + " Objectives to Win (Maximum Fail :  " + (loseTargetScore - 1) + " times)");
    }

    public override Vector3 GetSpawnPosition()
    {
        return gameplayLocation.position + new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-8f, 8f));
    }

    public override void UpdateGameMode()
    {
        base.UpdateGameMode();
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            ObjectiveUIManager.Instance.UpdateTimer(timer, isRelax);
        }
        else if (IsServer)
        {
            // Time is out.
            if (isRelax)
            {
                NextObjectiveServer();
            }
            else
            {
                EndObjectiveClientRPC();
                SetTimerClientRPC(relaxTime, true);
            }
        }
    }

    private void NextObjectiveServer()
    {
        currentObjective = new Objective();
        currentObjective.SetUp();

        NextObjectiveClientRPC(currentObjective.GetID(), currentObjective.score, currentObjective.targetScore);
        SetTimerClientRPC(currentObjective.duration, false);

        ObjectSpawner.Instance.StartObjective(currentObjective.GetID(), currentObjective.targetScore);
    }

    [ClientRpc]
    private void NextObjectiveClientRPC(int id, ushort score, ushort targetScore)
    {
        currentObjective.Update(id, score, targetScore);

        ObjectiveUIManager.Instance.StartObjective(currentObjective);
    }

    [ClientRpc]
    void EndObjectiveClientRPC()
    {
        ObjectiveUIManager.Instance.EndObjective(currentObjective);
        UpdateGameScore(currentObjective.isComplete);
    }

    [ClientRpc]
    void SetTimerClientRPC(float time, bool isRelax)
    {
        timer = time;
        this.isRelax = isRelax;
        ObjectiveUIManager.Instance.UpdateTimer(time, isRelax);
    }

    public override bool GetObject(PickableObject obj, string locationName)
    {
        if (!IsServer || isRelax) return false;

        if (currentObjective.ScoreObject(obj, locationName))
        {
            SendObjectiveClientRPC(currentObjective.score, currentObjective.targetScore);
            if (currentObjective.isComplete)
            {
                if (doneScore + 1 >= winTargetScore)
                {
                    EndGameClientRPC(true);
                    return true;
                }
            }
            PlaySoundClientRPC("approve" + Random.Range(1, 4), obj.transform.position);
            PlayParticleClientRPC(obj.transform.position);
            return true;
        }
        PlaySoundClientRPC("deny", obj.transform.position);
        return false;
    }

    private void UpdateGameScore(bool isDone)
    {
        if (isDone)
        {
            ++doneScore;
            if (doneScore >= winTargetScore)
            {
                EndGameClient(true);
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
                EndGameClient(false);
            }
            else
            {
                SoundManager.Play("failed");
            }
        }
    }

    private void EndGameClient(bool isWin)
    {
        ObjectPool.Instance.RecallAllObjects();
        if (isWin)
        {
            Debug.Log("YOU WIN");
            GameplayManager.Instance.SetWinText("YOU WIN", true);
            SoundManager.Play("win");
        }
        else
        {
            Debug.Log("YOU LOSE");
            GameplayManager.Instance.SetLoseText("YOU LOSE", true);
            SoundManager.Play("lose");
        }
        if (IsServer)
        {
            WarpAllPlayerToLobbyServer();
        }
        ObjectiveUIManager.Instance.SetTimerActive(false);
        AfterGameEndClient();
    }

    [ClientRpc]
    void EndGameClientRPC(bool isWin)
    {
        EndGameClient(isWin);
    }

    public Objective GetCurrentObjective()
    {
        return currentObjective;
    }

    public void SetCurrentObjective(Objective objective)
    {
        currentObjective = objective;
    }

    public override void RequestGameModeUpdate(ClientRpcParams clientRpcParams = default)
    {
        base.RequestGameModeUpdate();
        SendGameScoreClientRPC(doneScore, failScore, winTargetScore, loseTargetScore, clientRpcParams);
        SendObjectiveClientRPC(currentObjective.score, currentObjective.targetScore, clientRpcParams);
        SetTimerClientRPC(timer, isRelax);
    }

    [ClientRpc]
    void SendGameScoreClientRPC(ushort doneScore, ushort failScore, ushort winTargetScore, ushort loseTargetScore, ClientRpcParams clientRpcParams = default)
    {
        if (IsServer) return;
        this.doneScore = doneScore;
        this.failScore = failScore;
        this.winTargetScore = winTargetScore;
        this.loseTargetScore = loseTargetScore;
    }

    [ClientRpc]
    void SendObjectiveClientRPC(ushort score, ushort targetScore, ClientRpcParams clientRpcParams = default)
    {
        currentObjective.Update(score, targetScore);
        UpdateObjectiveUI();
    }

}
