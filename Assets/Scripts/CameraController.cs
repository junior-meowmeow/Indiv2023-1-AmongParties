using System;
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
    [SerializeField] private float maxVerticalAngle = 60f;

    [Header("Zoom")]
    [SerializeField] private Vector3 flatOffset = new(0f,0.35f,0f);
    [SerializeField] private Vector3 offset;
    [SerializeField] private float defaultDistance = 10f;
    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float maxDistance = 20f;
    [SerializeField] private float scrollSpeed = 10f;
    [SerializeField] private float zoomSpeed = 5f;

    [Header("Camera Blocking")]
    [SerializeField] private LayerMask layersToExclude;
    private LayerMask processedLayer;

    [Header("Debug")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float currentDistance = 40f;

    private PlayerController player;
    private float verticalRotation = 0;
    private float horizontalRotation = 0;
    private float mouseScroll = 0;

    void Awake()
    {
        player = GetComponentInParent<PlayerController>();
        //mainCamera = Camera.main;
        currentDistance = defaultDistance;
        offset = mainCamera.transform.localPosition;
        processedLayer = ~layersToExclude;
    }

    void Update()
    {
        if (player == null) return;

        /* Camera Position */

        Vector3 targetPosition = player.rb.transform.position + flatOffset;
        
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

        currentDistance -= mouseScroll * scrollSpeed;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        float newDistance = currentDistance;
        
        //LayerMask layerMask = ~(1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("PlayerWall") | 1 << LayerMask.NameToLayer("Item")); // ignore both layerX and layerY
        Vector3 displacement = mainCamera.transform.position - targetPosition;
        if (Physics.Raycast(targetPosition, displacement.normalized, out RaycastHit hit, currentDistance, processedLayer))
        {
            newDistance = hit.distance;
        }

        float camDistance = mainCamera.transform.localPosition.magnitude;
        if (camDistance != newDistance)
        {
            //mainCamera.fieldOfView = Mathf.MoveTowards(mainCamera.fieldOfView, currentFOV, zoomSpeed * Time.deltaTime);
            camDistance = Mathf.Lerp(camDistance, newDistance, zoomSpeed * Time.deltaTime);
            mainCamera.transform.localPosition = offset.normalized*camDistance;
        }

        /* Camera Rotation */

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        verticalRotation -= mouseY;
        horizontalRotation += mouseX;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxVerticalAngle, maxVerticalAngle); // Limit vertical rotation to prevent flipping

        transform.Rotate(Vector3.up * mouseX);
        transform.localRotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0);

        UpdatePlayerNameText();
    }

    public void SetScrollAxis(float axis)
    {
        mouseScroll = axis;
    }

    public void Disable()
    {
        gameObject.SetActive(false);
    }

    private void UpdatePlayerNameText()
    {
        if (!player.isDisplayUI) return;

        foreach (PlayerData player in GameManager.instance.GetPlayerList())
        {
            player.playerNameText.rectTransform.rotation = Quaternion.Euler (verticalRotation, horizontalRotation, 0);
        }
    }
}
