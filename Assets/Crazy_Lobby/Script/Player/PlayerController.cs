using System;
using System.Collections.Generic;
using Crazy_Lobby.Player;
using Crazy_Lobby.Player.Components;
using Crazy_Lobby.UI;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;
public struct NetworkInputData : INetworkInput
{
    public Vector2 Movement;
    public float CameraYaw;
    public NetworkBool Jump;
}
[RequireComponent(typeof(NetworkCharacterController))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerController : NetworkBehaviour , INetworkRunnerCallbacks
{
    [Header("Movement Settings")]
    public float jumpForce = 10f;
    public float maxSpeed = 10f; 
    public float acceleration = 100f; 
    public float braking = 100f; 

    [Header("Interaction Settings")]
    public LayerMask platformLayer; 

    private NetworkCharacterController _ncc;
    private CharacterAnimation _characterAnimation;
    private PlayerMovement _playerMovement;
    private PlayerInteraction _playerInteraction;

    private Vector2 _localMoveInput; 
    private bool _jumpPressed;

    public NetworkId CurrentTargetId { get; internal set; }
    public bool IsDead { get; internal set; }
     public static readonly List<PlayerController> ActivePlayers = new List<PlayerController>();

    private void Awake()
    {
        _ncc = GetComponent<NetworkCharacterController>();
        _characterAnimation = new CharacterAnimation(GetComponentInChildren<Animator>());
    }
  
    public override void Spawned()
    {
        // Initialize player components
        _playerMovement = new PlayerMovement(_ncc, _characterAnimation, transform, Runner, jumpForce, maxSpeed, acceleration, braking);
        _playerInteraction = new PlayerInteraction(Object, transform, platformLayer);

        if (HasInputAuthority)
        {
            Runner.AddCallbacks(this);

            // Tắt và bật lại PlayerInput để ép Unity kết nối lại với thiết bị (bàn phím/chuột) của Client
            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = false;
                playerInput.enabled = true;
            }
            
            // Tìm script Menu (kể cả khi GameObject đang ẩn) và tắt _CrazyLobby
            Menu menu = FindObjectOfType<Menu>(true);
            if (menu != null && menu._CrazyLobby != null)
            {
                menu._CrazyLobby.SetActive(false);
            }
        }
            else
            {
                // Tắt PlayerInput trên các nhân vật của người chơi khác (Proxy)
                // để tránh việc chúng giành quyền điều khiển bàn phím/chuột của máy bạn
                PlayerInput playerInput = GetComponent<PlayerInput>();
                if (playerInput != null)
                {
                    playerInput.enabled = false;
                }
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

    public void OnJump(InputValue value)
    {
        if (value.isPressed) _jumpPressed = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            _playerMovement.ProcessInput(data);
        }

        _playerInteraction.CheckPlatformBeneath();
    }

    public override void Render()
    {
        _playerMovement.UpdateAnimations();
    }
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();
        data.Movement = _localMoveInput;

        if (UnityEngine.Camera.main != null)
        {
            var customCamera = UnityEngine.Camera.main.GetComponent<Camera>();
            if (customCamera != null)
            {
                data.CameraYaw = customCamera.CurrentYaw;
            }
            else
            {
                data.CameraYaw = UnityEngine.Camera.main.transform.eulerAngles.y;
            }
        }

        data.Jump = _jumpPressed;

        input.Set(data); 
        _jumpPressed = false;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_PickUpItem(string itemName, int amount)
    {
        Debug.Log($"Bạn vừa nhặt được: {amount} {itemName}");
        
        if(ItemUIManager.Instance != null)
        {
            ItemUIManager.Instance.ShowItemPickup(itemName, amount);
        }
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        PlayerPrefs.Save();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }
}