using UnityEngine;
using Unity.Netcode;

public enum ObjectType {ANY, Box, Sphere, Weapon}
public enum ObjectColor {ANY, RED, BLUE, YELLOW, ORANGE}

[System.Serializable]
public class Objective : INetworkSerializeByMemcpy
{
    public int req; // 0 = specific type, 1 = specific color, 2 = specific both type and color
    public ObjectType type;
    public ObjectColor color;
    public int locationId;
    public bool isComplete;
    [SerializeField] private string[] locationList = {"Base", "Furnace", "Garbage"};
    [SerializeField] private string[] typeList = {"Object", "Crate", "Core"};
    [SerializeField] private string[] colorList = {"Any", "Red", "Blue", "Yellow", "Orange"};

    [HideInInspector] public float startTime; //not used yet
    [HideInInspector] public float duration; //not used yet

    public int score;
    public int targetScore;

    public void SetUp()
    {
        req = Random.Range(0, 3);
        type = req == 1? (ObjectType)Random.Range(1, 3) : 0;
        color = req == 2? (ObjectColor)Random.Range(1, 4) : 0;
        locationId = Random.Range(0, locationList.Length);
        startTime = Time.time;
        score = 0;
        targetScore = Random.Range(2, 5);
        duration = 30 + 10 * targetScore;
        isComplete = score >= targetScore;
    }

    public void Update(int id, int score, int targetScore)
    {
        SetUp(id);
        this.score = score;
        this.targetScore = targetScore;
        isComplete = score >= targetScore;
    }

    public void Update(int score, int targetScore)
    {
        this.score = score;
        this.targetScore = targetScore;
        isComplete = score >= targetScore;
    }

    public bool ScoreObject(PickableObject obj, string location)
    {
        if ((obj.type == type || obj.type == ObjectType.ANY) &&
            (obj.color == color || obj.color == ObjectColor.ANY) &&
            locationList[locationId] == location)
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
        return req * 1000 + (int)type * 100 + (int)color * 10 + locationId;
    }

    public string GetObjectType()
    {
        if ((int)type == 0) return typeList[Random.Range(1, typeList.Length)];
        return typeList[(int)type];
    }

    public int GetObjectColorId()
    {
        if ((int)color == 0) return Random.Range(1, colorList.Length);
        return (int)color;
    }

    public string GetObjectiveDescription()
    {
        return $"Deliver {colorList[(int)color]} {typeList[(int)type]} to {locationList[locationId]}";
    }

    void SetUp(int id)
    {
        req = id/1000;
        id %= 1000;
        type = (ObjectType)(id/100);
        id %= 100;
        color = (ObjectColor)(id/10);
        id %= 10;
        locationId = id;

    }
}
