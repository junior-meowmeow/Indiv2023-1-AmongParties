using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private NetworkObject[] allObjects;
    [Header ("Movement")]
    [SerializeField] private float movementSpeed = 5.0f;
    [SerializeField] private float rotationSpeed = 1.0f;
    [SerializeField] private float jumpForce = 100f;
    [SerializeField] private float speedMultiplier = 1f;
    [SerializeField] private float jumpMultiplier = 1f;
    [SerializeField] private float stepSpeed = 20f;
    [SerializeField] private float slopeMultiplier = -60f;
    private float stepMultiplier = 0.5f;
    private float currentStepTime;
    private bool stopped = false;
    private bool isMoving = false;

    [Header ("Object Interact")]
    [SerializeField] private float interactRange = 3.0f;

    [SerializeField] private float fallDuration = 3f;
    private float lastfallDuration;
    private float lastFallTime;
    private bool isFall = false;

    private float scrollAxis = 0f;

    [Header ("Input Action")]
    public InputActionReference moveInput;
    public InputActionReference jumpInput;
    public InputActionReference interactInput;
    public InputActionReference useItemInput;
    public InputActionReference scrollInput;
    public InputActionReference playDeadInput;

    [Header ("Body")]
    public Rigidbody rb;
    public ConfigurableJoint hipJoint;
    public ConfigurableJoint leftLeg;
    public ConfigurableJoint rightLeg;

    private CameraController cam;
    private PlayerSetting playerSetting;
    public PickableObject holdingObject;
    public Vector3 defaultHoldPos = new(0f, 0.006f, -0.014f);
    public Transform holdPos;
    public Transform interactPoint;
    public InteractionCollider interactionCollider;
    public Transform groundPoint;

    /*
    private void Awake()
    {
        if(IsOwner)
        {
            SyncNetworkObjectServerRPC();
        }
    }
    */

    void Start()
    {
        cam = GetComponentInChildren<CameraController>();
        playerSetting = GetComponent<PlayerSetting>();
        if (!IsOwner) cam.Disable();

        Cursor.lockState = CursorLockMode.Locked;

        currentStepTime = 0f;

        if(!IsOwner)
        {
            return;
        }
        interactInput.action.started += Interact;
        //jumpInput.action.started += Jump;
        jumpInput.action.started += JumpServer;
        useItemInput.action.started += _ => { UseItemServerRPC(true); };
        useItemInput.action.canceled += _ => { UseItemServerRPC(false); };
        playDeadInput.action.started += _ => { Fall(fallDuration); };

        //hipJoint.GetComponent<NetworkTransform>().enabled = false;
    }

    private void Update()
    {
        if (!IsOwner) return;

        CameraControl();
    }

    void FixedUpdate()
    {
        if (isMoving)
        {
            LegPos();
        }
        if (!IsOwner) return;

        /*
        if (Input.GetKeyDown(KeyCode.K))
        {
            SyncNetworkObjectServerRPC();
        }
        */

        CheckFall();
        if (isFall) return;
        MoveServer();
    }

    void MoveServer()
    {
        Vector2 moveDir = moveInput.action.ReadValue<Vector2>();

        if (moveDir == Vector2.zero)
        {
            if (!stopped)
            {
                SetMoveServerRPC(false);
                stopped = true;
            }
            return;
        }
        if(stopped)
        {
            SetMoveServerRPC(true);
            stopped = false;
        }

        float camRotY = cam.transform.localEulerAngles.y;
        float targetAngle = Mathf.Atan2(-moveDir.x, moveDir.y) * Mathf.Rad2Deg - camRotY;
        targetAngle = (targetAngle + 720f) % 360f;

        MoveServerRPC(camRotY, moveDir.x, moveDir.y, targetAngle);

    }

    [ServerRpc]
    void MoveServerRPC(float camRotY, float moveX, float moveY, float targetAngle)
    {
        if (isFall) return;

        float forwardMovement = moveY * movementSpeed * speedMultiplier;
        float strafeMovement = moveX * movementSpeed * speedMultiplier;

        Vector3 targetVelocity = Quaternion.Euler(0f, camRotY, 0f) * transform.forward * forwardMovement
                    + Quaternion.Euler(0f, camRotY, 0f) * transform.right * strafeMovement;

        float currentAngle = hipJoint.targetRotation.eulerAngles.y;
        float distance = targetAngle - currentAngle;
        //Pass Zero Degree
        if (Mathf.Abs(targetAngle - currentAngle) > 180f)
        {
            distance = targetAngle - currentAngle + (currentAngle > 180 ? 360f : -360f);
        }

        if (Mathf.Abs(distance) > rotationSpeed * speedMultiplier * Time.fixedDeltaTime)
        {
            targetAngle = currentAngle + rotationSpeed * speedMultiplier * Mathf.Sign(distance) * Time.fixedDeltaTime;
        }

        Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);

        MoveClientRPC(targetVelocity, targetRotation);

        /* position */
        //client sent camRot + forward movement + strafe movement
        //server get camrot(int)
        //vv

        //1. use NetworkVariable byte (0-255) but *2 when use 0-500+
        //2. update this variable only when trying to move + delta targetangle more than 2
        //3. set delay to about 0.1 sec per request

        //^^
        //server set rb.velocity

        /* rotation */
        //server know current angle
        //client send targetAngle
        //vv

        //1. use NetworkVariable byte (0-255) but *2 when use 0-500+
        //2. update this variable only when trying to move + delta targetangle more than 2
        //3. set delay to about 0.1 sec per request

        //^^
        //server calculate targetRotation

        /* leg rotation */
        //clientRpc when start moving
        //clientRpc when stop moving
        //server send signal to client move leg between this
        //bool movingLeg
    }

    [ClientRpc]
    void MoveClientRPC(Vector3 targetVelocity, Quaternion targetRotation)
    {
        rb.velocity = targetVelocity;
        hipJoint.targetRotation = targetRotation;
    }

    [ServerRpc]
    void SetMoveServerRPC(bool isMove)
    {
        SetMoveClientRPC(isMove);
    }

    [ClientRpc]
    void SetMoveClientRPC(bool isMove)
    {
        if(isMove)
        {
            isMoving = true;
        }
        else
        {
            // last step by degree (radian)
            float lastStep = (currentStepTime * stepMultiplier) % (2 * Mathf.PI);
            // go to next leg
            currentStepTime = (lastStep < Mathf.PI) ? (Mathf.PI / stepMultiplier) : 0f;
            // reset leg rotation
            leftLeg.targetRotation = Quaternion.identity;
            rightLeg.targetRotation = Quaternion.identity;
            isMoving = false;
        }
    }

    /*
    [ServerRpc]
    void SyncNetworkObjectServerRPC()
    {
        allObjects = FindObjectsOfType<NetworkObject>();
        foreach (NetworkObject obj in allObjects)
        {
            SyncNetworkObjectClientRPC(obj, obj.transform.position, obj.transform.rotation, obj.transform.localScale);
        }
    }

    [ClientRpc]
    private void SyncNetworkObjectClientRPC(NetworkObjectReference obj, Vector3 position, Quaternion rotation, Vector3 localScale)
    {
        if (obj.TryGet(out NetworkObject targetObject))
        {
            targetObject.transform.SetPositionAndRotation(position, rotation);
            targetObject.transform.localScale = localScale;
        }
        else
        {
            // Object not found on server, likely because it already has been destroyed/despawned.
        }
    }
    */

    void Interact(InputAction.CallbackContext c)
    {
        if (holdingObject == null)
        {
            if (interactionCollider.HasObjectNearby)
            {
                //PickObject(interactionCollider.GetNearestObject());
                PickObjectServerRPC();
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
            DropObjectServerRPC();
        }
    }

    [ServerRpc]
    void PickObjectServerRPC()
    {
        if (interactionCollider.HasObjectNearby)
        {
            PickableObject obj = interactionCollider.GetNearestObject();
            if (obj.IsPickable() && obj.TryGetComponent(out NetworkObject obj_ref))
            {
                PickObjectClientRPC(obj_ref);
            }
        }
    }

    [ClientRpc]
    void PickObjectClientRPC(NetworkObjectReference obj_ref)
    {
        PickableObject obj; 
        if(obj_ref.TryGet(out NetworkObject targetObject))
        {
            obj = targetObject.GetComponent<PickableObject>();
            obj.Hold(this);
            holdingObject = obj;
            holdPos.localPosition = obj.holdPos;
            holdPos.localRotation = Quaternion.Euler(obj.holdRotation);
            speedMultiplier = Mathf.Clamp(1f - holdingObject.weight / 100f, 0.1f, 1f);
            jumpMultiplier = Mathf.Clamp(1f - holdingObject.weight / 100f, 0.1f, 1f);
        }
    }

    [ServerRpc]
    void DropObjectServerRPC()
    {
        if (holdingObject != null)
        {
            DropObjectClientRPC();
        }
    }

    [ClientRpc]
    void DropObjectClientRPC()
    {
        if (holdingObject != null)
        {
            holdingObject.Drop();
            holdPos.localPosition = defaultHoldPos;
            holdPos.localRotation = Quaternion.identity;
            holdingObject = null;
            speedMultiplier = 1f;
            jumpMultiplier = 1f;
        }
    }

    void UseItem(bool isHolding)
    {
        if (holdingObject == null) return;
        if(holdingObject.isDroppedAfterUse)
        {
            holdingObject.Drop();
            holdPos.localPosition = defaultHoldPos;
            holdPos.localRotation = Quaternion.identity;
            holdingObject = null;
            speedMultiplier = 1f;
            jumpMultiplier = 1f;
        }
    }

    [ServerRpc]
    void UseItemServerRPC(bool isHolding)
    {
        if (holdingObject == null) return;
        UseItemClientRPC(isHolding);
    }

    [ClientRpc]
    void UseItemClientRPC(bool isHolding)
    {
        if (holdingObject == null) return;
        holdingObject.Use(isHolding);
        if (holdingObject.isDroppedAfterUse)
        {
            holdingObject.Drop();
            holdPos.localPosition = defaultHoldPos;
            holdPos.localRotation = Quaternion.identity;
            holdingObject = null;
            speedMultiplier = 1f;
            jumpMultiplier = 1f;
        }
    }

    void JumpServer(InputAction.CallbackContext c)
    {
        if (IsOwner && Physics.Raycast(groundPoint.position, -hipJoint.transform.up, out _, 0.15f))
        {
            JumpServerRPC();
        }
    }

    [ServerRpc]
    void JumpServerRPC()
    {
        if (Physics.Raycast(groundPoint.position, -hipJoint.transform.up, out _, 0.15f))
        {
            JumpClientRPC();
        }
    }

    [ClientRpc]
    void JumpClientRPC()
    {
        rb.AddForce(jumpForce * jumpMultiplier * Vector3.up, ForceMode.Impulse);
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
            //jumpInput.action.started += Jump;
            jumpInput.action.started += JumpServer;

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
        float offset = 0f;

        float distance = 1f;
        Ray ray = new Ray(hipJoint.transform.position, Vector3.down);

        //Debug.DrawRay(ray.origin, ray.direction * distance, Color.red);

        if (Physics.Raycast(ray, out RaycastHit hit, distance))
        {
            /*
            Debug.Log("Hit collider " + hit.collider + ", at " + hit.point + ", normal " + hit.normal);
            Debug.DrawRay(hit.point, hit.normal * 2f, Color.blue);
            */
            /*
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            Quaternion rotation = Quaternion.LookRotation(hit.normal, Vector3.up);
            */
            float dotProduct = Vector3.Dot(hit.normal, hipJoint.transform.forward);
            offset = dotProduct * slopeMultiplier;
            //Debug.Log("angle " + angle);
        }

        float leftLegAngle = offset + 25f * Mathf.Sin(currentStepTime * stepMultiplier);
        float rightLegAngle = offset + -25f * Mathf.Sin(currentStepTime * stepMultiplier);
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

    void Fall(float fallDuration)
    {
        isFall = true;
        interactInput.action.started -= Interact;
        //jumpInput.action.started -= Jump;
        jumpInput.action.started -= JumpServer;

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

    public PlayerSetting GetPlayerSetting()
    {
        return playerSetting;
    }
}
