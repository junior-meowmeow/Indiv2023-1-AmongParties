using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class SyncObjectManager : NetworkBehaviour
{
    public List<SyncObject> inSceneObjectList;
    public List<SyncObject> objectList;
    public Dictionary<SyncObject, ushort> objectToKey = new();
    public ushort count = 0;
    public static SyncObjectManager instance;

    void Awake()
    {
        instance = this;
        objectList.Clear();
        objectToKey.Clear();
        count = 0;
    }
    void Start()
    {
        inSceneObjectList.AddRange(FindObjectsOfType<SyncObject>(true));
    }

    public void Initialize()
    {
        if (IsServer)
        {
            foreach(SyncObject obj in inSceneObjectList)
            {
                obj.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
            }
            RecreateObjectList();
        }
        else
        {
            foreach (SyncObject obj in inSceneObjectList)
            {
                Destroy(obj);
            }
            RequestObjectList();
        }
    }

    [ContextMenu(itemName: "Recreate Object List")]
    public void RecreateObjectList()
    {
        if (!IsServer) return;
        objectList.Clear();
        objectToKey.Clear();
        count = 0;
        objectList.AddRange(FindObjectsOfType<SyncObject>(true));
        NetworkObjectReference[] refList = new NetworkObjectReference[objectList.Count];
        for (ushort i = count; i < objectList.Count; i++)
        {
            objectToKey[objectList[i]] = i;
            refList[i] = objectList[i].net_obj;
            count++;
        }
        RecreateObjectListClientRPC(refList);
    }

    [ClientRpc]
    public void RecreateObjectListClientRPC(NetworkObjectReference[] referenceList)
    {
        if (IsServer) return;

        objectList.Clear();
        objectToKey.Clear();
        count = 0;
        foreach (NetworkObjectReference reference in referenceList)
        {
            if (reference.TryGet(out NetworkObject obj))
            {
                objectList.Add(obj.GetComponent<SyncObject>());
            }
            else
            {
                print("NO OBJECT FROM REFERENCE!!!");
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
            print("Sending Object List Back...");
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
                refList[i] = objectList[i].net_obj;
            }
            SendObjectListClientRPC(refList, clientRpcParams);
        }
    }

    [ClientRpc]
    public void SendObjectListClientRPC(NetworkObjectReference[] referenceList, ClientRpcParams clientRpcParams = default)
    {
        objectList.Clear();
        objectToKey.Clear();
        count = 0;
        foreach (NetworkObjectReference reference in referenceList)
        {
            if (reference.TryGet(out NetworkObject obj))
            {
                objectList.Add(obj.GetComponent<SyncObject>());
            }
            else
            {
                print("NO OBJECT FROM REFERENCE!!!");
                objectList.Add(null);
            }
        }
        for (ushort i = count; i < objectList.Count; i++)
        {
            if (objectList[i] == null) return;
            objectToKey[objectList[i]] = i;
            count++;
        }
        SyncInitialStates();
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
            print("NO OBJECT FROM REFERENCE!!!");
            objectList.Add(null);
        }

        if (objectList[count] == null) return;
        objectToKey[objectList[count]] = count;
        count++;
    }

    [ContextMenu(itemName: "Set Exact Position")]
    private void SetExactPositionAll()
    {
        foreach(SyncObject obj in objectList)
        {
            SetExactPositionServerRPC(objectToKey[obj]);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetExactPositionServerRPC(ushort obj_key)
    {
        Transform t = objectList[obj_key].transform;
        SetExactPositionClientRPC(obj_key, t.position,t.rotation);
    }

    [ClientRpc]
    private void SetExactPositionClientRPC(ushort obj_key, Vector3 pos, Quaternion rot)
    {
        print(pos);
        objectList[obj_key].transform.SetPositionAndRotation(pos, rot);
    }

    [ContextMenu(itemName: "Sync Objects")]
    private void SyncInitialStates()
    {
        foreach (SyncObject obj in objectList)
        {
            if (obj == null) continue;
            Debug.Log("Syncing" + obj.name);
            obj.SyncObjectServerRPC(objectToKey[obj]);
        }
    }

}
