using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header ("Movement")]
    [SerializeField] private float movementSpeed = 5.0f;
    [SerializeField] private float rotationSpeed = 1.0f;
    [SerializeField] private float jumpForce = 100f;
    [SerializeField] private float speedMultiplier = 1f;
    [SerializeField] private float jumpMultiplier = 1f;
    [SerializeField] private float stepSpeed = 20f;
    private float stepMultiplier = 0.5f;
    private float currentStepTime;
    private bool stopped = false;

    [Header ("Object Interact")]
    [SerializeField] private float interactRange = 3.0f;
    [SerializeField] private float maxThrowForce = 600f;
    [SerializeField] private float startThrowForce = 300f;
    [SerializeField] private float throwChargeSpeed = 300f;
    [SerializeField] private float throwForce;

    [SerializeField] private float fallDuration = 3f;
    private float lastfallDuration;
    private float lastFallTime;
    private bool isFall = false;

    private float scrollAxis = 0f;

    private CharacterController characterController;

    [Header ("Input Action")]
    public InputActionReference moveInput;
    public InputActionReference jumpInput;
    public InputActionReference interactInput;
    public InputActionReference throwInput;
    public InputActionReference scrollInput;
    public InputActionReference playDeadInput;

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
        CameraControl();
        CheckThrow();
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
        jumpInput.action.started += _ => { Jump(); };
        throwInput.action.started += _ => { ChargeThrowObject(true); };
        throwInput.action.canceled += _ => { ChargeThrowObject(false); };
        playDeadInput.action.started += _ => { Fall(fallDuration); };
    }

    void Movement()
    {
        Vector2 moveDir = moveInput.action.ReadValue<Vector2>();

        if (moveDir == Vector2.zero)
        {
            if (!stopped)
            {
                // last step by degree (radian)
                float lastStep = (currentStepTime * stepMultiplier) % (2 * Mathf.PI);
                // go to next leg
                currentStepTime = (lastStep < Mathf.PI) ? (Mathf.PI / stepMultiplier) : 0f;
                stopped = true;
            }
            return;
        }
        stopped = false;
        
        LegPos();

        /* Movement */

        float forwardMovement = moveDir.y * movementSpeed * speedMultiplier;
        float strafeMovement = moveDir.x * movementSpeed * speedMultiplier;
        float camRotY = cam.transform.localEulerAngles.y;
 
        rb.velocity = Quaternion.Euler(0f, camRotY, 0f) * transform.forward * forwardMovement
                    + Quaternion.Euler(0f, camRotY, 0f) * transform.right * strafeMovement;

        /* Rotation */

        float targetAngle = Mathf.Atan2(-moveDir.x, moveDir.y) * Mathf.Rad2Deg - camRotY;
        targetAngle = (targetAngle + 720f) % 360f;

        float currentAngle = hipJoint.targetRotation.eulerAngles.y;
        float distance = targetAngle - currentAngle;
        //Pass Zero Degree
        if (Mathf.Abs(targetAngle - currentAngle) > 180f)
        {
            distance = targetAngle - currentAngle + (currentAngle > 180? 360f : -360f);
        }

        if(Mathf.Abs(distance) > rotationSpeed * speedMultiplier * Time.fixedDeltaTime)
        {
            targetAngle = currentAngle + rotationSpeed * speedMultiplier * Mathf.Sign(distance) * Time.fixedDeltaTime;
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
        currentStepTime += stepSpeed * speedMultiplier * Time.fixedDeltaTime;
    }

    void CameraControl()
    {
        float axis = scrollInput.action.ReadValue<Vector2>().normalized.y;
        if (scrollAxis != axis)
        {
            cam.setScrollAxis(axis);
            scrollAxis = axis;
        } 
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

    void Jump()
    {
        RaycastHit hit;
        if (Physics.Raycast(groundPoint.position, -transform.up, out hit, 0.3f))
        {
            rb.AddForce(transform.up * jumpForce * jumpMultiplier, ForceMode.Impulse);
        }
    }

    void PickObject(PickableObject obj)
    {
        if (obj != null)
        {
            obj.Hold(this);
            holdingObject = obj;
            speedMultiplier = Mathf.Clamp(1f - holdingObject.weight / 100f, 0.1f, 1f);
            jumpMultiplier = Mathf.Clamp(1f - holdingObject.weight / 100f, 0.1f, 1f);
        }
    }

    PickableObject DropObject()
    {
        holdingObject.Drop();
        var toReturn = holdingObject;
        holdingObject = null;
        speedMultiplier = 1f;
        jumpMultiplier = 1f;

        return toReturn;
    }

    void ChargeThrowObject(bool isCharge)
    {
        if (holdingObject == null) return;
        if (isCharge)
        {
            throwForce = startThrowForce;
        }
        else
        {
            DropObject().GetComponent<Rigidbody>().AddForce(rb.transform.forward * -throwForce);
            throwForce = 0;
        }
    }

    void CheckThrow()
    {
        if (throwForce == 0 || throwForce >= maxThrowForce) return;
        
        throwForce += Time.deltaTime * throwChargeSpeed;
        if (throwForce > maxThrowForce) throwForce = maxThrowForce;
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
