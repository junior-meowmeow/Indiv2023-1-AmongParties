using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ObjectSpawner : NetworkBehaviour
{
    private static ObjectSpawner instance;
    public static ObjectSpawner Instance => instance;

    [SerializeField] private bool isEnableRandomSpawn = true;
    [SerializeField] private Transform[] spawnPos;
    [SerializeField] private Color[] colorList;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
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
            byte typeId = objective.GetObjectTypeId();
            SpawnObjectClientRPC(Objective.GetObjectType(typeId), typeId, objective.GetObjectColorId(), spawnIdx);
        }
        if(isEnableRandomSpawn)
        {
            int range = Random.Range(0, 3);
            for (int i = 0; i < range; i++)
            {
                int spawnIdx = 0;
                if (objective.locationId == 0) spawnIdx = Random.Range(1, spawnPos.Length);
                byte typeId = Objective.GetRandomObjectTypeId();
                SpawnObjectClientRPC(Objective.GetObjectType(typeId), typeId, objective.GetObjectColorId(), spawnIdx);
            }
        }
    }

    [ClientRpc]
    private void SpawnObjectClientRPC(string tag, byte typeId, byte colorId, int spawnIdx)
    {
        GameObject obj = ObjectPool.Instance.SpawnObject(tag, spawnPos[spawnIdx].position, spawnPos[spawnIdx].rotation);
        if(obj != null)
        {
            obj.GetComponent<PickableObject>().SetUp(typeId, colorId, GetColorFromId(colorId));
        }
    }

    public Color GetColorFromId(byte colorId)
    {
        if(colorId < 0)
        {
            colorId = 0;
        }
        return colorList[colorId];
    }
}
