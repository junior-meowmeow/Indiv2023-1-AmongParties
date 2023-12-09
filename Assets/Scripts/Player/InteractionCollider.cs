using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionCollider : MonoBehaviour
{

    [SerializeField] private PlayerController player;
    [SerializeField] private List<PickableObject> objectList;
    [SerializeField] private PickableObject nearestObject;
    [SerializeField] private bool pickable;
    public bool HasObjectNearby
    {
        get { return pickable; }
    }

    public PickableObject GetNearestObject()
    {
        return nearestObject;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.TryGetComponent(out PickableObject obj))
        {
            objectList.Add(obj);
            UpdateObjectList();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent(out PickableObject obj))
        {
            objectList.Remove(obj);
            UpdateObjectList();
        }
    }

    private void UpdateObjectList()
    {
        objectList.Sort(CompareObjectByDistance);
        pickable = true;
        if (objectList.Count == 0)
        {
            pickable = false;
            player.UpdateObjectInfo(false, "");
        }
        if(pickable)
        {
            nearestObject = objectList[0];
            player.UpdateObjectInfo(true, objectList[0].objectName);
        }
        else
        {
            player.UpdateObjectInfo(false,"");
        }
    }

    private int CompareObjectByDistance(PickableObject a, PickableObject b)
    {
        float distanceA = Vector3.Distance(a.transform.position, transform.position);
        float distanceB = Vector3.Distance(b.transform.position, transform.position);
        return distanceA.CompareTo(distanceB);
    }
}
