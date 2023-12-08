using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PlayerColorButtonUI : MonoBehaviour
{
    [SerializeField] private Color color;

    public void Init(Color color)
    {
        this.color = color;
        GetComponent<Image>().color = color;
        GetComponent<Button>().onClick.AddListener(() => {
            NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerData>().SetPlayerColorServerRPC(color);
        });
    }
}
