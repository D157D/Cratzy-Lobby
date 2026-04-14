using UnityEngine;
using Fusion;

public class Playerrunner : SimulationBehaviour, IPlayerJoined
{
    [SerializeField] private GameObject playerPrefab;

    public void PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer)
        {
            Vector3 spawnPos = new Vector3(0, 1, 0);

            Runner.Spawn(
                playerPrefab,
                spawnPos,
                Quaternion.identity,
                player // 🔥 QUAN TRỌNG
            );
        }
    }
}