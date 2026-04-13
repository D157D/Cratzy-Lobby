using UnityEngine;
using Fusion;
using System.Collections.Generic;

[System.Serializable]
public struct CharacterEntry
{
    public CharacterType Type;
    public GameObject ModelPrefab;
    public Sprite Icon;
    public string DisplayName;
}

public class CharacterDatabase : MonoBehaviour
{
    public CharacterEntry[] character;
    private Dictionary<CharacterType, CharacterEntry> _map;

    private void Awake()
    {
        _map = new Dictionary<CharacterType, CharacterEntry>();

        foreach (var c in character)
        {
            _map[c.Type] = c;
        }
    }

    public CharacterEntry GetEntry(CharacterType type)
    {
        if (_map != null && _map.TryGetValue(type, out var entry))
            return entry;

        Debug.LogWarning($"[CharacterDatabase] Không tìm thấy entry cho {type}");
        return default;
    }

    public CharacterEntry[] GetAllEntries()
    {
        return character;
    }

    public int Count => character != null ? character.Length : 0;
}
