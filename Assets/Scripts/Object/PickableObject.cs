using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

public class PickableObject : SyncObject
{
    [Header("Object Info")]
    public string objectName = "Pickable Object";
    [TextArea(6, 10)]
    public string description = "Press [E] to DROP\nPress [MOUSE1] to DROP";
    [Header("Base Property")]
    public ObjectType objectType;
    public ObjectColor objectColor;
    public string impactSound = "defaultImpact";
    public float minimumImpact = 4f;
    public float defaultMinimumImpact = 4f;
    public float holdingMinimumImpact = 7f;
    public float impactSoundCooldown = 0.2f;
    private float lastImpactTime;

    public Vector3 holdPos = new(0f, 0.006f, -0.014f);
    public Vector3 holdRotation = new(0f, 0f, 0f);
    public float movingSpeed = 8.0f;
    public float rotatingSpeed = 8.0f;
    public float weight = 1f;
    public bool hasRigidbody = true;
    public bool isDroppedAfterUse = true;
    public bool hasAltUse = false;
    public bool isDroppedAfterAltUse = true;
    public bool isStealable = false;
    public bool isHandShow = false;

    protected Rigidbody rb;
    public PlayerController holdPlayer = null;

    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private GameObject[] hands;

    protected virtual void Start()
    {
        if (hasRigidbody)
        {
            rb = GetComponent<Rigidbody>();
        }

        ShowHand(false);
        minimumImpact = defaultMinimumImpact;
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
        transform.SetPositionAndRotation(holdPlayer.holdPos.position, holdPlayer.holdPos.rotation);
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

    public virtual void SetUp(byte typeId, byte colorId, Color color)
    {
        objectType = (ObjectType)typeId;
        objectColor = (ObjectColor)colorId;

        if(objectType != ObjectType.Weapon)
        {
            objectName = objectColor.ToString() + " " + objectName;
        }

        if (meshRenderer == null) return;
        meshRenderer.material.color = color;
    }

    public void Hold(PlayerController player)
    {
        if (holdPlayer != null)
        {
            holdPlayer.DropObjectServerRPC(isSteal: true);
        }
        holdPlayer = player;
        ShowHand(true);
        minimumImpact = holdingMinimumImpact;
    }

    public void Drop()
    {
        holdPlayer = null;
        ShowHand(false);
        minimumImpact = defaultMinimumImpact;
    }

    public bool IsPickable()
    {
        return holdPlayer == null || isStealable;
    }

    public void ShowHand(bool show)
    {
        if (hands.Length == 0) return;
        isHandShow = show;
        foreach (GameObject hand in hands)
        {
            hand.SetActive(show);
            if (holdPlayer != null) hand.GetComponentInChildren<SkinnedMeshRenderer>().material.color = holdPlayer.GetPlayerData().GetColor();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public override void SyncObjectServerRPC(ushort obj_key)
    {
        base.SyncObjectServerRPC(obj_key);
        byte[] data = { (byte)objectType, (byte)objectColor };
        bool[] states = { isDroppedAfterUse, isDroppedAfterAltUse, isStealable, isHandShow };
        if(holdPlayer == null)
        {
            SyncObjectVariableClientRPC(obj_key, ushort.MaxValue, data, states);
        }
        else
        {
            SyncObjectVariableClientRPC(obj_key, SyncObjectManager.instance.objectToKey[holdPlayer], data, states);
        }
    }

    [ClientRpc]
    private void SyncObjectVariableClientRPC(ushort obj_key, ushort holdPlayer_key, byte[] data, bool[] states)
    {
        if (IsServer) return;
        PickableObject obj = SyncObjectManager.instance.objectList[obj_key].GetComponent<PickableObject>();
        obj.objectType = (ObjectType)data[0];
        obj.objectColor = (ObjectColor)data[1];
        obj.isDroppedAfterUse = states[0];
        obj.isDroppedAfterAltUse = states[1];
        obj.isStealable = states[2];
        if(holdPlayer_key != ushort.MaxValue)
        {
            PlayerController player = SyncObjectManager.instance.objectList[holdPlayer_key].GetComponent<PlayerController>();
            obj.holdPlayer = player;
            player.holdingObject = obj;
        }
        obj.ShowHand(states[3]);
        if (meshRenderer != null)
        {
            meshRenderer.material.color = ObjectSpawner.instance.GetColorFromId(data[1]);
        }
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude > minimumImpact)
        {
            PlayImpactSound(collision.relativeVelocity.magnitude, collision.transform.position);
        }
    }

    protected virtual void PlayImpactSound(float magnitude, Vector3 position)
    {
        if (Time.time - lastImpactTime < impactSoundCooldown) return;
        float scale = magnitude / 30f;
        SoundManager.PlayNew(impactSound, scale, position);
        lastImpactTime = Time.time;
    }

}
