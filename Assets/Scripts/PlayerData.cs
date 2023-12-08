using System.Collections;
using System.Collections.Generic;
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

    void Awake()
    {
        meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        playerColor = meshRenderer.material.color;
    }

    void Start()
    {
        GameManager.instance.AddNewPlayer(this);
        if(IsOwner)
        {
            SetPlayerNameServerRPC(GameManager.instance.currentPlayerName);
        }
        player = gameObject.GetComponent<PlayerController>();
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
        Debug.Log("OnNetworkDespawn Called");
        GameManager.instance.RemovePlayer(this);
        NetworkManagerUI.instance.UpdatePlayerList();
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
        if(player.holdingObject != null)
        {
            player.holdingObject.ShowHand(player.holdingObject.isHandShow);
        }
    }

    public void SetPlayerColor(Color color)
    {
        playerColor = color;
        meshRenderer.material.color = color;
    }

    public Color GetColor()
    {
        return meshRenderer.material.color;
    }

    [ServerRpc]
    private void SetPlayerNameServerRPC(string name)
    {
        SetPlayerNameClientRPC(name);
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
        NetworkManagerUI.instance.UpdatePlayerList();
    }

}
