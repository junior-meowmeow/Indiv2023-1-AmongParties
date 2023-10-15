using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickableObject : MonoBehaviour
{
    public float movingSpeed = 8.0f;
    public float weight = 1f;
    public bool hasRigidbody = true;

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
        rb.velocity = (targetPosition - transform.position) * movingSpeed;
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
