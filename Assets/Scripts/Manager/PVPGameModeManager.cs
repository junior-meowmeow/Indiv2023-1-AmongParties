using UnityEngine;
using Unity.Netcode;

public class PVPGameModeManager : GameModeManager
{

    [SerializeField] private Transform[] gameplayLocations;
    [SerializeField] private int spawnCount = 0;
    [SerializeField] private Transform[] itemSpawnLocations;
    [SerializeField] private DangerZone dangerZone;
    [SerializeField] private int round;

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
        dangerZone.StartMove(1f);
        UpdateUI();
        if (IsServer)
        {
            foreach (PlayerData ps in GameDataManager.Instance.GetPlayerList())
            {
                ps.player.WarpClientRPC(GetSpawnPosition(), isDropItem: true);
            }
        }
    }

    private void ResetValueBeforeGame()
    {
        GameplayManager.Instance.SetWinText("YOU WIN", false);
        GameplayManager.Instance.SetLoseText("YOU LOSE", false);
        spawnCount = Random.Range(0, gameplayLocations.Length);
        dangerZone.Reset();
    }

    private void UpdateUI()
    {
        GameplayManager.Instance.SetMainObjectiveText("Be the Last One Alive.");
    }

    public override void UpdateGameMode()
    {
        base.UpdateGameMode();

        if(!IsServer) return;

        if(GameDataManager.Instance.GetDeadPlayerCount() >= GameDataManager.Instance.GetPlayerList().Count - 1)
        {
            EndGameClientRPC();
        }
    }

    public override Vector3 GetSpawnPosition()
    {
        spawnCount %= gameplayLocations.Length;
        return gameplayLocations[spawnCount++].position + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
    }

    [ClientRpc]
    private void EndGameClientRPC()
    {
        EndGameClient();
    }

    public override int GetCurrentRound()
    {
        return round;
    }

    private void EndGameClient()
    {
        Debug.Log("PVP ENDED");
        dangerZone.StopMove();

        if(GameDataManager.Instance.localPlayer.GetPlayerData().player.CheckIsDead())
        {
            string winnerName = GameDataManager.Instance.GetPlayerList()[GameDataManager.Instance.spectatingPlayerIndex].playerName;
            GameplayManager.Instance.SetLoseText(winnerName + " WIN", true);
        }
        else
        {
            GameplayManager.Instance.SetWinText("YOU WIN", true);
        }

        if (IsServer)
        {
            WarpAllPlayerToLobbyServer();
            ReviveAllPlayerServer();
        }

        AfterGameEndClient();
    }

}
