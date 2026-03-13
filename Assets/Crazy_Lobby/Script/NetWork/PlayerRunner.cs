using System.Linq;
using Fusion;
using UnityEngine;

public class PlayerRunner : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    public NetworkPrefabRef _player;
    public NetworkPrefabRef _enemyPrefab;
    public void PlayerJoined(PlayerRef player)
    {
        if (Runner.IsServer)
        {
            Vector3 spawnPosition = new Vector3(11, 0, 11) + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
            Runner.Spawn(_player, spawnPosition, Quaternion.identity, player);

            // Spawn Enemy
            if(Runner.ActivePlayers.Count() >= 1)
            {
                Vector3 enemySpawnPosition = new Vector3(11, 0, 11) + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
                Runner.Spawn(_enemyPrefab, enemySpawnPosition);
            }
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        if(Runner.IsServer)
        {
            Runner.Despawn(Runner.GetPlayerObject(player));
        }
    }
}