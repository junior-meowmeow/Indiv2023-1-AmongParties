using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class HostDisconnectManager : NetworkBehaviour
{

    private bool isTryQuit = false;

    private void Awake()
    {
        Application.wantsToQuit += WantsToQuit;
        NetworkManager.Singleton.OnClientDisconnectCallback += CheckTryQuit;
        isTryQuit = false;
    }

    private bool WantsToQuit()
    {
        if (IsHost && GameDataManager.Instance.GetPlayerList().Count > 1)
        {
            AfterHostQuitClientRPC();
            isTryQuit = true;
            return false;
        }
        return true;
    }

    private void CheckTryQuit(ulong clientId)
    {
        if(isTryQuit)
        {
            Application.Quit();
        }
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void AfterHostQuitClientRPC()
    {
        if (IsServer) return;
        DisconnectToMenu();
    }

    private void DisconnectToMenu()
    {
        GameObject networkManager = NetworkManager.Singleton.gameObject;
        NetworkManager.Singleton.Shutdown();
        Destroy(networkManager);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    }

}
