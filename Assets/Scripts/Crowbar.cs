using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crowbar : PickableObject
{
    [Header("Crowbar Property")]
    [SerializeField] private float throwForce = 10f;

    public override void Use(bool isHolding)
    {
        Debug.Log("ATTACK!");
    }

    public override void AltUse(bool isHolding)
    {
        rb.AddForce(holdPlayer.hipJoint.transform.forward * -throwForce, ForceMode.Impulse);
    }

    private void Charge()
    {

    }

    private void Attack()
    {

    }
}
