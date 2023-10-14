using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float movementSpeed = 5.0f;
    public float jumpForce = 1.0f;
    public float mouseSensitivity = 2.0f;
    public float interactRange = 3.0f;
    public float rotationSpeed = 1.0f;
    public float stepMultiplier = 0.01f;
    public float stepSpeed = 0.5f;
    public float currentStepTime;

    public bool holdRotation = false;

    private Camera cam;
    private float verticalRotation = 0;

    private CharacterController characterController;

    [Header ("Input Action")]
    public InputActionReference moveInput;
    public InputActionReference interactInput;

    [Header ("Body")]
    public Rigidbody rb;
    public ConfigurableJoint hipJoint;
    public Transform leftLeg;
    public Transform rightLeg;

    public PickableObject holdingObject;
    public Transform holdPos;
    public Transform interactPoint;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>();
        currentStepTime = 0f;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void FixedUpdate()
    {
        Movement();
        // Interact();
    }

    void OnEnable()
    {
        interactInput.action.started += Interact;
    }

    void Movement()
    {
        Vector2 moveDir = moveInput.action.ReadValue<Vector2>();

        //print(moveDir);
        //print(hipJoint.transform.localEulerAngles);
        if (moveDir == Vector2.zero)
        {

            if (!holdRotation)
            {
                float currentAngleY = hipJoint.transform.localEulerAngles.y;
                //print(currentAngleY);
                if (currentAngleY < 0)
                {
                    currentAngleY *= -1;
                }
                else
                {
                    currentAngleY = 360f - currentAngleY;
                }
                //print(currentAngleY);
                Vector3 currentRotation = new Vector3(0f, currentAngleY, 0f);
                //print(currentRotation);
                hipJoint.targetRotation = Quaternion.Euler(currentRotation);
                holdRotation = true;
            }
            return;
        }

        holdRotation = false;

        Vector3 leftLegAngle = leftLeg.localEulerAngles;
        Vector3 rightLegAngle = rightLeg.localEulerAngles;
        print("old : " + leftLegAngle);
        leftLegAngle.x = 350f + 30f * Mathf.Sin(currentStepTime * stepMultiplier);
        rightLegAngle.x = 350f - 30f * Mathf.Sin(currentStepTime * stepMultiplier);
        print("new : " + leftLegAngle);
        leftLeg.localRotation = Quaternion.Euler(leftLegAngle);
        rightLeg.localRotation = Quaternion.Euler(rightLegAngle);
        currentStepTime += stepSpeed * Time.fixedDeltaTime;

        float forwardMovement = moveDir.y * movementSpeed;
        float strafeMovement = moveDir.x * movementSpeed;

        rb.velocity = transform.forward * forwardMovement + transform.right * strafeMovement;

        float targetAngle = Mathf.Atan2(-moveDir.x, moveDir.y) * Mathf.Rad2Deg;

        if(targetAngle < 0)
        {
            targetAngle += 360f;
        }

        float currentAngle = hipJoint.targetRotation.eulerAngles.y;

        bool passZeroDegree = Mathf.Abs(targetAngle - currentAngle) > 180f;

        float distance;
        if (passZeroDegree)
        {
            if(currentAngle > 180)
            {
                distance = targetAngle - currentAngle + 360f;
            }
            else
            {
                distance = targetAngle - currentAngle - 360f;
            }
        }
        else
        {
            distance = targetAngle - currentAngle;
        }

        //print("before :"+currentAngle + " -> " + targetAngle);
        if(Mathf.Abs(distance) > rotationSpeed * Time.fixedDeltaTime)
        {
            targetAngle = currentAngle + rotationSpeed * Mathf.Sign(distance) * Time.fixedDeltaTime;
        }
        //print("after :"+targetAngle);

        hipJoint.targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
    }

    void Interact(InputAction.CallbackContext c)
    {
        if (holdingObject == null)
        {
            RaycastHit hit;
            if (Physics.Raycast(interactPoint.transform.position, interactPoint.transform.forward, out hit, interactRange))
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

    void OldMovement()
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

    void OldInteract()
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

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(interactPoint.transform.position, transform.forward * interactRange);
    }
}
