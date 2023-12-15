using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerController : SyncObject
{
    public bool isDisplayUI = true;
    [Header("Movement")]
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
    private float lastfallDuration;
    private float lastFallTime;
    [SerializeField] private bool isFall = false;
    [SerializeField] private bool isDead = false;

    private float scrollAxis = 0f;

    [Header("Input Action")]
    [SerializeField] private PlayerControl playerControl;

    [Header("Body")]
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
        GlobalInit();
        if (!IsOwner) return;
        LocalInit();
    }

    private void GlobalInit()
    {
        playerData = GetComponent<PlayerData>();
        cam = GetComponentInChildren<CameraController>();
        if (!IsOwner)
        {
            cam.Disable();
        }
        Cursor.lockState = CursorLockMode.Locked;
        currentStepTime = 0f;
    }

    private void LocalInit()
    {
        playerControl = new PlayerControl();
        playerControl.Enable();

        playerControl.Humanoid.ToggleUI.started += ToggleUI;
        playerControl.Humanoid.Jump.started += JumpServer;
        playerControl.Humanoid.Interact.started += Interact;
        playerControl.Humanoid.UseItem.started += _ => { UseItem(true); };
        playerControl.Humanoid.UseItem.canceled += _ => { UseItem(false); };
        playerControl.Humanoid.UseItemAlt.started += _ => { UseItemAlt(true); };
        playerControl.Humanoid.UseItemAlt.canceled += _ => { UseItemAlt(false); };

        //playDeadInput.action.started += _ => { Fall(fallDuration); };

        SyncObjectManager.Instance.NetworkInitialize();
        GameDataManager.Instance.RequestGameDataServerRPC();
        MainUIManager.SyncUIState();
        ObjectiveUIManager.Instance.RequestObjectiveServerRPC();

        GameDataManager.Instance.localPlayer = this;
        SoundManager.SetInspectingPlayer(rb.transform);
        MainUIManager.Instance.ToggleLoading(false);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (!IsOwner) return;
        playerControl.Humanoid.ToggleUI.started -= ToggleUI;
        playerControl.Humanoid.Jump.started -= JumpServer;
        playerControl.Humanoid.Interact.started -= Interact;
        playerControl.Humanoid.UseItem.started -= _ => { UseItem(true); };
        playerControl.Humanoid.UseItem.canceled -= _ => { UseItem(false); };
        playerControl.Humanoid.UseItemAlt.started -= _ => { UseItemAlt(true); };
        playerControl.Humanoid.UseItemAlt.canceled -= _ => { UseItemAlt(false); };
        playerControl.Disable();
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
        Vector2 moveDir = playerControl.Humanoid.Move.ReadValue<Vector2>();

        if (moveDir == Vector2.zero)
        {
            if (!stopped)
            {
                SetMoveServerRPC(false);
                stopped = true;
            }
            return;
        }
        if (stopped)
        {
            SetMoveServerRPC(true);
            stopped = false;
        }

        float camRotY = cam.transform.localEulerAngles.y;
        float targetAngle = Mathf.Atan2(-moveDir.x, moveDir.y) * Mathf.Rad2Deg - camRotY;
        targetAngle = (targetAngle + 720f) % 360f;

        MoveServerRPC(camRotY, moveDir.x, moveDir.y, targetAngle);

    }

    [ServerRpc(Delivery = RpcDelivery.Unreliable)]
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
        if (isMove)
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
                PickObjectClientRPC(SyncObjectManager.Instance.GetKey(sync_obj));
            }
        }
    }

    [ClientRpc]
    void PickObjectClientRPC(ushort obj_key)
    {
        SyncObject sync_obj;
        if (sync_obj = SyncObjectManager.Instance.GetSyncObject(obj_key))
        {
            PickableObject obj = sync_obj.GetComponent<PickableObject>();
            obj.Hold(this);
            holdingObject = obj;
            holdPos.localPosition = obj.holdPos;
            holdPos.localRotation = Quaternion.Euler(obj.holdRotation);
            speedMultiplier = Mathf.Clamp(1f - holdingObject.weight / 100f, 0.1f, 1f);
            jumpMultiplier = Mathf.Clamp(1f - holdingObject.weight / 100f, 0.1f, 1f);

            if (IsOwner)
            {
                ObjectInfoController.Instance.SetHoldingText(obj.objectName, obj.description);
            }
        }
        SoundManager.PlayNew("hold", rb.transform.position);
    }

    [ServerRpc(RequireOwnership = false)]
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
            if (!isSteal)
            {
                holdingObject.Drop();
            }
            Drop();
        }
    }

    public void Drop()
    {
        holdPos.localPosition = defaultHoldPos;
        holdPos.localRotation = Quaternion.identity;
        holdingObject = null;
        speedMultiplier = 1f;
        jumpMultiplier = 1f;

        if (IsOwner)
        {
            ObjectInfoController.Instance.ResetHoldingText();
        }
    }

    private void UseItem(bool isHolding)
    {
        if (holdingObject == null) return;
        UseItemServerRPC(isHolding);
    }

    [ServerRpc]
    private void UseItemServerRPC(bool isHolding)
    {
        if (holdingObject == null) return;
        UseItemClientRPC(isHolding);
    }

    [ClientRpc]
    private void UseItemClientRPC(bool isHolding)
    {
        if (holdingObject == null) return;
        holdingObject.Use(isHolding);
        if (holdingObject.isDroppedAfterUse)
        {
            holdingObject.Drop();
            Drop();
        }
    }

    private void UseItemAlt(bool isHolding)
    {
        if (holdingObject == null) return;
        UseItemAltServerRPC(isHolding);
    }

    [ServerRpc]
    private void UseItemAltServerRPC(bool isHolding)
    {
        if (holdingObject == null) return;
        if (!holdingObject.hasAltUse) return;
        UseItemAltClientRPC(isHolding);
    }

    [ClientRpc]
    private void UseItemAltClientRPC(bool isHolding)
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
        if (!IsOwner)
        {
            return;
        }
        if (Physics.Raycast(groundPoint.position, -rb.transform.up, out _, 0.25f))
        {
            JumpServerRPC();
        }
    }

    [ServerRpc]
    void JumpServerRPC()
    {
        if (Physics.Raycast(groundPoint.position, -rb.transform.up, out _, 0.25f))
        {
            JumpClientRPC();
        }
    }

    [ClientRpc]
    void JumpClientRPC()
    {
        rb.AddForce(jumpForce * jumpMultiplier * Vector3.up, ForceMode.Impulse);
        SoundManager.PlayNew("jump", rb.transform.position + Vector3.down * 0.8f);
    }

    [ClientRpc]
    public void FallClientRPC(float fallDuration)
    {
        Fall(fallDuration);
        SoundManager.PlayNew("hit", rb.transform.position);
    }

    void Fall(float fallDuration)
    {
        isFall = true;
        isMoving = false;
        stopped = true;

        if (IsOwner)
        {
            playerControl.Humanoid.Interact.started -= Interact;
            playerControl.Humanoid.Jump.started -= JumpServer;
        }

        JointDrive jointXDrive = hipJoint.angularXDrive;
        jointXDrive.positionSpring = 0f;
        hipJoint.angularXDrive = jointXDrive;

        if (holdingObject != null)
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

    void CheckFallServer()
    {
        if (!IsServer) return;
        if (isFall && Time.time - lastFallTime > lastfallDuration && !isDead)
        {
            GetUpClientRPC();
        }
    }

    [ClientRpc]
    public void DieClientRPC()
    {
        Fall(0f);
        isDead = true;
        GameDataManager.Instance.UpdateDeadPlayerCount();
        SoundManager.PlayNew("dead", rb.transform.position);
    }

    [ClientRpc]
    public void ReviveClientRPC()
    {
        isDead = false;
        GameDataManager.Instance.UpdateDeadPlayerCount();
    }

    public bool CheckIsDead()
    {
        return isDead;
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

        if(IsOwner)
        {
            playerControl.Humanoid.Interact.started += Interact;
            playerControl.Humanoid.Jump.started += JumpServer;
        }

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

        float angle = Mathf.Sin(currentStepTime * stepMultiplier);

        float leftLegAngle = offset + 25f * angle;
        float rightLegAngle = offset + -25f * angle;
        leftLeg.targetRotation = Quaternion.Euler(leftLegAngle, 0f, 0f);
        rightLeg.targetRotation = Quaternion.Euler(rightLegAngle, 0f, 0f);

        currentStepTime += stepSpeed * speedMultiplier * Time.fixedDeltaTime;

        if (angle * Mathf.Sin(currentStepTime * stepMultiplier) < 0 && Physics.Raycast(groundPoint.position, Vector3.down, out _, 0.35f))
        {
            SoundManager.PlayNew("footstep", rb.transform.position + Vector3.down * 0.8f);
        }
    }

    void CameraControl()
    {
        float axis = playerControl.Humanoid.Scroll.ReadValue<Vector2>().normalized.y;
        if (scrollAxis != axis)
        {
            cam.SetScrollAxis(axis);
            scrollAxis = axis;
        }

    }

    public PlayerData GetPlayerData()
    {
        return playerData;
    }

    [ServerRpc(RequireOwnership = false)]
    public void WarpServerRPC(Vector3 destination, bool isDropItem)
    {
        WarpClientRPC(destination, isDropItem);
    }

    [ClientRpc]
    public void WarpClientRPC(Vector3 destination, bool isDropItem)
    {
        rb.transform.position = destination;
        if(isDropItem && holdingObject != null)
        {
            holdingObject.Drop();
            Drop();
        }
    }

    public void UpdateObjectInfo(bool isSet, string itemName)
    {
        if (!IsOwner) return;
        if (isSet)
        {
            ObjectInfoController.Instance.SetItemText(itemName);
        }
        else
        {
            ObjectInfoController.Instance.ResetItemText();
        }
    }

    private void ToggleUI(InputAction.CallbackContext c)
    {
        isDisplayUI = !isDisplayUI;
        GameDataManager.Instance.isLocalPlayerEnableUI = isDisplayUI;
        foreach (PlayerData player in GameDataManager.Instance.GetPlayerList())
        {
            if (player == null) continue;
            player.playerNameText.enabled = isDisplayUI;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public override void SyncObjectServerRPC(ushort obj_key)
    {
        base.SyncObjectServerRPC(obj_key);
        SyncPlayerDataClientRPC(obj_key, playerData.playerName, playerData.playerColor);
        float[] data = { speedMultiplier, jumpMultiplier, lastFallTime, lastfallDuration, hipJoint.targetRotation.eulerAngles.y };
        if (holdingObject == null)
        {
            SyncPlayerVariableClientRPC(obj_key, ushort.MaxValue, data, isFall);
        }
        else
        {
            SyncPlayerVariableClientRPC(obj_key, SyncObjectManager.Instance.GetKey(holdingObject), data, isFall);
        }
    }

    [ClientRpc]
    private void SyncPlayerVariableClientRPC(ushort obj_key, ushort holdingObject_key, float[] data, bool isFall)
    {
        if (IsServer) return;
        PlayerController player = SyncObjectManager.Instance.GetSyncObject(obj_key).GetComponent<PlayerController>();
        if (holdingObject_key != ushort.MaxValue)
        {
            player.holdingObject = SyncObjectManager.Instance.GetSyncObject(holdingObject_key).GetComponent<PickableObject>();
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
        PlayerData obj = SyncObjectManager.Instance.GetSyncObject(obj_key).GetComponent<PlayerData>();
        obj.SetPlayerName(playerName);
        obj.SetPlayerColor(playerColor);
    }

}
