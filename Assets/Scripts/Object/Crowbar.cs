using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
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
            rb.AddForce((holdPlayer.hipJoint.transform.forward + Vector3.down * 0.25f) * -throwForce, ForceMode.Impulse);
            isThrowing = true;
            isAttacking = true;
            SoundManager.Instance.PlayNew("throw", transform.position);
        }
    }

    private void Attack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        holdPlayer.holdPos.gameObject.GetComponent<HoldPosController>().PlayAnimation(attackAnimation);
        SoundManager.Instance.PlayNew("attack", transform.position);
    }

    public override void SetUp(byte typeId, byte colorId, Color color)
    {
        objectType = ObjectType.Weapon;
        if (typeId == 3)
        {
            objectColor = ObjectColor.RED;
            objectName = "Crowbar";
        }
        else if(typeId == 4)
        {
            objectColor = ObjectColor.BLUE;
            objectName = "Hammer";
        }
    }

    protected override void PlayImpactSound(float magnitude, Vector3 position)
    {
        base.PlayImpactSound(magnitude, position);
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
        if (!isThrowing) return;
        if (collision.gameObject.layer != 0) return;
        isThrowing = false;
        isAttacking = false;
        PlayImpactSound(collision.relativeVelocity.magnitude, collision.transform.position);
    }

    [ServerRpc(RequireOwnership = false)]
    public override void SyncObjectServerRPC(ushort obj_key)
    {
        base.SyncObjectServerRPC(obj_key);
        SyncCrowbarClientRPC(obj_key, lastAttackTime, isThrowing);
    }

    [ClientRpc]
    private void SyncCrowbarClientRPC(ushort obj_key, float lastAttackTime, bool isThrowing)
    {
        if (IsServer) return;
        Crowbar obj = SyncObjectManager.instance.objectList[obj_key].GetComponent<Crowbar>();
        obj.lastAttackTime = lastAttackTime;
        obj.isThrowing = isThrowing;
    }
}
