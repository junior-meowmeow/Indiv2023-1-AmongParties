using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SyncObjectManager : NetworkBehaviour
{
    private static SyncObjectManager instance;
    public static SyncObjectManager Instance => instance;

    [SerializeField] private List<SyncObject> inSceneObjectList;
    [SerializeField] private List<SyncObject> objectList;
    private Dictionary<SyncObject, ushort> objectToKey = new();
    private ushort count = 0;
    [SerializeField] private ushort list_count = 0;
    [SerializeField] private ushort sync_count = 0;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        ResetObjectList();
    }

    private void ResetObjectList()
    {
        objectList.Clear();
        objectToKey.Clear();
        count = 0;
    }

    private void Start()
    {
        // Initialize In-Scene Objects
        inSceneObjectList.AddRange(FindObjectsOfType<SyncObject>(true));
        foreach (SyncObject obj in inSceneObjectList)
        {
            obj.isInScene = true;
        }
    }

    public void NetworkInitialize()
    {
        if (IsServer)
        {
            foreach(SyncObject obj in inSceneObjectList)
            {
                obj.GetComponent<NetworkObject>().Despawn(destroy: false);
                obj.isInScene = false;
                obj.GetComponent<NetworkObject>().Spawn(destroyWithScene:true);
            }
            RecreateObjectList();
        }
        else
        {
            SoundManager.SetSfxEnable(false);
            list_count = 0;
            sync_count = 0;
            RequestObjectList();
        }
    }

    public SyncObject GetSyncObject(ushort key)
    {
        return objectList[key];
    }

    public ushort GetKey(SyncObject obj)
    {
        return objectToKey[obj];
    }

    public ushort GetObjectCount()
    {
        return count;
    }

    [ContextMenu(itemName: "Recreate Object List")]
    public void RecreateObjectList()
    {
        if (!IsServer) return;
        ResetObjectList();
        objectList.AddRange(FindObjectsOfType<SyncObject>(true));
        NetworkObjectReference[] refList = new NetworkObjectReference[objectList.Count];
        for (ushort i = count; i < objectList.Count; i++)
        {
            objectToKey[objectList[i]] = i;
            refList[i] = objectList[i].GetNetworkObject();
            count++;
        }
        RecreateObjectListClientRPC(refList);
    }

    [ClientRpc]
    public void RecreateObjectListClientRPC(NetworkObjectReference[] referenceList)
    {
        if (IsServer) return;
        ResetObjectList();
        foreach (NetworkObjectReference reference in referenceList)
        {
            if (reference.TryGet(out NetworkObject obj))
            {
                objectList.Add(obj.GetComponent<SyncObject>());
            }
            else
            {
                Debug.Log("NO OBJECT FROM REFERENCE!!!");
                objectList.Add(null);
            }
        }
        for (ushort i = count; i < objectList.Count; i++)
        {
            if (objectList[i] == null) return;
            objectToKey[objectList[i]] = i;
            count++;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddObjectListServerRPC(NetworkObjectReference obj_ref)
    {
        AddObjectListClientRPC(obj_ref);
    }

    [ClientRpc]
    public void AddObjectListClientRPC(NetworkObjectReference obj_ref)
    {
        if (obj_ref.TryGet(out NetworkObject obj))
        {
            objectList.Add(obj.GetComponent<SyncObject>());
        }
        else
        {
            Debug.Log("NO OBJECT FROM REFERENCE!!!");
            objectList.Add(null);
        }

        if (objectList[count] != null)
        {
            objectToKey[objectList[count]] = count;
        }
        count++;
    }

    [ContextMenu(itemName:"Request Object List")]
    public void RequestObjectList()
    {
        SendObjectListServerRPC();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendObjectListServerRPC(ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientId))
        {
            Debug.Log("Sending Object List Back...");
            //var client = NetworkManager.ConnectedClients[clientId];
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };
            NetworkObjectReference[] refList = new NetworkObjectReference[objectList.Count];
            for (ushort i = 0; i < objectList.Count; i++)
            {
                if (objectList[i] == null)
                {
                    refList[i] = default;
                    continue;
                }
                refList[i] = objectList[i].GetNetworkObject();
            }
            SendObjectListClientRPC(refList, clientRpcParams);
        }
    }

    [ClientRpc]
    public void SendObjectListClientRPC(NetworkObjectReference[] referenceList, ClientRpcParams clientRpcParams = default)
    {
        ResetObjectList();
        foreach (NetworkObjectReference reference in referenceList)
        {
            if (reference.TryGet(out NetworkObject obj))
            {
                objectList.Add(obj.GetComponent<SyncObject>());
                //Debug.Log(obj.gameObject.name);
            }
            else
            {
                objectList.Add(null);
            }
        }
        for (ushort i = count; i < objectList.Count; i++)
        {
            if (objectList[count] != null)
            {
                objectToKey[objectList[count]] = count;
            }
            count++;
        }
        SyncInitialStates();
        ObjectPool.Instance.CheckLateJoinServerRPC();
    }

    [ContextMenu(itemName: "Synchronize Objects")]
    private void SyncInitialStates()
    {
        foreach (SyncObject obj in objectList)
        {
            if (obj == null) continue;
            //Debug.Log("Syncing " + obj.name);
            obj.SyncObjectServerRPC(objectToKey[obj]);
            list_count++;
        }
    }

    public void CountSynchronize()
    {
        sync_count++;
        if (sync_count == list_count)
        {
            // Bad Practice But It's Necessary right now.
            Invoke(nameof(OnInitialSyncFinish), 0.2f);
        }
    }

    private void OnInitialSyncFinish()
    {
        SoundManager.SetSfxEnable(true);
    }

}
