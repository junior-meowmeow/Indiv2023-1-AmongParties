using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;

public class MenuUIManager : NetworkBehaviour
{

    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject menuCamera;

    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField addressInput;
    private string address = "127.0.0.1";
    private string username = "Player";

    private void OnEnable()
    {
        MainUIManager.updateGameStateUI += GameStateChanged;
    }

    private void OnDisable()
    {
        MainUIManager.updateGameStateUI -= GameStateChanged;
    }

    private void Awake()
    {
        InitButton();
    }

    private void InitButton()
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
            GameDataManager.Instance.OnJoinLobby(username);
            SoundManager.Play("select");
        });
        clientBtn.onClick.AddListener(() => {
            MainUIManager.Instance.ToggleLoading(true, "Finding Server...");
            NetworkManager.Singleton.StartClient();
            GameDataManager.Instance.OnJoinLobby(username);
            SoundManager.Play("select");
        });
    }

    private void GameStateChanged(GameState gameState)
    {
        menuCanvas.SetActive(gameState == GameState.MENU);
        menuCamera.SetActive(gameState == GameState.MENU);
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
