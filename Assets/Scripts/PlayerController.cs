using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header ("Movement")]
    [SerializeField] private float movementSpeed = 5.0f;
    [SerializeField] private float jumpForce = 1.0f;
    [SerializeField] private float interactRange = 3.0f;
    [SerializeField] private float rotationSpeed = 1.0f;
    [SerializeField] private float stepSpeed = 20f;
    private float stepMultiplier = 0.5f;
    private float currentStepTime;

    private bool isFall = false;
    [SerializeField] private float defaultFallDuration = 3f;
    private float lastfallDuration;
    private float lastFallTime;

    private CharacterController characterController;

    [Header ("Input Action")]
    public InputActionReference moveInput;
    public InputActionReference jumpInput;
    public InputActionReference interactInput;

    [Header ("Body")]
    public Rigidbody rb;
    public ConfigurableJoint hipJoint;
    public Transform leftLeg;
    public Transform rightLeg;

    private CameraController cam;
    public PickableObject holdingObject;
    public Transform holdPos;
    public Transform interactPoint;
    public Transform groundPoint;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        cam = GetComponentInChildren<CameraController>();

        Cursor.lockState = CursorLockMode.Locked;

        currentStepTime = 0f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            Fall(defaultFallDuration);
        }
    }

    void FixedUpdate()
    {
        CheckFall();
        if (isFall) return;
        Movement();
    }

    void OnEnable()
    {
        interactInput.action.started += Interact;
        jumpInput.action.started += Jump;
    }

    void Movement()
    {
        Vector2 moveDir = moveInput.action.ReadValue<Vector2>();

        if (moveDir == Vector2.zero) return;
        
        LegPos();

        float forwardMovement = moveDir.y * movementSpeed;
        float strafeMovement = moveDir.x * movementSpeed;
        float camRotY = cam.transform.localEulerAngles.y;
 
        rb.velocity = Quaternion.Euler(0f, camRotY, 0f) * transform.forward * forwardMovement
                    + Quaternion.Euler(0f, camRotY, 0f) * transform.right * strafeMovement;

        float targetAngle = Mathf.Atan2(-moveDir.x, moveDir.y) * Mathf.Rad2Deg - camRotY;
        targetAngle = (targetAngle + 720f) % 360f;


        float currentAngle = hipJoint.targetRotation.eulerAngles.y;
        float distance = targetAngle - currentAngle;
        //Pass Zero Degree
        if (Mathf.Abs(targetAngle - currentAngle) > 180f)
        {
            distance = targetAngle - currentAngle + (currentAngle > 180? 360f : -360f);
        }

        if(Mathf.Abs(distance) > rotationSpeed * Time.fixedDeltaTime)
        {
            targetAngle = currentAngle + rotationSpeed * Mathf.Sign(distance) * Time.fixedDeltaTime;
        }
        hipJoint.targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
    }

    void CheckFall()
    {
        if (Time.time - lastFallTime > lastfallDuration)
        {
            isFall = false;
            JointDrive jointXDrive = hipJoint.angularXDrive;
            jointXDrive.positionSpring = 1500f;
            hipJoint.angularXDrive = jointXDrive;

            JointDrive jointYZDrive = hipJoint.angularYZDrive;
            jointYZDrive.positionSpring = 1500f;
            hipJoint.angularYZDrive = jointYZDrive;
            interactInput.action.started += Interact;
        }
    }

    void LegPos()
    {
        Vector3 leftLegAngle = leftLeg.localEulerAngles;
        Vector3 rightLegAngle = rightLeg.localEulerAngles;
        // print("old : " + leftLegAngle);
        leftLegAngle.x = 350f + 30f * Mathf.Sin(currentStepTime * stepMultiplier);
        rightLegAngle.x = 350f - 30f * Mathf.Sin(currentStepTime * stepMultiplier);
        // print("new : " + leftLegAngle);
        leftLeg.localRotation = Quaternion.Euler(leftLegAngle);
        rightLeg.localRotation = Quaternion.Euler(rightLegAngle);
        currentStepTime += stepSpeed * Time.fixedDeltaTime;
    }

    void Interact(InputAction.CallbackContext c)
    {
        if (holdingObject == null)
        {
            RaycastHit hit;
            if (Physics.Raycast(interactPoint.transform.position, interactPoint.transform.forward, out hit, interactRange))
            {
                PickableObject obj = hit.collider.GetComponent<PickableObject>();
                PickObject(obj);
            }
        }
        else
        {
            DropObject();
        }
    }

    void Jump(InputAction.CallbackContext c)
    {
        RaycastHit hit;
        if (Physics.Raycast(groundPoint.position, -transform.up, out hit, 0.3f))
        {
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }
    }

    void PickObject(PickableObject obj)
    {
        if (obj != null)
        {
            obj.Hold(this);
            holdingObject = obj;
        }
    }

    void DropObject()
    {
        holdingObject.Drop();
        holdingObject = null;
    }

    void Fall(float fallDuration)
    {
        isFall = true;
        interactInput.action.started -= Interact;

        JointDrive jointXDrive = hipJoint.angularXDrive;
        jointXDrive.positionSpring = 0f;
        hipJoint.angularXDrive = jointXDrive;

        if(holdingObject != null)
        {
            holdingObject.Drop();
            holdingObject = null;
        }

        JointDrive jointYZDrive = hipJoint.angularYZDrive;
        jointYZDrive.positionSpring = 0f;
        hipJoint.angularYZDrive = jointYZDrive;

        lastfallDuration = fallDuration;
        lastFallTime = Time.time;
    }
}
