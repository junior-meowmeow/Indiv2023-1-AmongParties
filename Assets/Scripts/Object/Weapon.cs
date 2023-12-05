using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : PickableObject
{
    [Header("Weapon Property")]
    public bool isAttacking = false;
    public float miminumDamage = 10f;
}
