using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crowbar : Weapon
{
    [Header("Crowbar Property")]
    [SerializeField] private float throwForce = 10f;
    [SerializeField] private float attackDuration = 0f;
    [SerializeField] private AnimationClip attackAnimation;
    private float lastAttackTime;

    protected override void Start()
    {
        base.Start();
        isDroppedAfterUse = false;
        hasAltUse = true;
        isDroppedAfterAltUse = true;
        attackDuration = attackAnimation.length;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if(Time.time - lastAttackTime > attackDuration)
        {
            isAttacking = false;
        }
    }

    public override void Use(bool isHolding)
    {
        if(isHolding && !isAttacking)
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
        isAttacking = true;
        lastAttackTime = Time.time;
        holdPlayer.holdPos.gameObject.GetComponent<HoldPosController>().PlayAnimation(attackAnimation);
    }

}
