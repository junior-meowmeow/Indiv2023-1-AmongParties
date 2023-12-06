using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ObjectSpawner : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPos;
    [SerializeField] private Color[] colorList;

    public static ObjectSpawner instance;

    void Awake()
    {
        instance = this;
    }

    [ServerRpc]
    public void StartObjectiveServerRPC(Objective objective)
    {
        for (int i = 0; i < objective.targetScore; i++)
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
