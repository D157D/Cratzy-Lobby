using System.Linq;
using Fusion;
using UnityEngine;
using Crazy_Lobby.Enemy;
using UnityEngine.SceneManagement;

public class SpawnPlayer : NetworkBehaviour, IPlayerJoined
{
    public NetworkPrefabRef _player;
    public Transform[] spawnPoints;
    
    [Header("Enemy Spawning")]
    // 👉 Đổi thành mảng để bạn có thể gắn bao nhiêu vị trí tùy thích
    public Transform[] enemySpawnPoints; 
    public NetworkPrefabRef _enemy;
    
    private bool _hasFilledBots = false;

    public override void Spawned()
    {
        if (!Runner.IsServer) return;

        // Sinh ra người chơi hiện tại
        foreach (var player in Runner.ActivePlayers)
        {
            if (Runner.GetPlayerObject(player) == null)
            {
                SpawnPlayerCharacter(player);
            }
        }

        // Sinh quái (Bot) tại các điểm đã thiết lập
        if (!_hasFilledBots)
        {
            SpawnAllBots();
            _hasFilledBots = true;
        }
    }

    private void SpawnAllBots()
    {
        // Kiểm tra xem mảng có điểm spawn nào không
        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
        {
            Debug.LogWarning("[SpawnPlayer] Mảng enemySpawnPoints đang trống! Không có quái nào được sinh ra.");
            return;
        }

        Debug.Log($"[SpawnPlayer] Sẽ sinh ra {enemySpawnPoints.Length} quái vật tại các điểm đã đánh dấu.");

        // Duyệt qua từng điểm trong mảng
        foreach (var spawnPoint in enemySpawnPoints)
        {
            if (spawnPoint != null)
            {
                // Gọi hàm sinh quái và truyền vị trí + hướng xoay của điểm đó vào
                SpawnBot(spawnPoint.position, spawnPoint.rotation);
            }
        }
    }

    private void SpawnBot(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        // Random ngoại hình cho bot
        CharacterType randomChar = (CharacterType)Random.Range(0, System.Enum.GetValues(typeof(CharacterType)).Length);

        if (_enemy.IsValid) 
        {
            Runner.Spawn(_enemy, spawnPosition, spawnRotation, onBeforeSpawned: (runner, obj) => {
                if (obj.TryGetComponent<EnemyCharacterHandler>(out var charHandler))
                {
                    charHandler.CurrentCharacter = randomChar;
                }
            });
        } 
        else 
        {
            Debug.LogWarning("[SpawnPlayer] _enemy prefab chưa được gán. Không thể sinh quái.");
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

        Quaternion spawnRotation = spawnPoints[index].rotation;
        
        if (SceneManager.GetActiveScene().name == "Ending")
        {
            spawnRotation *= Quaternion.Euler(0, 180, 0); // Xoay ngược lại 180 độ quanh trục Y
        }

        var obj = Runner.Spawn(_player, spawnPosition, spawnRotation, player);
        Runner.SetPlayerObject(player, obj);
    }
}