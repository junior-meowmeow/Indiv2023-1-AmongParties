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
    [SerializeField] private Queue<GameObject> tempPool;

    public static ObjectPool instance;

    void Awake()
    {
        instance = this;
    }

    public void PrewarmSpawn()
    {
        //poolDict = new Dictionary<string, Queue<GameObject>>();
        ResetDictClientRPC();

        foreach (Pool pool in pools)
        {
            //Queue<GameObject> objectPool = new Queue<GameObject>();
            ResetTempPoolClientRPC();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab, objectParent);
                obj.GetComponent<NetworkObject>().Spawn(destroyWithScene:true);
                //obj.GetComponent<SpawnableObject>().SetActiveClientRPC(false);
                //objectPool.Enqueue(obj);
                AddPoolClientRPC(obj);
            }

            //poolDict.Add(pool.tag, objectPool);
            AddDictClientRPC(pool.tag);
        }
    }

    [ClientRpc]
    public void ResetDictClientRPC()
    {
        poolDict = new Dictionary<string, Queue<GameObject>>();
    }

    [ClientRpc]
    public void AddDictClientRPC(string tag)
    {
        poolDict.Add(tag, tempPool);
    }

    [ClientRpc]
    public void ResetTempPoolClientRPC()
    {
        tempPool = new Queue<GameObject>();
    }

    [ClientRpc]
    public void AddPoolClientRPC(NetworkObjectReference obj_ref)
    {
        if (obj_ref.TryGet(out NetworkObject obj))
        {
            obj.gameObject.SetActive(false);
            tempPool.Enqueue(obj.gameObject);
        }
        else
        {
            Debug.Log("Object Not Found!!!");
        }
    }

    public GameObject SpawnObject(string tag, Vector3 pos, Quaternion rot)
    {
        //Debug.Log(poolDict.Count);

        if (!poolDict.ContainsKey(tag))
        {
            Debug.Log("no tag match : " + tag);
            return null;
        }

        GameObject obj = poolDict[tag].Dequeue();

        obj.SetActive(true);
        obj.transform.SetPositionAndRotation(pos, rot);

        poolDict[tag].Enqueue(obj);

        return obj;
    }
}
