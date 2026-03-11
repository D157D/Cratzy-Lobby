using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRunner : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] private NetworkPrefabRef _playerPrefab;

    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    public void PlayerJoined(PlayerRef player)
    {
        if (Runner.IsServer)
        {
            NetworkObject networkPlayerObject = Runner.Spawn(_playerPrefab, new Vector3(11, 2, 13), Quaternion.identity, player);
            _spawnedCharacters.Add(player, networkPlayerObject);
        }
        else if (Runner.GameMode == GameMode.Shared && player == Runner.LocalPlayer)
        {
            NetworkObject networkPlayerObject = Runner.Spawn(_playerPrefab, new Vector3(11, 2, 13), Quaternion.identity, player);
            _spawnedCharacters.Add(player, networkPlayerObject);
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            Runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }
}