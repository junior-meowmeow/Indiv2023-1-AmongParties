using System.Collections;
using System.Collections.Generic;
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
            rb.AddForce(holdPlayer.hipJoint.transform.forward * -throwForce, ForceMode.Impulse);
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
        rb.AddForce(holdPlayer.hipJoint.transform.forward * -chargeForce, ForceMode.Impulse);
    }
}
