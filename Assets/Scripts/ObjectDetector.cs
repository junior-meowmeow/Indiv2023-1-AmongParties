using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ObjectDetector : MonoBehaviour
{
    private Collider col;

    void Awake()
    {
        col = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider col)
    {
        if (col.TryGetComponent<PickableObject>(out PickableObject obj))
        {
            GameManager.instance.GetScore(1);
        }
    }
}
