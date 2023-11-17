using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerSetting : NetworkBehaviour
{
    [SerializeField] private SkinnedMeshRenderer meshRenderer;

    void Awake()
    {
        meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
    }

    void Start()
    {
        GameManager.instance.AddNewPlayer(this);
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
}
