using System;
using System.Collections.Generic;
using Crazy_Lobby.Player;
using Crazy_Lobby.Player.Components;
using Crazy_Lobby.UI;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public struct NetworkInputData : INetworkInput
{
    public Vector2 Movement;
    public float CameraYaw;
    public NetworkBool Jump;
    public NetworkBool UseItem;
    public NetworkBool Magic;
}

[RequireComponent(typeof(NetworkCharacterController))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(PlayerCombat))] 
[RequireComponent(typeof(PlayerAudio))]  
public class PlayerController : NetworkBehaviour , INetworkRunnerCallbacks
{
    [Header("Movement Settings")]
    private float jumpForce = 10f;
    private float maxSpeed = 10f; 
    private float acceleration = 100f; 
    private float braking = 100f; 

    [Header("Interaction Settings")]
    public LayerMask platformLayer; 

    private NetworkCharacterController _ncc;
    private CharacterAnimation _characterAnimation;
    private PlayerMovement _playerMovement;
    private PlayerInteraction _playerInteraction;
    private PlayerHealth _playerHealth;
    private PlayerCombat _playerCombat;
    private PlayerAudio _playerAudio;

    private Vector2 _localMoveInput; 
    private bool _jumpPressed;
    private bool _useItemPressed;
    private bool _magicPressed;

    public bool IsInLobby => SceneManager.GetActiveScene().name == "Login_Crazy"; 
    public NetworkId CurrentTargetId { get; internal set; }
    public bool IsDead { get; internal set; }
    public static readonly List<PlayerController> ActivePlayers = new List<PlayerController>();

    private void Awake()
    {
        _ncc = GetComponent<NetworkCharacterController>();
        _playerHealth = GetComponent<PlayerHealth>();
        _playerCombat = GetComponent<PlayerCombat>();
        _playerAudio = GetComponent<PlayerAudio>();
        _characterAnimation = new CharacterAnimation(GetComponentInChildren<Animator>());
    }
  
    public override void Spawned()
    {
        ActivePlayers.Add(this);

        if (_playerHealth != null) _playerHealth.OnDeath += HandleDeath;
        HandleInitialDeathState();

        _playerMovement = new PlayerMovement(_ncc, _characterAnimation, transform, Runner, jumpForce, maxSpeed, acceleration, braking);
        _playerInteraction = new PlayerInteraction(Object, transform, platformLayer);
        
        _playerCombat.Initialize(this, _playerAudio);

        if (HasInputAuthority)
        {
            Runner.AddCallbacks(this);

            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = false;
                playerInput.enabled = true;
            }
            
            Menu menu = FindObjectOfType<Menu>(true);
            if (menu != null && menu._CrazyLobby != null)
            {
                menu._CrazyLobby.SetActive(false);
            }
        }
        else
        {
            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput != null) playerInput.enabled = false;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        ActivePlayers.Remove(this);
        if (_playerHealth != null) _playerHealth.OnDeath -= HandleDeath;
        if (HasInputAuthority) Runner.RemoveCallbacks(this);
    }

    public void OnMove(InputValue value) { _localMoveInput = value.Get<Vector2>(); }
    public void OnJump(InputValue value) { if (value.isPressed) _jumpPressed = true; }
    public void OnUseItem(InputValue value) { if (value.isPressed) _useItemPressed = true; }
    public void OnMagic(InputValue value) { if (value.isPressed) _magicPressed = true; }


    public override void FixedUpdateNetwork()
    {
        if (IsDead) return;

        if (GetInput(out NetworkInputData data))
        {
            if (_playerCombat != null && _playerCombat.IsStunned)
            {
                data.Movement = Vector2.zero;
                data.Jump = false;
                data.UseItem = false;
                data.Magic = false;
            }
            else 
            {
                if (data.Jump && _ncc.Grounded) 
                {
                    if (_playerAudio != null) _playerAudio.PlayJump(Runner.IsForward);
                }
            }

            _playerMovement.ProcessInput(data);

            if (_playerCombat != null) 
            {
                _playerCombat.ProcessCombatInput(data, IsInLobby, _characterAnimation);
            }
        }

        _playerInteraction.CheckPlatformBeneath();
    }

    public override void Render()
    {
        if (IsDead) return;

        if (_characterAnimation != null && _characterAnimation.GetAnimator() == null)
        {
            var animator = GetComponentInChildren<Animator>();
            if (animator != null) _characterAnimation.SetAnimator(animator);
        }

        _playerMovement.UpdateAnimations();
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();
        data.Movement = _localMoveInput;

        if (Camera.main != null)
        {
            var customCamera = Camera.main.GetComponent<CameraP>();
            if (customCamera != null) data.CameraYaw = customCamera.CurrentYaw;
            else data.CameraYaw = Camera.main.transform.eulerAngles.y;
        }

        data.Jump = _jumpPressed;
        data.UseItem = _useItemPressed;
        data.Magic = _magicPressed;

        input.Set(data); 
        _jumpPressed = false;
        _useItemPressed = false;
        _magicPressed = false;
    }

    private void HandleDeath()
    {
        if (IsDead) return;

        IsDead = true;
        _ncc.enabled = false;   
        
        // Gọi âm thanh chết
        if (_playerAudio != null) _playerAudio.PlayDeath();

        if (_characterAnimation != null)
        {
            if (_characterAnimation.GetAnimator() == null)
                _characterAnimation.SetAnimator(GetComponentInChildren<Animator>());
                
            _characterAnimation.TriggerDeath();
        }

        if (HasInputAuthority)
        {
            var cam = Camera.main.GetComponent<CameraP>();
            if (cam != null) cam.OnPlayerDied();
        }
    }

    private void HandleInitialDeathState()
    {
        if (_playerHealth != null && _playerHealth.IsDead) HandleDeath();
    }

    // Các Callbacks trống của Fusion
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { PlayerPrefs.Save(); }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
}