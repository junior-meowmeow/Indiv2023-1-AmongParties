using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SyncObject : NetworkBehaviour
{
    public NetworkObject net_obj;
    protected virtual void Awake()
    {
        net_obj = GetComponent<NetworkObject>();
        //SyncObjectManager.instance.AddObjectListServerRPC(net_obj);
    }

    [ServerRpc(RequireOwnership = false)]
    public virtual void SyncObjectServerRPC(ushort obj_key)
    {
        SyncTransformClientRPC(obj_key, transform.position, transform.rotation);
    }

    [ClientRpc]
    protected virtual void SyncTransformClientRPC(ushort obj_key, Vector3 pos, Quaternion rot)
    {
        SyncObjectManager.instance.objectList[obj_key].transform.SetPositionAndRotation(pos, rot);
    }
}
