using System.Linq;
using Fusion;
using UnityEngine;
using Crazy_Lobby.Enemy;
public class SpawnPlayer : NetworkBehaviour, IPlayerJoined
{
    public NetworkPrefabRef _player;
    public Transform[] spawnPoints;
    public Transform _enemySpawn;
    public NetworkPrefabRef _enemy;
    private bool _hasFilledBots = false;

    private const int MAX_PLAYERS = 10;

    public override void Spawned()
    {
        if (!Runner.IsServer) return;

        // Spawn existing players
        foreach (var player in Runner.ActivePlayers)
        {
            if (Runner.GetPlayerObject(player) == null)
            {
                SpawnPlayerCharacter(player);
            }
        }

        // Fill the rest with bots
        if (!_hasFilledBots)
        {
            FillWithBots();
            _hasFilledBots = true;
        }
    }

    private void FillWithBots()
    {
        int playerCount = Runner.ActivePlayers.Count();
        int botsToSpawn = MAX_PLAYERS - playerCount;

        Debug.Log($"[SpawnPlayer] Player count: {playerCount}. Spawning {botsToSpawn} bots to fill to {MAX_PLAYERS}.");

        for (int i = 0; i < botsToSpawn; i++)
        {
            SpawnBot();
        }
    }

    private void SpawnBot()
    {
        Vector3 spawnPosition = Vector3.zero;
        if (_enemySpawn != null)
        {
            spawnPosition = _enemySpawn.position + new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
        }
        else
        {
            Debug.LogWarning("[SpawnPlayer] _enemySpawn is null. Spawning bot at Vector3.zero.");
        }
        
        CharacterType randomChar = (CharacterType)Random.Range(0, System.Enum.GetValues(typeof(CharacterType)).Length);

        if (_enemy.IsValid) // Check if NetworkPrefabRef is valid (not null/empty)
        {
            Runner.Spawn(_enemy, spawnPosition, Quaternion.identity, onBeforeSpawned: (runner, obj) => {
                if (obj.TryGetComponent<EnemyCharacterHandler>(out var charHandler))
                {
                    charHandler.CurrentCharacter = randomChar;
                }
            });
        } else {
            Debug.LogWarning("[SpawnPlayer] _enemy prefab is not assigned. Cannot spawn bot.");
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