using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class InteractableObject : SyncObject
{
    [SerializeField] private bool isOnce = true;
    [SerializeField] private bool isUsed = false;
    [SerializeField] private Animation anim;
    [SerializeField] private AnimationClip interactClip;

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.TryGetComponent(out Weapon w))
        {
            if (!w.isAttacking) return;

            //anim.Play(interactClip.name);
            InteractionClientRPC();
            if (isOnce) enabled = false;
        }

    }

    [ClientRpc]
    void InteractionClientRPC()
    {
        Interaction();
    }

    void Interaction()
    {
        anim.Play(interactClip.name);
        isUsed = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public override void SyncObjectServerRPC(ushort obj_key)
    {
        base.SyncObjectServerRPC(obj_key);
        SyncInteractionClientRPC(obj_key, enabled, isOnce);
    }

    [ClientRpc]
    private void SyncInteractionClientRPC(ushort obj_key, bool enabled, bool isUsed)
    {
        if (IsServer) return;
        InteractableObject obj = SyncObjectManager.instance.objectList[obj_key].GetComponent<InteractableObject>();
        obj.enabled = enabled;
        obj.isUsed = isUsed;
        if(obj.isOnce && isUsed)
        {
            Interaction();
        }
    }
}
