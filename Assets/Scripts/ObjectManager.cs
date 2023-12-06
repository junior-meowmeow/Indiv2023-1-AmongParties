using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class ObjectManager : NetworkBehaviour
{
    public List<NetworkObject> objectList;
    public Dictionary<NetworkObject, ushort> objectToKey = new();
    public ushort count = 0;
    public static ObjectManager instance;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if(!IsServer)
        {
            return;
        }
        objectList.AddRange(FindObjectsOfType<NetworkObject>());
        for (ushort i = count; i < objectList.Count; i++)
        {
            objectToKey[objectList[i]] = i;
            count++;
        }
    }

    void Update()
    {
        /*
        objectList.AddRange(FindObjectsOfType<NetworkObject>());
        for (ushort i = count; i < objectList.Count; i++)
        {
            objectToKey[objectList[i]] = i;
            //objectToKey.Remove(allObjects[i]);
            count++;
        }
        */
    }

    [ContextMenu(itemName: "Update Object List")]
    public void UpdateObjectList()
    {
        objectList.AddRange(FindObjectsOfType<NetworkObject>());
        for (ushort i = count; i < objectList.Count; i++)
        {
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
                refList[i] = objectList[i];
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
                objectList.Add(obj);
            }
            else
            {
                print("NO OBJECT FROM REFERENCE!!!");
            }
        }
        for (ushort i = count; i < objectList.Count; i++)
        {
            objectToKey[objectList[i]] = i;
            count++;
        }
    }

}
