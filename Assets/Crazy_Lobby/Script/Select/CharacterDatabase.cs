using UnityEngine;
using UnityEngine.UI;
using Fusion;
using System.Collections.Generic;

[System.Serializable]
public struct CharacterEntry {
	public CharacterType Type;
    public NetworkPrefabRef PlayerPrefab;
}

public class CharacterDatabase  : MonoBehaviour
{
	public CharacterEntry[] character;
    private Dictionary<CharacterType, NetworkPrefabRef> _map;

    private void Awake()
    {
        _map = new Dictionary<CharacterType, NetworkPrefabRef>();

        foreach(var c in character)
        {
            _map[c.Type] = c.PlayerPrefab;
        }
    }

    public NetworkPrefabRef GetPrefab(CharacterType type)
    {
        return _map[type];
    }
}
