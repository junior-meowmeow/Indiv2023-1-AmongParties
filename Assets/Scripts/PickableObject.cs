using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickableObject : MonoBehaviour
{
    public float movementSpeed = 8.0f;
    public bool hasRigidbody = false;

    private Rigidbody rb;
    private PlayerController holdPlayer;

    private void Start()
    {
        if (hasRigidbody)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    void FixedUpdate()
    {
        if (holdPlayer)
        {
            if (hasRigidbody)
            {
                UpdatePositionRB();
            }
            else
            {
                UpdatePositionNoRB();
            }
            transform.rotation = holdPlayer.holdPos.rotation;
        }
    }

    private void UpdatePositionRB()
    {
        Vector3 targetPosition = holdPlayer.holdPos.position;
        rb.velocity = (targetPosition - transform.position) * movementSpeed;
    }

    private void UpdatePositionNoRB()
    {
        transform.position = holdPlayer.holdPos.position;
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
