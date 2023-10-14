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
    [SerializeField] private float stepMultiplier = 0.01f;
    private float stepSpeed = 20f;
    private float currentStepTime;

    private bool isFall = false;
    [SerializeField] private float defaultFallDuration = 3f;
    private float lastfallDuration;
    private float lastFallTime;

    public bool holdRotation = false;

    private CharacterController characterController;

    [Header ("Input Action")]
    public InputActionReference moveInput;
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

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        cam = GetComponentInChildren<CameraController>();
        currentStepTime = 0f;
        Cursor.lockState = CursorLockMode.Locked;
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

        LegPos();

        float forwardMovement = moveDir.y * movementSpeed;
        float strafeMovement = moveDir.x * movementSpeed;

        float camRot = cam.transform.localEulerAngles.y;
        
        Debug.Log(camRot);
        rb.velocity = Quaternion.Euler(0f, camRot, 0f) * transform.forward * forwardMovement
                    + Quaternion.Euler(0f, camRot, 0f) * transform.right * strafeMovement;

        float targetAngle = (Mathf.Atan2(-moveDir.x, moveDir.y) * Mathf.Rad2Deg - camRot);
        if(targetAngle < 0)
        {
            targetAngle += 360f;
        }

        Debug.Log("T " + targetAngle);

        float currentAngle = hipJoint.targetRotation.eulerAngles.y;

        bool passZeroDegree = Mathf.Abs(targetAngle - currentAngle) > 180f;

        float distance;
        if (passZeroDegree)
        {
            if (currentAngle > 180)
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
