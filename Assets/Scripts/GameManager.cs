using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum Objective {FIND_OBJECT}

public class GameManager : NetworkBehaviour
{
    [SerializeField] private Objective objective;
    [SerializeField] private int score;
    [SerializeField] private int targetScore;
    [SerializeField] private float timer;
    [SerializeField] private bool isRelax;

    public static GameManager instance;

    void Awake()
    {
        instance = this;
    }

    public void GameStart()
    {
        if (!IsServer) return;

        SetTimer(20f, true);
        NextObjective();
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
}
