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
        Debug.Log("1");
        StartObjectiveServerRPC(id, targetScore);
        Debug.Log("3");
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartObjectiveServerRPC(int id, int targetScore)
    {
        Debug.Log("2");
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
    public void SpawnObjectClientRPC(string tag, int typeId, int colorId, int spawnIdx)
    {
        Debug.Log("ObjSpawner : " + typeId + " " + colorId);
        GameObject obj = ObjectPool.instance.SpawnObject(tag, spawnPos[spawnIdx].position, spawnPos[spawnIdx].rotation);
        obj.GetComponent<PickableObject>().SetUp(typeId, colorId, colorList[colorId - 1]);
    }
}
