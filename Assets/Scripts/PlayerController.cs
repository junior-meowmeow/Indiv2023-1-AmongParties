using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float movementSpeed = 5.0f;
    public float jumpForce = 1.0f;
    public float mouseSensitivity = 2.0f;
    public float interactRange = 3.0f;


    private Camera cam;
    private float verticalRotation = 0;

    private CharacterController characterController;

    public PickableObject holdingObject;
    public Transform holdPos;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Movement();
        Interact();
    }

    void Movement()
    {
        float forwardMovement = Input.GetAxis("Vertical") * movementSpeed;
        float strafeMovement = Input.GetAxis("Horizontal") * movementSpeed;

        Vector3 movement = transform.forward * forwardMovement + transform.right * strafeMovement;
        characterController.Move(movement * Time.deltaTime);

        // Player rotation (looking around)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90, 90); // Limit vertical rotation to prevent flipping

        transform.Rotate(Vector3.up * mouseX);
        cam.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);

        if (characterController.isGrounded && Input.GetButtonDown("Jump"))
        {
            characterController.Move(Vector3.up * jumpForce);
        }

    }

    void Interact()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (holdingObject == null)
            {
                RaycastHit hit;
                if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, interactRange))
                {
                    PickableObject obj = hit.collider.GetComponent<PickableObject>();
                    if (obj != null)
                    {
                        obj.Hold(this);
                        holdingObject = obj;
                    }
                }
            }
            else
            {
                holdingObject.Drop();
                holdingObject = null;
            }
        }

    }
}
