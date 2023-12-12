using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class MenuUIManager : NetworkBehaviour
{
    [Header("Menu")]
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private Camera menuCam;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField addressInput;
    private string address = "127.0.0.1";
    private string username = "Player";

    void Awake()
    {
        InitButton();
    }

    void InitButton()
    {
        if (addressInput != null)
        {
            addressInput.onEndEdit.AddListener(delegate { AddressChanged(addressInput); });
        }
        if (usernameInput != null)
        {
            usernameInput.onEndEdit.AddListener(delegate { UsernameChanged(usernameInput); });
        }
        hostBtn.onClick.AddListener(() => {
            MainUIManager.Instance.ToggleLoading(true, "Loading...");
            NetworkManager.Singleton.StartHost();
            GameManager.instance.JoinLobby(username);
            SoundManager.Play("select");
        });
        clientBtn.onClick.AddListener(() => {
            MainUIManager.Instance.ToggleLoading(true, "Findind Server...");
            NetworkManager.Singleton.StartClient();
            GameManager.instance.JoinLobby(username);
            SoundManager.Play("select");
        });
    }

    private void AddressChanged(TMP_InputField input)
    {
        address = input.text;
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
            address,  // The IP address is a string
            (ushort)12345 // The port number is an unsigned short
            );
        Debug.Log("New Address : " + address);
    }

    private void UsernameChanged(TMP_InputField input)
    {
        username = input.text;
        if (username == string.Empty)
        {
            username = "Player";
        }
        Debug.Log("New Username : " + username);
    }

}
