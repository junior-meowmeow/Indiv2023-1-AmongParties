using UnityEngine;
using Unity.Netcode;
using TMPro;

public class PlayerData : NetworkBehaviour
{
    [SerializeField] private SkinnedMeshRenderer meshRenderer;
    public PlayerController player;
    public TMP_Text playerNameText;
    public Objective objective;
    public string playerName;
    public Color playerColor;
    public int indexInPlayerList;
    public bool isDead;

    void Awake()
    {
        meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        playerColor = meshRenderer.material.color;
    }

    void Start()
    {
        GameDataManager.Instance.AddNewPlayer(this);
        if(IsOwner)
        {
            SetPlayerNameServerRPC(GameDataManager.Instance.localPlayerName);
        }
        player = gameObject.GetComponent<PlayerController>();
        playerNameText.enabled = GameDataManager.Instance.isLocalPlayerEnableUI;
    }

    void Update()
    {
        if(playerNameText != null)
        {
            playerNameText.rectTransform.position = player.rb.transform.position + Vector3.up * 1.5f;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        //Debug.Log("OnNetworkDespawn Called");
        GameDataManager.Instance.RemovePlayer(this);
        MainUIManager.Instance.UpdatePlayerList();
    }

    [ServerRpc]
    public void SetPlayerColorServerRPC(Color color)
    {
        SetPlayerColorClientRPC(color);
    }

    [ClientRpc]
    void SetPlayerColorClientRPC(Color color)
    {
        SetPlayerColor(color);
    }

    public void SetPlayerColor(Color color)
    {
        playerColor = color;
        meshRenderer.material.color = color;
        if (player.holdingObject != null)
        {
            player.holdingObject.ShowHand(player.holdingObject.isHandShow);
        }
    }

    public Color GetColor()
    {
        return meshRenderer.material.color;
    }

    [ServerRpc]
    private void SetPlayerNameServerRPC(string name)
    {
        string validUsername = GameDataManager.Instance.GetValidUsername(name);
        SetPlayerNameClientRPC(validUsername);
    }

    [ClientRpc]
    private void SetPlayerNameClientRPC(string name)
    {
        SetPlayerName(name);
    }

    public void SetPlayerName(string name)
    {
        playerName = name;
        playerNameText.text = name;
        MainUIManager.Instance.UpdatePlayerList();
    }

}
