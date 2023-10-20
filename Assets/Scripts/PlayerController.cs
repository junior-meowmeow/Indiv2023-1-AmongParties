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
    public ConfigurableJoint leftLeg;
    public ConfigurableJoint rightLeg;

    private CameraController cam;
    public PickableObject holdingObject;
    public Vector3 defaultHoldPos = new(0f, 0.006f, -0.014f);
    public Transform holdPos;
    public Transform interactPoint;
    public InteractionCollider interactionCollider;
    public Transform groundPoint;

    void Start()
    {
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
        jumpInput.action.started += Jump;
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
                // reset leg rotation
                leftLeg.targetRotation = Quaternion.identity;
                rightLeg.targetRotation = Quaternion.identity;
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
        if (isFall && Time.time - lastFallTime > lastfallDuration)
        {
            isFall = false;
            JointDrive jointXDrive = hipJoint.angularXDrive;
            jointXDrive.positionSpring = 1500f;
            hipJoint.angularXDrive = jointXDrive;

            JointDrive jointYZDrive = hipJoint.angularYZDrive;
            jointYZDrive.positionSpring = 1500f;
            hipJoint.angularYZDrive = jointYZDrive;
            interactInput.action.started += Interact;
            jumpInput.action.started += Jump;

            /* Get current direction */
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
            Vector3 currentRotation = new(0f, currentAngleY, 0f);
            //print(currentRotation);
            hipJoint.targetRotation = Quaternion.Euler(currentRotation);

        }
    }

    void LegPos()
    {
        /* old (use Transform)
        Vector3 leftLegAngle = leftLeg.localEulerAngles;
        Vector3 rightLegAngle = rightLeg.localEulerAngles;
        // print("old : " + leftLegAngle);
        leftLegAngle.x = 350f + 30f * Mathf.Sin(currentStepTime * stepMultiplier);
        rightLegAngle.x = 350f - 30f * Mathf.Sin(currentStepTime * stepMultiplier);
        // print("new : " + leftLegAngle);
        leftLeg.localRotation = Quaternion.Euler(leftLegAngle);
        rightLeg.localRotation = Quaternion.Euler(rightLegAngle);
        currentStepTime += stepSpeed * speedMultiplier * Time.fixedDeltaTime;
        */

        float leftLegAngle = 25f * Mathf.Sin(currentStepTime * stepMultiplier);
        float rightLegAngle = -25f * Mathf.Sin(currentStepTime * stepMultiplier);
        leftLeg.targetRotation = Quaternion.Euler(leftLegAngle, 0f, 0f);
        rightLeg.targetRotation = Quaternion.Euler(rightLegAngle, 0f, 0f);
        currentStepTime += stepSpeed * speedMultiplier * Time.fixedDeltaTime;
    }

    void CameraControl()
    {
        float axis = scrollInput.action.ReadValue<Vector2>().normalized.y;
        if (scrollAxis != axis)
        {
            cam.SetScrollAxis(axis);
            scrollAxis = axis;
        } 
    }

    void Interact(InputAction.CallbackContext c)
    {
        if (holdingObject == null)
        {
            if (interactionCollider.HasObjectNearby)
            {
                PickObject(interactionCollider.GetNearestObject());
            }
            /*
            if (Physics.Raycast(interactPoint.transform.position, interactPoint.transform.forward, out RaycastHit hit, interactRange))
            {
                PickableObject obj = hit.collider.GetComponent<PickableObject>();
                PickObject(obj);
            }
            */
        }
        else
        {
            DropObject();
        }
    }

    void Jump(InputAction.CallbackContext c)
    {
        if (Physics.Raycast(groundPoint.position, -transform.up, out _, 0.3f))
        {
            rb.AddForce(jumpForce * jumpMultiplier * Vector3.up, ForceMode.Impulse);
        }
    }

    void PickObject(PickableObject obj)
    {
        if (obj != null)
        {
            obj.Hold(this);
            holdingObject = obj;
            holdPos.localPosition = obj.holdPos;
            speedMultiplier = Mathf.Clamp(1f - holdingObject.weight / 100f, 0.1f, 1f);
            jumpMultiplier = Mathf.Clamp(1f - holdingObject.weight / 100f, 0.1f, 1f);
        }
    }

    PickableObject DropObject()
    {
        holdingObject.Drop();
        var toReturn = holdingObject;
        holdPos.localPosition = defaultHoldPos;
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
        jumpInput.action.started -= Jump;

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
