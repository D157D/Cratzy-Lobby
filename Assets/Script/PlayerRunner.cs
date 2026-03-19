using UnityEngine;
using Fusion;

public class PlayerRunner : SimulationBehaviour, IPlayerJoined
{
    [SerializeField]
    private NetworkObject playerPrefab; // dùng NetworkObject thay vì GameObject

    public void PlayerJoined(PlayerRef player)
    {
        // Spawn cho mỗi player khi join
        Runner.Spawn(playerPrefab, 
                     new Vector3(0, 1, 0), 
                     Quaternion.identity, 
                     player);
    }
}