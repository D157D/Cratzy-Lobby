using Fusion;
using UnityEngine;

public class SpawnPlayer : NetworkBehaviour, IPlayerJoined
{
    public NetworkPrefabRef _player;
    public Transform[] spawnPoints;
    public Transform _enemySpawn;
    public NetworkPrefabRef _enemy;
    private bool IsSpawned;

    public override void Spawned()
    {
        if (!Runner.IsServer) return;

        foreach (var player in Runner.ActivePlayers)
        {
            if (Runner.GetPlayerObject(player) == null)
            {
                SpawnPlayerCharacter(player);
                if(!IsSpawned)
                {
                    Runner.Spawn(_enemy, _enemySpawn.position, Quaternion.identity);
                    IsSpawned = true;
                }
            }
        }
    }
    

    public void PlayerJoined(PlayerRef player)
    {
        if (!Runner.IsServer) return;
        
        
        if (Runner.GetPlayerObject(player) == null)
        {
            SpawnPlayerCharacter(player);
        }
    }

    private void SpawnPlayerCharacter(PlayerRef player)
    {
        int index = player.RawEncoded % spawnPoints.Length;
        Vector3 spawnPosition = spawnPoints[index].position;

        var obj = Runner.Spawn(_player, spawnPosition, Quaternion.identity, player);
        Runner.SetPlayerObject(player, obj);
    }
}