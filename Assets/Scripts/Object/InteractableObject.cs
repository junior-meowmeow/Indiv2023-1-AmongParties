using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class InteractableObject : NetworkBehaviour
{
    [SerializeField] private bool isOnce = true;
    [SerializeField] private Animation anim;
    [SerializeField] private AnimationClip interactClip;

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.TryGetComponent(out Weapon w))
        {
            if (!w.isAttacking) return;

            anim.Play(interactClip.name);
            if (isOnce) enabled = false;
        }

    }
}
