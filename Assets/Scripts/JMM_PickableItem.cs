using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JMM_PickableItem : MonoBehaviour
{

    public JMM_ItemPicker owner;
    public Vector3 offset;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (owner != null)
        {
            this.transform.position = owner.transform.position + offset;
        }
    }
}
