using Fusion;
using UnityEngine;
using UnityEngine.XR;

public class PlayerCharacterHandler : NetworkBehaviour
{
    [Networked] public CharacterType CurrentCharacter {get; set;}
    private CharacterDatabase _database;

    public override void Spawned()
    {
        _database = FindObjectOfType<CharacterDatabase>();

        if(Object.HasInputAuthority)
        {

            RequestChangeCharacter(CharacterType.Mage);
        }
    }

    public void RequestChangeCharacter(CharacterType type) 
    {
        if(Object.HasInputAuthority)
        {
            RPC_RequestChange(type);
        }
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestChange(CharacterType type)
    {
        ChangeCharacter(type);
    }

    private void ChangeCharacter(CharacterType type)
    {
        if(!Object.HasInputAuthority) return;

        var prefab = _database.GetPrefab(type);

        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        Runner.Despawn(Object);

        var newplayer = Runner.Spawn(prefab, pos, rot, Object.InputAuthority);

        var handler = newplayer.GetComponent<PlayerCharacterHandler>();
        handler.CurrentCharacter = type;
    }
}