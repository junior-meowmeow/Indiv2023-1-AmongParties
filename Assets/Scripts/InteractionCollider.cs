using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionCollider : MonoBehaviour
{

    [SerializeField] private List<PickableObject> objectList;
    [SerializeField] private PickableObject nearestObject;
    [SerializeField] private bool pickable;
    public bool HasObjectNearby
    {
        get { return pickable; }
    }

    public PickableObject GetNearestObject()
    {
        UpdateObjectList();
        return nearestObject;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.TryGetComponent(out PickableObject obj))
        {
            objectList.Add(obj);
            pickable = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent(out PickableObject obj))
        {
            objectList.Remove(obj);
            if(objectList.Count == 0)
            {
                pickable = false;
            }
        }
    }

    private void UpdateObjectList()
    {
        objectList.Sort(CompareObjectByDistance);
        nearestObject = objectList[0];
    }

    private int CompareObjectByDistance(PickableObject a, PickableObject b)
    {
        float distanceA = Vector3.Distance(a.transform.position, transform.position);
        float distanceB = Vector3.Distance(b.transform.position, transform.position);
        return distanceA.CompareTo(distanceB);
    }
}
