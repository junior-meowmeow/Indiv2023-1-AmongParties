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

    protected override void Start()
    {
        base.Start();
        hasAltUse = true;
        if(isChargeable)
        {
            isDroppedAfterAltUse = false;
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
            else
            {
                isDroppedAfterAltUse = true;
                ChargeThrow();
            }
            return;
        }
        rb.AddForce(holdPlayer.hipJoint.transform.forward * -throwForce, ForceMode.Impulse);
    }

    private void StartCharge()
    {
        startThrowTime = Time.time;
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
