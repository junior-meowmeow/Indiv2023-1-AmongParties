using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[Serializable]
struct Pool
{
    public string tag;
    public GameObject prefab;
    public int size;
}

public class ObjectPool : NetworkBehaviour
{
    [SerializeField] private List<Pool> pools;
    [SerializeField] private Dictionary<string, Queue<GameObject>> poolDict;
    [SerializeField] private Transform objectParent;

    public static ObjectPool instance;

    void Awake()
    {
        instance = this;
    }

    public void PrewarmSpawn()
    {
        poolDict = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab, objectParent);
                obj.GetComponent<NetworkObject>().Spawn();
                obj.GetComponent<SpawnableObject>().SetActiveClientRPC(false);
                objectPool.Enqueue(obj);
            }

            poolDict.Add(pool.tag, objectPool);
        }
    }
    
    public GameObject SpawnObject(string tag, Vector3 pos, Quaternion rot)
    {
        Debug.Log(poolDict.Count);

        if (!poolDict.ContainsKey(tag)) return null;

        GameObject obj = poolDict[tag].Dequeue();

        obj.SetActive(true);
        obj.transform.position = pos;
        obj.transform.rotation = rot;

        poolDict[tag].Enqueue(obj);

        return obj;
    }
}
