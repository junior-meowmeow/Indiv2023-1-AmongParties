using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ObjectSpawner : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPos;

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
            int rand = Random.Range(0, spawnPos.Length);
            SpawnObjectClientRPC("Cube", rand);
        }
    }

    [ClientRpc]
    public void SpawnObjectClientRPC(string tag, int spawnIdx)
    {
        GameObject obj = ObjectPool.instance.SpawnObject(tag, spawnPos[spawnIdx].position, spawnPos[spawnIdx].rotation);
    }
}
