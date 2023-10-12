using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject player;
    private Vector3 offset;

    void Awake()
    {
        offset = player.transform.position - transform.position;
    }

    void Update()
    {
        if (player == null) return;

        transform.position = Vector3.MoveTowards(transform.position, player.transform.position - offset, 10 * Time.deltaTime);
    }
}
