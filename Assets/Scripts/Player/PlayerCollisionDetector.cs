using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerCollisionDetector : NetworkBehaviour
{

    [SerializeField] private PlayerController player;

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.TryGetComponent(out Weapon w))
        {
            // Debug.Log(collision.gameObject.name);
            if (!w.isAttacking) return;
            if (w.holdPlayer == player) return;
            // Debug.Log("Hit Player");
            player.FallClientRPC(3);
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.gameObject.TryGetComponent(out DangerZone dz))
        {
            dz.OnPlayerEnterServer(player);
        }
    }

}
