using Fusion;
using UnityEngine;
using System;

public class PlayerCharacterHandler : NetworkBehaviour
{
    [Networked]
    public CharacterType CurrentCharacter { get; set; } = CharacterType.Mage;

    private CharacterDatabase _database;
    private ChangeDetector _changeDetector;
    private GameObject _spawnedModel;

    public static event Action<PlayerCharacterHandler> OnAnyCharacterChanged;
    public static event Action<PlayerCharacterHandler> OnLocalPlayerSpawned;

    public override void Spawned()
    {
        _database = FindAnyObjectByType<CharacterDatabase>();
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (Object.HasInputAuthority)
        {
            CharacterType savedType = CharacterSaveManager.Load(CharacterType.Mage);
            Debug.Log($"[PCH] Local player – Nhân vật đã lưu: {savedType}");

            if (savedType != CurrentCharacter)
            {
                RPC_RequestChange(savedType);
            }
        }

        SpawnCharacterPrefab(CurrentCharacter);

        if (Object.HasInputAuthority)
            OnLocalPlayerSpawned?.Invoke(this);
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(CurrentCharacter):
                    Debug.Log($"[PCH] ChangeDetected → CurrentCharacter đổi thành: {CurrentCharacter}");
                    SpawnCharacterPrefab(CurrentCharacter);
                    OnAnyCharacterChanged?.Invoke(this);
                    break;
            }
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_spawnedModel != null)
            Destroy(_spawnedModel);
    }

    public void RequestChangeCharacter(CharacterType type)
    {
        Debug.Log($"[PCH] RequestChangeCharacter({type}) – HasInput: {Object.HasInputAuthority}, Current: {CurrentCharacter}");
        if (!Object.HasInputAuthority) return;
        if (CurrentCharacter == type)
        {
            Debug.Log($"[PCH] Đã là {type} rồi, bỏ qua.");
            return;
        }
        Debug.Log($"[PCH] Gửi RPC đổi sang {type}");
        RPC_RequestChange(type);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestChange(CharacterType type)
    {
        Debug.Log($"[PCH] RPC_RequestChange nhận trên Server: {type}");
        CurrentCharacter = type;
    }


    private void SpawnCharacterPrefab(CharacterType type)
    {
        Debug.Log($"[PCH] SpawnCharacterPrefab({type}) bắt đầu");

        if (_spawnedModel != null)
        {
            Debug.Log($"[PCH] Xóa model cũ: {_spawnedModel.name}");
            Destroy(_spawnedModel);
            _spawnedModel = null;
        }

        if (_database == null)
        {
            Debug.LogError("[PCH] Database NULL!");
            return;
        }

        var entry = _database.GetEntry(type);
        if (entry.ModelPrefab == null)
        {
            Debug.LogWarning($"[PCH] ModelPrefab NULL cho {type}!");
            return;
        }

        Debug.Log($"[PCH] Instantiate prefab: {entry.ModelPrefab.name} cho {type}");
        _spawnedModel = Instantiate(entry.ModelPrefab, transform);
        _spawnedModel.SetActive(true);
        _spawnedModel.transform.localPosition = Vector3.zero;
        _spawnedModel.transform.localRotation = Quaternion.identity;
        Debug.Log($"[PCH] Spawn thành công! Model: {_spawnedModel.name}, Active: {_spawnedModel.activeSelf}, Parent: {_spawnedModel.transform.parent.name}");
    }
}