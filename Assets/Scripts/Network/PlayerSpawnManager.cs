using Unity.Netcode;

public class PlayerSpawnManager : NetworkBehaviour
{
    private void Start()
    {
        NetworkManager.ConnectionApprovalCallback = ConnectionApprovalCallback;
    }

    private void ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        /* you can use this method in your project to customize one of more aspects of the player
         * (I.E: its start position, its character) and to perform additional validation checks. */
        response.Approved = true;
        response.CreatePlayerObject = true;
        response.Position = GameDataManager.Instance.GetPlayerSpawnPosition();
    }

    /*
    Vector3 GetPlayerSpawnPosition()
    {
        return new Vector3(Random.Range(-3, 3), 0, Random.Range(-3, 3));
    }
    */
}
