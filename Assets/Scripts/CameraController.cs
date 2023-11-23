using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5.0f;
    // [SerializeField] private float verticalThreshold = 0.1f;

    [Header("Rotation")]
    [SerializeField] private float mouseSensitivity = 2.0f;

    [Header("Zoom")]
    [SerializeField] private float defaultFOV = 40f;
    [SerializeField] private float minFOV = 20f;
    [SerializeField] private float maxFOV = 60f;
    [SerializeField] private float scrollSpeed = 10f;
    [SerializeField] private float zoomSpeed = 5f;
    //[SerializeField] private Vector3 offset;

    [SerializeField] private Camera mainCamera;
    [SerializeField] private float currentFOV = 40f;

    private PlayerController player;
    private float verticalRotation = 0;
    private float horizontalRotation = 0;
    private float mouseScroll = 0;

    void Awake()
    {
        player = GetComponentInParent<PlayerController>();
        //mainCamera = Camera.main;
        currentFOV = defaultFOV;
        mainCamera.fieldOfView = currentFOV;
    }

    void Update()
    {
        if (player == null) return;

        /* Camera Position */

        Vector3 targetPosition = player.rb.transform.position;
        
        /*
        if(Mathf.Abs(targetPosition.y - transform.position.y) < verticalThreshold)
        {
            targetPosition.y = transform.position.y;
        }
        */

        //transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        /* Camera Zoom */

        // float mouseScroll = Input.GetAxis("Mouse ScrollWheel");
        // if(mouseScroll != 0)
        // {
        //     print(mouseScroll);
        // }

        currentFOV -= mouseScroll * scrollSpeed;
        currentFOV = Mathf.Clamp(currentFOV, minFOV, maxFOV);
        if (mainCamera.fieldOfView != currentFOV)
        {
            //mainCamera.fieldOfView = Mathf.MoveTowards(mainCamera.fieldOfView, currentFOV, zoomSpeed * Time.deltaTime);
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, currentFOV, zoomSpeed * Time.deltaTime);
        }

        /* Camera Rotation */

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        verticalRotation -= mouseY;
        horizontalRotation += mouseX;
        verticalRotation = Mathf.Clamp(verticalRotation, -30, 30); // Limit vertical rotation to prevent flipping

        transform.Rotate(Vector3.up * mouseX);
        transform.localRotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0);
    }

    public void SetScrollAxis(float axis)
    {
        mouseScroll = axis;
    }

    public void Disable()
    {
        gameObject.SetActive(false);
    }
}
