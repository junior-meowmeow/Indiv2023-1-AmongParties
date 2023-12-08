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

    void Awake()
    {
        meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
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
        playerNameText.rectTransform.position = player.rb.transform.position + Vector3.up * 1.5f;
    }

    public void SetPlayerColor(Color color)
    {
        SetPlayerColorServerRPC(color);
    }

    [ServerRpc]
    void SetPlayerColorServerRPC(Color color)
    {
        SetPlayerColorClientRPC(color);
    }

    [ClientRpc]
    void SetPlayerColorClientRPC(Color color)
    {
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
        playerName = name;
        playerNameText.text = name;
        NetworkManagerUI.instance.UpdatePlayerList();
    }
}
