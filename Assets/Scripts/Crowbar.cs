using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crowbar : PickableObject
{
    [Header("Crowbar Property")]
    [SerializeField] private float throwForce = 10f;
    [SerializeField] private bool isAttacking = false;
    [SerializeField] private float attackDuration = 0f;
    [SerializeField] private float miminumDamage = 10f;

    protected override void Start()
    {
        base.Start();
        isDroppedAfterUse = false;
        hasAltUse = true;
        isDroppedAfterAltUse = true;
    }

    public override void Use(bool isHolding)
    {
        if(isHolding)
        {
            Attack();
            Debug.Log("ATTACK!");
        }
    }

    public override void AltUse(bool isHolding)
    {
        if(isHolding)
        {
            rb.AddForce(holdPlayer.hipJoint.transform.forward * -throwForce, ForceMode.Impulse);
        }
    }

    private void Attack()
    {

    }
}
