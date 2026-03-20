using Fusion;
using UnityEngine;

public class SpawnPlayer : NetworkBehaviour, IPlayerJoined
{
    public NetworkPrefabRef _player;
    public Transform[] spawnPoints;

    public override void Spawned()
    {
        if (!Runner.IsServer) return;

        // Khi Scene Game load xong, lập tức tạo nhân vật cho tất cả người chơi đã ở sẵn trong phòng (Lobby)
        foreach (var player in Runner.ActivePlayers)
        {
            if (Runner.GetPlayerObject(player) == null)
            {
                SpawnPlayerCharacter(player);
            }
        }
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (!Runner.IsServer) return;

        // Dành cho trường hợp có người chơi tham gia muộn (Vào thẳng Scene Game sau khi phòng đã bắt đầu)
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