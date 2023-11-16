using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickableObject : MonoBehaviour
{
    public Vector3 holdPos = new(0f, 0.006f, -0.14f);
    public Vector3 holdRotation = new(0f, 0f, 0f);
    public float movingSpeed = 8.0f;
    public float rotatingSpeed = 8.0f;
    public float weight = 1f;
    public bool hasRigidbody = true;
    public bool isDroppedAfterUse = true;

    private Rigidbody rb;
    private PlayerController holdPlayer;

    [SerializeField] private GameObject[] hands;

    private void Start()
    {
        if (hasRigidbody)
        {
            rb = GetComponent<Rigidbody>();
        }

        ShowHand(false);
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
        }
    }

    private void UpdatePositionRB()
    {
        Vector3 targetPosition = holdPlayer.holdPos.position;
        rb.velocity = (targetPosition - transform.position) * movingSpeed;
        //transform.rotation = holdPlayer.holdPos.rotation;
        Quaternion targetRotation = holdPlayer.holdPos.rotation;
        rb.MoveRotation(targetRotation); //Quaternion.Lerp(transform.rotation, targetRotation, rotatingSpeed * Time.deltaTime);
    }

    private void UpdatePositionNoRB()
    {
        transform.position = holdPlayer.holdPos.position;
        transform.rotation = holdPlayer.holdPos.rotation;
    }

    public void Use(bool isHolding)
    {
        rb.AddForce(holdPlayer.hipJoint.transform.forward * -10, ForceMode.Impulse);
    }

    public void Hold(PlayerController player)
    {
        holdPlayer = player;
        ShowHand(true);
    }

    public void Drop()
    {
        holdPlayer = null;
        ShowHand(false);
    }

    public bool IsPickable()
    {
        return holdPlayer == null;
    }

    void ShowHand(bool show)
    {
        if (hands.Length == 0) return;
        foreach (GameObject hand in hands)
        {
            hand.SetActive(show);
        }
    }
}
