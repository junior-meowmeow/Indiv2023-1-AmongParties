using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class SyncObject : NetworkBehaviour
{
    public NetworkObject net_obj;
    private ushort playerCount;
    private bool isAdded;
    public bool isInScene = false;

    protected virtual void Awake()
    {
        net_obj = GetComponent<NetworkObject>();
        playerCount = 0;
        isAdded = false;
        //SyncObjectManager.instance.AddObjectListServerRPC(net_obj);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        //Debug.Log("OnNetworkSpawn Called");
        CheckSpawnServerRPC();
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheckSpawnServerRPC()
    {
        if(!isInScene)
        {
            playerCount++;
        }
        if(!isAdded && playerCount == NetworkManager.ConnectedClientsIds.Count)
        {
            SyncObjectManager.instance.AddObjectListServerRPC(net_obj);
            isAdded = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public virtual void SyncObjectServerRPC(ushort obj_key)
    {
        SyncTransformClientRPC(obj_key, transform.position, transform.rotation);
    }

    [ClientRpc]
    protected virtual void SyncTransformClientRPC(ushort obj_key, Vector3 pos, Quaternion rot)
    {
        if (IsServer) return;
        SyncObjectManager.instance.objectList[obj_key].transform.SetPositionAndRotation(pos, rot);
    }
}
