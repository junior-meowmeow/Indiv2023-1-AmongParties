using UnityEngine;
using Unity.Netcode;

public enum ObjectType {ANY, Box, Sphere, Weapon}
public enum ObjectColor {ANY, RED, BLUE, YELLOW, ORANGE}

[System.Serializable]
public class Objective : INetworkSerializeByMemcpy
{
    public byte req; // 0 = specific type, 1 = specific color, 2 = specific both type and color
    public ObjectType objectType;
    public ObjectColor objectColor;
    public byte locationId;
    public bool isComplete;
    [SerializeField] private static string[] locationList = {"Base", "Furnace", "Garbage"};
    [SerializeField] private static string[] typeList = {"Object", "Crate", "Core"};
    [SerializeField] private static string[] colorList = {"Any", "Red", "Blue", "Yellow", "Orange"};
    [SerializeField] private static string[] allTypeList = { "Object", "Crate", "Core", "Crowbar", "Hammer" };

    [SerializeField] private static ushort[] targetScoreList = {2, 3, 3, 3, 4, 4, 5, 5, 6, 7, 8};

    [HideInInspector] public float startTime; //not used yet
    [HideInInspector] public float duration; //not used yet

    public ushort score;
    public ushort targetScore;

    public void SetUp()
    {
        req = (byte)Random.Range(1, 3);
        objectType = req == 1? (ObjectType)Random.Range(1, 3) : 0;
        objectColor = req == 2? (ObjectColor)Random.Range(1, 4) : 0;
        locationId = (byte)Random.Range(0, locationList.Length);
        startTime = Time.time;
        score = 0;
        int scoreIdx = GameplayManager.Instance.GetCurrentGameModeManager().GetCurrentRound() +
            (GameDataManager.Instance.GetPlayerCount() - 1) * 2;
        targetScore = targetScoreList[scoreIdx < targetScoreList.Length? scoreIdx : targetScoreList.Length];
        duration = 45 + GameplayManager.Instance.GetCurrentGameModeManager().GetCurrentRound() * 5;
        isComplete = score >= targetScore;
    }

    public void Update(int id, ushort score, ushort targetScore)
    {
        SetUp(id);
        this.score = score;
        this.targetScore = targetScore;
        isComplete = score >= targetScore;
    }

    public void Update(ushort score, ushort targetScore)
    {
        this.score = score;
        this.targetScore = targetScore;
        isComplete = score >= targetScore;
    }

    public bool ScoreObject(PickableObject obj, string location)
    {
        if ((obj.objectType == objectType || objectType == ObjectType.ANY) &&
            (obj.objectColor == objectColor || objectColor == ObjectColor.ANY) &&
            locationList[locationId] == location)
        {
            AddScore(1);
            return true;
        }
        return false;
    }

    void AddScore(ushort add)
    {
        score += add;
        if (score >= targetScore)
        {
            isComplete = true;
        }
    }

    public void SetUp(int id)
    {
        req = (byte)(id/1000);
        id %= 1000;
        objectType = (ObjectType)(id/100);
        id %= 100;
        objectColor = (ObjectColor)(id/10);
        id %= 10;
        locationId = (byte)id;
        startTime = Time.time;
        score = 0;
        targetScore = (ushort)Random.Range(2, 5);
        duration = 30 + 10 * targetScore;
        isComplete = score >= targetScore;
    }

    public int GetID()
    {
        return req * 1000 + (int)objectType * 100 + (int)objectColor * 10 + locationId;
    }

    public static string GetObjectType(byte objectTypeId)
    {
        return allTypeList[objectTypeId];
    }

    public byte GetObjectTypeId()
    {
        if (objectType == 0) return (byte)Random.Range(1, typeList.Length);
        return (byte)objectType;
    }

    public byte GetObjectColorId()
    {
        if (objectColor == 0) return (byte)Random.Range(1, colorList.Length);
        return (byte)objectColor;
    }

    public string GetObjectiveDescription()
    {
        return $"Deliver {colorList[(int)objectColor]} {typeList[(int)objectType]} to {locationList[locationId]}";
    }

    public static byte GetRandomObjectTypeId()
    {
        return (byte)Random.Range(1, allTypeList.Length);
    }

    public static byte GetRandomObjectColorId()
    {
        return (byte)Random.Range(1, colorList.Length);
    }
    
}
