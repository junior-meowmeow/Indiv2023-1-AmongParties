using Unity.Netcode;
using UnityEngine;

public class SyncObject : NetworkBehaviour
{
    [SerializeField] private NetworkObject net_obj;
    public bool isInScene = false;
    private byte playerCount;
    private bool isAdded;

    protected virtual void Awake()
    {
        net_obj = GetComponent<NetworkObject>();
        playerCount = 0;
        isAdded = false;
        //SyncObjectManager.instance.AddObjectListServerRPC(net_obj);
    }

    public NetworkObject GetNetworkObject()
    {
        return net_obj;
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
            SyncObjectManager.Instance.AddObjectListServerRPC(net_obj);
            isAdded = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public virtual void SyncObjectServerRPC(ushort obj_key)
    {
        SyncBaseClientRPC(obj_key, transform.position, transform.rotation);
    }

    [ClientRpc]
    protected virtual void SyncBaseClientRPC(ushort obj_key, Vector3 pos, Quaternion rot)
    {
        if (IsServer) return;
        // Synchronize Transform
        SyncObjectManager.Instance.GetSyncObject(obj_key).transform.SetPositionAndRotation(pos, rot);
        // Count Synchronized Object
        SyncObjectManager.Instance.CountSynchronize();
    }
}
