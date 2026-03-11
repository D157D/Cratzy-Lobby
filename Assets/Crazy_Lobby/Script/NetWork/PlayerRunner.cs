using Fusion;
using UnityEngine;

public class PlayerRunner : NetworkBehaviour, IPlayerJoined
{
    public NetworkObject _player;
    public void PlayerJoined(PlayerRef player)
    {
        if(player == Runner.LocalPlayer)
        {
            Runner.Spawn(_player, new Vector3(11, 0, 11), Quaternion.identity, player);
        }
    }
}