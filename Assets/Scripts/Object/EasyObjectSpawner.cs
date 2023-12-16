using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum ObjectInfo { Crate, Core, Crowbar, Hammer}

[System.Serializable]
public class ObjectToSpawn
{
    public ObjectInfo objectToSpawn;
    public Color color;
    public Transform transform;
    public GameObject obj_ref;
}

public class EasyObjectSpawner : MonoBehaviour
{

    private static EasyObjectSpawner instance;
    public static EasyObjectSpawner Instance => instance;

    public List<ObjectToSpawn> objectToSpawnList;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    [ClientRpc]
    public void SpawnObjectsClientRpc()
    {
        foreach(ObjectToSpawn obj_info in objectToSpawnList)
        {
            GameObject obj = ObjectPool.Instance.SpawnObject(obj_info.objectToSpawn.ToString(), obj_info.transform.position, obj_info.transform.rotation);
            if (obj != null)
            {
                obj.GetComponent<PickableObject>().SetUp((byte)obj_info.objectToSpawn, 0, obj_info.color);
                obj_info.obj_ref = obj;
            }
        }
    }

}
