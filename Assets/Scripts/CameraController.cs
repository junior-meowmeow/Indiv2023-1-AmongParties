using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 2.0f;
    [SerializeField] private Vector3 offset;
    
    private PlayerController player;
    private float verticalRotation = 0;
    private float horizontalRotation = 0;

    void Awake()
    {
        player = GetComponentInParent<PlayerController>();
        offset = transform.position - transform.position;
    }

    void Update()
    {
        if (player == null) return;

        transform.position = Vector3.MoveTowards(transform.position, player.rb.transform.position - offset, 10 * Time.deltaTime);

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        verticalRotation -= mouseY;
        horizontalRotation += mouseX;
        verticalRotation = Mathf.Clamp(verticalRotation, -30, 30); // Limit vertical rotation to prevent flipping

        transform.Rotate(Vector3.up * mouseX);
        transform.localRotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0);
    }
}
