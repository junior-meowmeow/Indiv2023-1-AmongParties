using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Weapon : PickableObject
{
    [Header("Weapon Property")]
    public bool isAttacking = false;
    public float miminumDamage = 10f;

    [ServerRpc(RequireOwnership = false)]
    public override void SyncObjectServerRPC(ushort obj_key)
    {
        base.SyncObjectServerRPC(obj_key);
        SyncWeaponVariableClientRPC(obj_key, isAttacking);
    }

    [ClientRpc]
    private void SyncWeaponVariableClientRPC(ushort obj_key, bool isAttacking)
    {
        if (IsServer) return;
        Weapon obj = SyncObjectManager.instance.objectList[obj_key].GetComponent<Weapon>();
        obj.isAttacking = isAttacking;
    }
}
