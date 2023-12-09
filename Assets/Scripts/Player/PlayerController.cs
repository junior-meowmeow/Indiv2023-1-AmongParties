using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerController : SyncObject
{
    public bool isDisplayUI = true;
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
    [SerializeField] private float fallDuration = 3f;
    private float lastfallDuration;
    private float lastFallTime;
    [SerializeField] private bool isFall = false;

    private float scrollAxis = 0f;

    [Header ("Input Action")]
    public InputActionReference moveInput;
    public InputActionReference jumpInput;
    public InputActionReference interactInput;
    public InputActionReference useItemInput;
    public InputActionReference useItemAltInput;
    public InputActionReference scrollInput;
    public InputActionReference playDeadInput;
    public InputActionReference toggleUIInput;

    [Header ("Body")]
    public Rigidbody rb;
    public ConfigurableJoint hipJoint;
    public ConfigurableJoint leftLeg;
    public ConfigurableJoint rightLeg;

    private CameraController cam;
    private PlayerData playerData;
    public PickableObject holdingObject;
    public Vector3 defaultHoldPos = new(0f, 0.006f, -0.014f);
    public Transform holdPos;
    public Transform interactPoint;
    public InteractionCollider interactionCollider;
    public Transform groundPoint;

    protected override void Awake()
    {
        base.Awake();
        //SyncObjectManager.instance.AddObjectListServerRPC(net_obj);
    }

    void Start()
    {
        cam = GetComponentInChildren<CameraController>();
        playerData = GetComponent<PlayerData>();
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
        useItemAltInput.action.started += _ => { UseItemAltServerRPC(true); };
        useItemAltInput.action.canceled += _ => { UseItemAltServerRPC(false); };
        playDeadInput.action.started += _ => { Fall(fallDuration); };
        toggleUIInput.action.started += ToggleUI;

        //hipJoint.GetComponent<NetworkTransform>().enabled = false;
        //WarpServerRPC(GameManager.instance.lobbyLocation.position);

        SyncObjectManager.instance.Initialize();
        GameManager.instance.UpdateGameStateServerRPC();
    }

    private void Update()
    {
        if (!IsOwner) return;

        CameraControl();
    }

    void FixedUpdate()
    {
        CheckFallServer();
        if (isMoving)
        {
            LegPos();
        }
        if (!IsOwner) return;
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

    void Interact(InputAction.CallbackContext c)
    {
        if (holdingObject == null)
        {
            if (interactionCollider.HasObjectNearby)
            {
                PickObjectServerRPC();
            }
        }
        else
        {
            DropObjectServerRPC(isSteal: false);
        }
    }

    [ServerRpc]
    void PickObjectServerRPC()
    {
        if (interactionCollider.HasObjectNearby)
        {
            PickableObject obj = interactionCollider.GetNearestObject();
            if (obj.IsPickable() && obj.TryGetComponent(out SyncObject sync_obj))
            {
                PickObjectClientRPC(SyncObjectManager.instance.objectToKey[sync_obj]);
            }
        }
    }

    [ClientRpc]
    void PickObjectClientRPC(ushort obj_key)
    {
        SyncObject sync_obj;
        if (sync_obj = SyncObjectManager.instance.objectList[obj_key])
        {
            PickableObject obj = sync_obj.GetComponent<PickableObject>();
            obj.Hold(this);
            holdingObject = obj;
            holdPos.localPosition = obj.holdPos;
            holdPos.localRotation = Quaternion.Euler(obj.holdRotation);
            speedMultiplier = Mathf.Clamp(1f - holdingObject.weight / 100f, 0.1f, 1f);
            jumpMultiplier = Mathf.Clamp(1f - holdingObject.weight / 100f, 0.1f, 1f);

            if(IsOwner)
            {
                ObjectInfoController.instance.SetHoldingText(obj.objectName, obj.description);
            }
        }
    }

    [ServerRpc]
    public void DropObjectServerRPC(bool isSteal)
    {
        if (holdingObject != null)
        {
            DropObjectClientRPC(isSteal);
        }
    }

    [ClientRpc]
    void DropObjectClientRPC(bool isSteal)
    {
        if (holdingObject != null)
        {
            if(!isSteal)
            {
                holdingObject.Drop();
            }
            Drop();
        }
    }

    private void Drop()
    {
        holdPos.localPosition = defaultHoldPos;
        holdPos.localRotation = Quaternion.identity;
        holdingObject = null;
        speedMultiplier = 1f;
        jumpMultiplier = 1f;

        if (IsOwner)
        {
            ObjectInfoController.instance.ResetHoldingText();
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
            Drop();
        }
    }

    [ServerRpc]
    void UseItemAltServerRPC(bool isHolding)
    {
        if (holdingObject == null) return;
        if (!holdingObject.hasAltUse) return;
        UseItemAltClientRPC(isHolding);
    }

    [ClientRpc]
    void UseItemAltClientRPC(bool isHolding)
    {
        if (holdingObject == null) return;
        holdingObject.AltUse(isHolding);
        if (holdingObject.isDroppedAfterAltUse)
        {
            holdingObject.Drop();
            Drop();
        }
    }

    void JumpServer(InputAction.CallbackContext c)
    {
        if(!IsOwner)
        {
            return;
        }
        if (Physics.Raycast(groundPoint.position, Vector3.down, out _, 0.25f))
        {
            JumpServerRPC();
        }
    }

    [ServerRpc]
    void JumpServerRPC()
    {
        if (Physics.Raycast(groundPoint.position, Vector3.down, out _, 0.25f))
        {
            JumpClientRPC();
        }
    }

    [ClientRpc]
    void JumpClientRPC()
    {
        rb.AddForce(jumpForce * jumpMultiplier * Vector3.up, ForceMode.Impulse);
    }

    [ClientRpc]
    public void FallClientRPC(float fallDuration)
    {
        Fall(fallDuration);
    }

    void CheckFallServer()
    {
        if (!IsServer) return;
        if (isFall && Time.time - lastFallTime > lastfallDuration)
        {
            GetUpClientRPC();
        }
    }

    [ClientRpc]
    void GetUpClientRPC()
    {
        GetUp();
    }

    void GetUp()
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

    void LegPos()
    {
        float offset = 0f;

        float distance = 1f;
        Ray ray = new(hipJoint.transform.position, Vector3.down);

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

    public PlayerData GetPlayerData()
    {
        return playerData;
    }

    [ServerRpc]
    public void WarpServerRPC(Vector3 destination)
    {
        WarpClientRPC(destination);
    }

    [ClientRpc]
    void WarpClientRPC(Vector3 destination)
    {
        rb.transform.position = destination;
    }

    public void UpdateObjectInfo(bool isSet, string itemName)
    {
        if (!IsOwner) return;
        if(isSet)
        {
            ObjectInfoController.instance.SetItemText(itemName);
        }
        else
        {
            ObjectInfoController.instance.ResetItemText();
        }
    }

    private void ToggleUI(InputAction.CallbackContext c)
    {
        isDisplayUI = !isDisplayUI;
        GameManager.instance.isLocalPlayerEnableUI = isDisplayUI;
        foreach (PlayerData player in GameManager.instance.GetPlayerList())
        {
            if (player == null) return;
            player.playerNameText.enabled = isDisplayUI;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public override void SyncObjectServerRPC(ushort obj_key)
    {
        base.SyncObjectServerRPC(obj_key);
        SyncPlayerDataClientRPC(obj_key, playerData.playerName, playerData.playerColor);
        float[] data = {speedMultiplier, jumpMultiplier, lastFallTime, lastfallDuration, hipJoint.targetRotation.eulerAngles.y };
        if(holdingObject == null)
        {
            SyncPlayerVariableClientRPC(obj_key, ushort.MaxValue, data, isFall);
        }
        else
        {
            SyncPlayerVariableClientRPC(obj_key, SyncObjectManager.instance.objectToKey[holdingObject], data, isFall);
        }
    }

    [ClientRpc]
    private void SyncPlayerVariableClientRPC(ushort obj_key, ushort holdingObject_key, float[] data, bool isFall)
    {
        if (IsServer) return;
        PlayerController player = SyncObjectManager.instance.objectList[obj_key].GetComponent<PlayerController>();
        if (holdingObject_key != ushort.MaxValue)
        {
            player.holdingObject = SyncObjectManager.instance.objectList[holdingObject_key].GetComponent<PickableObject>();
        }
        player.speedMultiplier = data[0];
        player.jumpMultiplier = data[1];
        player.lastFallTime = data[2];
        player.lastfallDuration = data[3];
        player.hipJoint.targetRotation = Quaternion.Euler(0, data[4], 0);
        player.isFall = isFall;
    }

    [ClientRpc]
    private void SyncPlayerDataClientRPC(ushort obj_key, string playerName, Color playerColor)
    {
        if (IsServer) return;
        PlayerData obj = SyncObjectManager.instance.objectList[obj_key].GetComponent<PlayerData>();
        obj.SetPlayerName(playerName);
        obj.SetPlayerColor(playerColor);
    }

}
