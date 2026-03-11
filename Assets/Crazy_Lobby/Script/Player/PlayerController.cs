using System;
using System.Collections.Generic;
using Crazy_Lobby.Player;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

public struct NetworkInputData : INetworkInput
{
    public Vector2 Movement;
    public float CameraYaw;
}
[RequireComponent(typeof(NetworkCharacterController))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerController : NetworkBehaviour , INetworkRunnerCallbacks
{
    private NetworkCharacterController _ncc;
    private CharacterAnimation _characterAnimation;
    private Vector2 _localMoveInput; 

    private void Awake()
    {
        _ncc = GetComponent<NetworkCharacterController>();
        _characterAnimation = new CharacterAnimation(GetComponentInChildren<Animator>());
    }
  
    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            Runner.AddCallbacks(this);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority)
        {
            Runner.RemoveCallbacks(this);
        }
    }

    public void OnMove(InputValue value)
    {
        _localMoveInput = value.Get<Vector2>();
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            Quaternion cameraRotation = Quaternion.Euler(0, data.CameraYaw, 0);
            
            Vector3 moveDirection = cameraRotation * new Vector3(data.Movement.x, 0, data.Movement.y);

            _ncc.Move(moveDirection.normalized * Runner.DeltaTime * 5f);

            if (moveDirection.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDirection), Runner.DeltaTime * 10f);
                _characterAnimation.UpdateMoveAnimation(moveDirection);
            }        
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();
        data.Movement = _localMoveInput;

        if (UnityEngine.Camera.main != null)
        {
            data.CameraYaw = UnityEngine.Camera.main.transform.eulerAngles.y;
        }
        
        input.Set(data); 
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        throw new NotImplementedException();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        throw new NotImplementedException();
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        throw new NotImplementedException();
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        throw new NotImplementedException();
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        throw new NotImplementedException();
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        throw new NotImplementedException();
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        throw new NotImplementedException();
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        throw new NotImplementedException();
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        throw new NotImplementedException();
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }
}