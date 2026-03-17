using Fusion;
using UnityEngine;

public class SpawnPlayer : NetworkBehaviour, IPlayerJoined
{
    public NetworkPrefabRef _player;
    public Transform[] spawnPoints;

    public void PlayerJoined(PlayerRef player)
    {
        if (!Runner.IsServer) return;

        int index = player.RawEncoded % spawnPoints.Length;
        Vector3 spawnPosition = spawnPoints[index].position;

        var obj = Runner.Spawn(_player, spawnPosition, Quaternion.identity, player);
        Runner.SetPlayerObject(player, obj);
    }
}