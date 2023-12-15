using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DangerZone : MonoBehaviour
{

    private Vector3 startPosition;
    private bool isMoving;
    private float moveSpeed;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void FixedUpdate()
    {
        if(isMoving)
        {
            transform.Translate(moveSpeed * Time.deltaTime * Vector3.up);
        }
    }

    public void Reset()
    {
        transform.position = startPosition;
    }

    public void StartMove(float moveSpeed)
    {
        isMoving = true;
        this.moveSpeed = moveSpeed;
    }

    public void StopMove()
    {
        isMoving = false;
    }

    public void OnPlayerEnterServer(PlayerController player)
    {
        player.DieClientRPC();
    }

}
