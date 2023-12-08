using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crowbar : Weapon
{
    [Header("Crowbar Property")]
    [SerializeField] private bool isThrowing = false;
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
        if(Time.time - lastAttackTime > attackDuration && !isThrowing)
        {
            isAttacking = false;
        }
    }

    public override void Use(bool isHolding)
    {
        if(isHolding && !isAttacking)
        {
            Attack();
        }
    }

    public override void AltUse(bool isHolding)
    {
        if(isHolding)
        {
            rb.AddForce(holdPlayer.hipJoint.transform.forward * -throwForce, ForceMode.Impulse);
            isThrowing = true;
            isAttacking = true;
        }
    }

    private void Attack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        holdPlayer.holdPos.gameObject.GetComponent<HoldPosController>().PlayAnimation(attackAnimation);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isThrowing) return;
        if (collision.gameObject.layer != 0) return;
        isThrowing = false;
        isAttacking = false;
    }
}
