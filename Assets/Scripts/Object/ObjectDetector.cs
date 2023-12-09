using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ObjectDetector : MonoBehaviour
{
    private Collider col;

    [SerializeField] private Color baseColor;

    void Awake()
    {
        col = GetComponent<Collider>();

        //Not working yet, Check it later :(
        GetComponent<MeshRenderer>().material.SetColor("_BaseColor", baseColor);
    }

    private void OnTriggerEnter(Collider col)
    {
        if (col.TryGetComponent(out PickableObject obj))
        {
            if(GameManager.instance.GetObject(obj, name))
            {
                ObjectPool.instance.SpawnObject("Score Particle", obj.transform.position, Quaternion.identity);
                SoundManager.Instance.Play("approve" + Random.Range(1, 4));
            }
            else
            {
                SoundManager.Instance.Play("deny");
            }
            if (obj.holdPlayer != null)
            {
                obj.holdPlayer.Drop();
                obj.Drop();
            }
            obj.gameObject.SetActive(false);
            foreach (PlayerData ps in GameManager.instance.GetPlayerList())
            {
                ps.player.interactionCollider.RemoveObject(obj);
            }
        }
    }
}
