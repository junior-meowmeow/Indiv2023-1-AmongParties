using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Detector : MonoBehaviour
{
    private Collider col;

    void Start()
    {
        col = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider col)
    {
        if (col.TryGetComponent<PickableObject>(out PickableObject obj))
        {
            Debug.Log(obj.gameObject);
        }
    }
}
