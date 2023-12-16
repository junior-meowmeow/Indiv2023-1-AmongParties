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
        if (isOnce && isUsed) return;

        if (collision.gameObject.TryGetComponent(out Weapon w))
        {
            if (!w.isAttacking) return;

            //anim.Play(interactClip.name);
            InteractionClientRPC();
        }

    }

    private void OnEnable()
    {
        GameplayManager.resetAllObjects += ResetAnimation;
    }

    private void OnDisable()
    {
        GameplayManager.resetAllObjects -= ResetAnimation;
    }

    [ClientRpc]
    private void InteractionClientRPC()
    {
        Interaction();
    }

    private void Interaction()
    {
        anim.clip = interactClip;
        anim.Play();
        SoundManager.PlayNew("door_open",transform.position);
        isUsed = true;
    }

    [ContextMenu(itemName: "Reset")]
    private void ResetAnimation()
    {
        isUsed = false;
        anim.Rewind();
        anim.Play();
        anim.Sample();
        anim.Stop();
    }

    [ServerRpc(RequireOwnership = false)]
    public override void SyncObjectServerRPC(ushort obj_key)
    {
        base.SyncObjectServerRPC(obj_key);
        SyncInteractionClientRPC(obj_key, enabled, isUsed);
    }

    [ClientRpc]
    private void SyncInteractionClientRPC(ushort obj_key, bool enabled, bool isUsed)
    {
        if (IsServer) return;
        InteractableObject obj = SyncObjectManager.Instance.GetSyncObject(obj_key).GetComponent<InteractableObject>();
        obj.isUsed = isUsed;
        if(obj.isOnce && isUsed)
        {
            Interaction();
        }
        obj.enabled = enabled;
    }
}
