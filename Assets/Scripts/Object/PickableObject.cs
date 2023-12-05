using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PickableObject : MonoBehaviour
{
    [Header("Base Property")]
    public ObjectType objectType;
    public ObjectColor objectColor;

    public Vector3 holdPos = new(0f, 0.006f, -0.014f);
    public Vector3 holdRotation = new(0f, 0f, 0f);
    public float movingSpeed = 8.0f;
    public float rotatingSpeed = 8.0f;
    public float weight = 1f;
    public bool hasRigidbody = true;
    public bool isDroppedAfterUse = true;
    public bool hasAltUse = false;
    public bool isDroppedAfterAltUse = true;

    protected Rigidbody rb;
    public PlayerController holdPlayer;

    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private GameObject[] hands;

    protected virtual void Start()
    {
        if (hasRigidbody)
        {
            rb = GetComponent<Rigidbody>();
        }

        ShowHand(false);
    }

    protected virtual void FixedUpdate()
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

    public virtual void Use(bool isHolding)
    {
        isDroppedAfterUse = isHolding;
        return;
    }

    public virtual void AltUse(bool isHolding)
    {
        return;
    }

    public void SetUp(int typeId, int colorId, Color color)
    {
        objectType = (ObjectType)typeId;
        objectColor = (ObjectColor)colorId;

        if (meshRenderer == null) return;
        meshRenderer.material.color = color;
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
            if (holdPlayer != null) hand.GetComponentInChildren<SkinnedMeshRenderer>().material.color = holdPlayer.GetPlayerSetting().GetColor();
        }
    }
}
