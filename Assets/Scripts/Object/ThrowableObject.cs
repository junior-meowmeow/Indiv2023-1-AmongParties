using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ThrowableObject : PickableObject
{
    [Header("Throwable Property")]
    [SerializeField] private float throwForce = 10f;
    [SerializeField] private bool isChargeable = false;
    [SerializeField] private float startThrowForce = 3f;
    [SerializeField] private float maxThrowForce = 15f;
    [SerializeField] private float throwChargeSpeed = 10f;
    [SerializeField] private float startThrowTime;
    [SerializeField] private bool isCharging = false;

    protected override void Start()
    {
        base.Start();
        hasAltUse = true;
        if(isChargeable)
        {
            isDroppedAfterAltUse = false;
            isCharging = false;
        }
    }

    public override void AltUse(bool isHolding)
    {
        if(isChargeable)
        {
            if(isHolding)
            {
                isDroppedAfterAltUse = false;
                StartCharge();
            }
            else if(isCharging)
            {
                isDroppedAfterAltUse = true;
                ChargeThrow();
            }
            return;
        }
        if(isHolding)
        {
            Throw(throwForce);
        }
    }

    private void StartCharge()
    {
        startThrowTime = Time.time;
        isCharging = true;
    }

    private void ChargeThrow()
    {
        float chargeForce = startThrowForce + throwChargeSpeed * (Time.time - startThrowTime);
        if(chargeForce > maxThrowForce)
        {
            chargeForce = maxThrowForce;
        }
        Throw(chargeForce);
        isCharging = false;
    }

    private void Throw(float currentThrowForce)
    {
        rb.AddForce((holdPlayer.hipJoint.transform.forward + Vector3.down * 0.25f) * -currentThrowForce, ForceMode.Impulse);
        SoundManager.Instance.PlayNew("throw", transform.position);
    }

    [ServerRpc(RequireOwnership = false)]
    public override void SyncObjectServerRPC(ushort obj_key)
    {
        base.SyncObjectServerRPC(obj_key);
        SyncThrowableObjectVariableClientRPC(obj_key, startThrowTime,isCharging);
    }

    [ClientRpc]
    private void SyncThrowableObjectVariableClientRPC(ushort obj_key, float startThrowTime, bool isCharging)
    {
        if (IsServer) return;
        ThrowableObject obj = SyncObjectManager.instance.objectList[obj_key].GetComponent<ThrowableObject>();
        obj.startThrowTime = startThrowTime;
        obj.isCharging = isCharging;
    }
}
