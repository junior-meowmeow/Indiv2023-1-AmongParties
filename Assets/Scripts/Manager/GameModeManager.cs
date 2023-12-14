using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class GameModeManager : NetworkBehaviour
{

    protected bool isPlaying = false;

    public abstract GameMode GetGameMode();

    public virtual void UpdateGameMode()
    {
        return;
    }

    public virtual void StartGameServer()
    {
        isPlaying = true;
        StartGameClientRPC();
    }

    [ClientRpc]
    private void StartGameClientRPC()
    {
        isPlaying = true;
    }

    protected void AfterGameEndClient()
    {
        isPlaying = false;
        GameplayManager.Instance.GameModeEnd();
    }

    public virtual Vector3 GetSpawnPosition()
    {
        return GameDataManager.Instance.GetLobbySpawnPosition();
    }

    public virtual bool GetObject(PickableObject obj, string locationName)
    {
        return false;
    }

    protected void WarpAllPlayerToLobby()
    {
        foreach (PlayerData ps in GameDataManager.Instance.GetPlayerList())
        {
            ps.player.WarpClientRPC(GameDataManager.Instance.GetLobbySpawnPosition(), isDropItem: true);
        }
    }

    [ClientRpc]
    protected void PlaySoundClientRPC(string name, Vector3 position)
    {
        SoundManager.PlayNew(name, position);
    }

    [ClientRpc]
    protected void PlayParticleClientRPC(Vector3 position)
    {
        ObjectPool.Instance.SpawnObject("Score Particle", position, Quaternion.identity);
    }

    public virtual void RequestGameModeUpdate(ClientRpcParams clientRpcParams = default)
    {
        SendBaseUpdateClientRPC(isPlaying, clientRpcParams);
    }

    [ClientRpc]
    private void SendBaseUpdateClientRPC(bool isPlaying, ClientRpcParams clientRpcParams = default)
    {
        this.isPlaying = isPlaying;
    }

}
