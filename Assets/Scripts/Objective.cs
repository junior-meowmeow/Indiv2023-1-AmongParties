using UnityEngine;
using Unity.Netcode;

public enum ObjectType {Box, Sphere}
public enum ObjectColor {RED, BLUE} 

[System.Serializable]
public class Objective : INetworkSerializeByMemcpy
{
    public int id;
    public ObjectType type;
    public ObjectColor color;
    public bool isComplete;

    [HideInInspector] public float startTime; //not used yet
    [HideInInspector] public float duration; //not used yet

    public int score;
    public int targetScore;

    public void SetUp()
    {
        id = Random.Range(0, 2);
        type = (ObjectType)Random.Range(0, 2);
        color = (ObjectColor)Random.Range(0, 2);
        startTime = Time.time;
        duration = 20 + 5 * Random.Range(0, 2);
        score = 0;
        targetScore = Random.Range(3, 6);
        isComplete = score >= targetScore;
    }

    public void Update(int id, int score, int targetScore)
    {
        SetID(id);
        this.score = score;
        this.targetScore = targetScore;
        isComplete = score >= targetScore;
    }

    public bool ScoreObject(PickableObject obj)
    {
        if ((id == 0 && obj.type == type) ||
            (id == 1 && obj.color == color))
        {
            AddScore(1);
            return true;
        }
        return false;
    }

    void AddScore(int add)
    {
        score += add;
        if (score >= targetScore)
        {
            isComplete = true;
        }
    }

    public int GetID()
    {
        if (id == 0) return (int)type;
        return 10 + (int)color;
    }

    void SetID(int id)
    {
        this.id = id/10;
        type = (ObjectType)(id%10);
        color = (ObjectColor)(id%10);
    }
}
