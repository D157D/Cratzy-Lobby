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
        private GameObject _spawnedModel;

        public event Action<GameObject> OnModelChanged;

        public override void Spawned()
        {
            _database = FindFirstObjectByType<CharacterDatabase>();
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

            // Bots have their character set by the Server during spawn or via logic.
            // We just ensure the model is spawned.
            SpawnCharacterPrefab(CurrentCharacter);
        }

        public override void Render()
        {
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(CurrentCharacter):
                        Debug.Log($"[ECH] ChangeDetected -> CurrentCharacter đổi thành: {CurrentCharacter}");
                        SpawnCharacterPrefab(CurrentCharacter);
                        break;
                }
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (_spawnedModel != null)
                Destroy(_spawnedModel);
        }

        private void SpawnCharacterPrefab(CharacterType type)
        {
            if (_spawnedModel != null)
            {
                Destroy(_spawnedModel);
                _spawnedModel = null;
            }

            if (_database == null)
            {
                _database = FindFirstObjectByType<CharacterDatabase>();
                if (_database == null)
                {
                    Debug.LogError("[ECH] Database NULL!");
                    return;
                }
            }

            var entry = _database.GetEntry(type);
            if (entry.ModelPrefab == null)
            {
                Debug.LogWarning($"[ECH] ModelPrefab NULL cho {type}!");
                return;
            }

            _spawnedModel = Instantiate(entry.ModelPrefab, transform);
            _spawnedModel.SetActive(true);
            _spawnedModel.transform.localPosition = Vector3.zero;
            _spawnedModel.transform.localRotation = Quaternion.identity;
            
            OnModelChanged?.Invoke(_spawnedModel);
            
            Debug.Log($"[ECH] Spawn thành công! Model: {_spawnedModel.name} cho {type}");
        }
    }
}