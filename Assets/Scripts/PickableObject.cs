using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickableObject : MonoBehaviour
{
    private PlayerController holdPlayer;

    void Update()
    {
        if (holdPlayer)
        {
            transform.position = holdPlayer.holdPos.position;
            transform.rotation = holdPlayer.holdPos.rotation;
        }
    }

    public void Hold(PlayerController player)
    {
        holdPlayer = player;
    }

    public void Drop()
    {
        holdPlayer = null;
    }
}
