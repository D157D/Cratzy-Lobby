using Fusion;
using UnityEngine;
using System;

namespace Crazy_Lobby.Enemy
{
    public class EnemyCharacterHandler : NetworkBehaviour
    {
        [Networked]
        public CharacterType CurrentCharacter { get; set; } = CharacterType.Barbarian;

        private CharacterDatabase _database;
        private ChangeDetector _changeDetector;
        
        public GameObject SpawnedModel { get; private set; } 

        public event Action<GameObject> OnModelChanged;

        private CharacterType _lastSpawnedType = (CharacterType)(-1);

        public override void Spawned()
        {
            _database = FindFirstObjectByType<CharacterDatabase>();
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

            TrySpawnCharacter(CurrentCharacter);
        }

        public override void Render()
        {
            // Theo dõi sự thay đổi biến từ Server
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(CurrentCharacter):
                        TrySpawnCharacter(CurrentCharacter);
                        break;
                }
            }

            if (SpawnedModel == null && _database != null)
            {
                TrySpawnCharacter(CurrentCharacter);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (SpawnedModel != null)
                Destroy(SpawnedModel);
        }

        private void TrySpawnCharacter(CharacterType type)
        {
            if (_database == null)
            {
                _database = FindFirstObjectByType<CharacterDatabase>();
                if (_database == null) return; // Nếu database vẫn chưa kịp load, bỏ qua đợi frame sau
            }

            var entry = _database.GetEntry(type);
            
            if (entry.ModelPrefab == null)
            {
                Debug.LogWarning($"[ECH] KHÔNG TÌM THẤY Prefab cho {type}! Đang tự động lấy con đầu tiên trong Database để thay thế...");
                
                var allEntries = _database.GetAllEntries();
                if (allEntries != null && allEntries.Length > 0 && allEntries[0].ModelPrefab != null)
                {
                    entry = allEntries[0]; 
                }
                else
                {
                    Debug.LogError("[ECH] Database trống trơn hoặc con đầu tiên cũng bị NULL! Chịu thua!");
                    return; 
                }
            }

            if (SpawnedModel != null && _lastSpawnedType == type) return;

            if (SpawnedModel != null)
            {
                Destroy(SpawnedModel);
                SpawnedModel = null;
            }

            SpawnedModel = Instantiate(entry.ModelPrefab, transform);
            SpawnedModel.SetActive(true);
            SpawnedModel.transform.localPosition = Vector3.zero;
            SpawnedModel.transform.localRotation = Quaternion.identity;
            
            _lastSpawnedType = type;
            
            OnModelChanged?.Invoke(SpawnedModel);
        }
    }
}