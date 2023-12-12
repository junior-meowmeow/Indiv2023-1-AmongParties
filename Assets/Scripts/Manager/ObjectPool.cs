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
    private static ObjectPool instance;
    public static ObjectPool Instance => instance;

    [SerializeField] private List<Pool> pools;
    [SerializeField] private Dictionary<string, Queue<GameObject>> poolDict;
    [SerializeField] private Transform spawningTransform;
    [SerializeField] private Queue<GameObject> tempPool;
    [SerializeField] private bool isLateJoin = false;
    [SerializeField] private bool isPoolReady = false;
    [SerializeField] private bool isPoolInitialized = false;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (spawningTransform == null)
        {
            spawningTransform = transform.root;
        }
    }

    public bool CheckPoolInitialized()
    {
        return isPoolInitialized;
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheckLateJoinServerRPC(ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientId))
        {
            //var client = NetworkManager.ConnectedClients[clientId];
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };
            CheckLateJoinClientRPC(isPoolInitialized);
        }
    }

    [ClientRpc]
    private void CheckLateJoinClientRPC(bool isLateJoin, ClientRpcParams clientRpcParams = default)
    {
        this.isLateJoin = isLateJoin;
    }

    void Update()
    {
        if(isLateJoin && !isPoolInitialized)
        {
            CheckPool();
        }
    }

    public void CheckPool()
    {
        CheckPoolReadyServerRPC(SyncObjectManager.Instance.GetObjectCount());
        if(isPoolReady)
        {
            poolDict = new Dictionary<string, Queue<GameObject>>();
            RequestPoolServerRPC();
            isLateJoin = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CheckPoolReadyServerRPC(ushort count,ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientId))
        {
            //var client = NetworkManager.ConnectedClients[clientId];
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };
            CheckPoolReadyClientRPC(SyncObjectManager.Instance.GetObjectCount() == count);
        }
    }

    [ClientRpc]
    private void CheckPoolReadyClientRPC(bool isPoolReady, ClientRpcParams clientRpcParams = default)
    {
        this.isPoolReady = isPoolReady;
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
                GameObject obj = Instantiate(pool.prefab, spawningTransform.position, spawningTransform.rotation);
                obj.GetComponent<NetworkObject>().Spawn(destroyWithScene:true);
                //obj.GetComponent<SpawnableObject>().SetActiveClientRPC(false);
                //objectPool.Enqueue(obj);
                AddPoolClientRPC(obj);
            }

            //poolDict.Add(pool.tag, objectPool);
            AddDictClientRPC(pool.tag);
        }
        isPoolInitialized = true;
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

    public void RecallAllObjects()
    {
        foreach (Queue<GameObject> queue in poolDict.Values)
        {
            IEnumerator<GameObject> enumerator = queue.GetEnumerator();
            while (enumerator.MoveNext())
            {
                enumerator.Current.SetActive(false);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPoolServerRPC(ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientId))
        {
            //var client = NetworkManager.ConnectedClients[clientId];
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };
            foreach (var keyValue in poolDict)
            {
                string tag = keyValue.Key;
                Queue<GameObject> queue = keyValue.Value;
                ushort[] obj_keys = new ushort[queue.Count];
                bool[] obj_actives = new bool[queue.Count];
                IEnumerator<GameObject> enumerator = queue.GetEnumerator();
                ushort count = 0;
                while (enumerator.MoveNext())
                {
                    GameObject obj = enumerator.Current;
                    obj_keys[count] = SyncObjectManager.Instance.GetKey(obj.GetComponent<SyncObject>());
                    obj_actives[count] = obj.activeInHierarchy;
                    count++;
                }
                SyncPooledObjectsClientRPC(obj_keys, obj_actives, tag, clientRpcParams);
            }
        }
    }

    [ClientRpc]
    private void SyncPooledObjectsClientRPC(ushort[] obj_keys, bool[] obj_actives, string tag, ClientRpcParams clientRpcParams = default)
    {
        tempPool = new Queue<GameObject>();
        for (int i = 0; i < obj_keys.Length; i++)
        {
            GameObject obj = SyncObjectManager.Instance.GetSyncObject(obj_keys[i]).gameObject;
            obj.SetActive(obj_actives[i]);
            //obj.transform.SetParent(objectParent);
            tempPool.Enqueue(obj);
        }
        poolDict.Add(tag, tempPool);
        isPoolInitialized = true;
    }

}
