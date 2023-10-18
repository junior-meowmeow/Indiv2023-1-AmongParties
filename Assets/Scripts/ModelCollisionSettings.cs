using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelCollisionSettings : MonoBehaviour
{

    public Collider[] bodyColliders;

    void Start()
    {
        bodyColliders = GetComponentsInChildren<Collider>();
        for(int i = 0; i < bodyColliders.Length; i++)
        {
            for(int j = i+1; j < bodyColliders.Length; j++)
            {
                Physics.IgnoreCollision(bodyColliders[i], bodyColliders[j]);
            }
        }
    }

}
