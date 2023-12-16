using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DangerZone : MonoBehaviour
{

    private Vector3 startPosition;
    private bool isMoving;
    private float moveSpeed;
    private Vector3 direction = Vector3.up;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void FixedUpdate()
    {
        if(isMoving)
        {
            transform.Translate(moveSpeed * Time.deltaTime * direction);
        }
    }

    public void Reset()
    {
        transform.position = startPosition;
    }

    public void StartMove(float moveSpeed, Vector3 direction)
    {
        isMoving = true;
        this.moveSpeed = moveSpeed;
        this.direction = direction.normalized;
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
