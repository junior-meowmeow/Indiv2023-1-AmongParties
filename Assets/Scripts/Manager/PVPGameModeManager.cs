using UnityEngine;
using Unity.Netcode;

public class PVPGameModeManager : GameModeManager
{

    [SerializeField] private Transform[] gameplayLocations;
    [SerializeField] private int spawnCount = 0;

    public override GameMode GetGameMode()
    {
        return GameMode.PVP;
    }

    public override void StartGameServer()
    {
        base.StartGameServer();
        ObjectPool.Instance.InitPool();
        StartGameClientRPC();
    }

    [ClientRpc]
    private void StartGameClientRPC()
    {
        ResetValueBeforeGame();
        SoundManager.PlayMusic("battle");
        UpdateUI();
        if (IsServer)
        {
            foreach (PlayerData ps in GameDataManager.Instance.GetPlayerList())
            {
                ps.player.WarpClientRPC(GetSpawnPosition(), isDropItem: true);
            }
        }
        EndGameClient();
    }

    private void ResetValueBeforeGame()
    {
        GameplayManager.Instance.SetWinText("YOU WIN", false);
        GameplayManager.Instance.SetLoseText("YOU LOSE", false);
        spawnCount = Random.Range(0, gameplayLocations.Length);
    }

    private void UpdateUI()
    {
        GameplayManager.Instance.SetMainObjectiveText("Be the Last One Alive.");
    }

    public override void UpdateGameMode()
    {
        base.UpdateGameMode();
        return;
    }

    public override Vector3 GetSpawnPosition()
    {
        spawnCount %= gameplayLocations.Length;
        return gameplayLocations[spawnCount++].position + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
    }

    private void EndGameClient()
    {
        Debug.Log("PVP ENDED");
        AfterGameEndClient();
    }

}
