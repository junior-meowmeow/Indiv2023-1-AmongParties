using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SpawnableObject : NetworkBehaviour
{
    [ClientRpc]
    public void SetActiveClientRPC(bool a)
    {
        gameObject.SetActive(a);
    }
}
