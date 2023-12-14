using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class HostDisconnectManager : NetworkBehaviour
{

    private bool rpcSended = false;

    private void Awake()
    {
        Application.wantsToQuit += WantsToQuit;
    }

    private bool WantsToQuit()
    {
        if (IsHost)
        {
            AfterHostQuitClientRPC();
            return rpcSended;
        }
        return true;
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void AfterHostQuitClientRPC()
    {
        NotifyServerRPC();
        DisconnectToMenu();
    }

    [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
    private void NotifyServerRPC()
    {
        rpcSended = true;
    }

    private void DisconnectToMenu()
    {
        GameObject networkManager = NetworkManager.Singleton.gameObject;
        NetworkManager.Singleton.Shutdown();
        Destroy(networkManager);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    }

}
