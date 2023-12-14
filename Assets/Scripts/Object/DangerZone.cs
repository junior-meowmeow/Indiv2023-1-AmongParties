using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DangerZone : MonoBehaviour
{

    private void OnTriggerEnter(Collider col)
    {
        if (col.TryGetComponent(out PlayerController player))
        {
        }
    }

}
