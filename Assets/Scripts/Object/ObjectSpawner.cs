using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ObjectSpawner : NetworkBehaviour
{
    [SerializeField] private Transform[] spawnPos;
    [SerializeField] private Color[] colorList;

    public static ObjectSpawner instance;

    void Awake()
    {
        instance = this;
    }

    public void StartObjective(int id, int targetScore)
    {
        Objective objective = new();
        objective.SetUp(id);
        for (int i = 0; i < targetScore; i++)
        {
            int spawnIdx = 0;
            if (objective.locationId == 0) spawnIdx = Random.Range(1, spawnPos.Length);
            SpawnObjectClientRPC(objective.GetObjectType(), objective.GetObjectTypeId(), objective.GetObjectColorId(), spawnIdx);
        }
    }

    [ClientRpc]
    public void SpawnObjectClientRPC(string tag, byte typeId, byte colorId, int spawnIdx)
    {
        GameObject obj = ObjectPool.instance.SpawnObject(tag, spawnPos[spawnIdx].position, spawnPos[spawnIdx].rotation);
        if(obj != null)
        {
            obj.GetComponent<PickableObject>().SetUp(typeId, colorId, colorList[colorId - 1]);
        }
    }
}
