using System;
using System.Collections.Generic;
using Crazy_Lobby.Player;
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
    public NetworkBool UseItem;
    public NetworkId TargetId;
}
[RequireComponent(typeof(NetworkCharacterController))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerController : NetworkBehaviour , INetworkRunnerCallbacks
{
    private float jumpForce = 10f;
    public float maxSpeed = 10f; 
    public float acceleration = 100f; 
    public float braking = 100f; 
    private NetworkCharacterController _ncc;
    private CharacterAnimation _characterAnimation;
    private Vector2 _localMoveInput; 
    private bool _jumpPressed;
    private bool _useItemPressed;
    //
    public LayerMask platformLayer; 
    private FragilePlatform currentPlatform;

    [Header("Death & Respawn")]
    public float fallDeathThreshold = -10f;
    public float respawnDelay = 3f;
    [Networked] public NetworkBool IsDead { get; set; }
    [Networked] private TickTimer RespawnTimer { get; set; }

    public NetworkId CurrentTargetId { get; private set; }
    
    public static readonly List<PlayerController> ActivePlayers = new List<PlayerController>();
    private static Menu _cachedMenu;
    private static GameObject[] _cachedCheckpoints;
    private static SpawnPlayer _cachedSpawnPlayer;

    private void Awake()
    {
        _ncc = GetComponent<NetworkCharacterController>();
        _characterAnimation = new CharacterAnimation(GetComponentInChildren<Animator>());
        _ncc.maxSpeed = maxSpeed;
        _ncc.acceleration = acceleration;
        _ncc.braking = braking;
    }

    public override void Spawned()
    {
        ActivePlayers.Add(this);

        if (HasInputAuthority)
        {
            Runner.AddCallbacks(this);

            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = false;
                playerInput.enabled = true;
            }
            
            if (_cachedMenu == null)
            {
                _cachedMenu = FindObjectOfType<Menu>(true);
            }
            
            if (_cachedMenu != null && _cachedMenu._CrazyLobby != null)
            {
                _cachedMenu._CrazyLobby.SetActive(false);
            }
        }
        else
        {
            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = false;
            }
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        ActivePlayers.Remove(this);

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

    public void OnUseItem(InputValue value)
    {
        if (value.isPressed) _useItemPressed = true;
    }

    private void Update()
    {
        if (HasInputAuthority && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            _useItemPressed = true;
        }
    }

    public override void FixedUpdateNetwork()
    {

        if (HasStateAuthority)
        {
            CheckPlatformBeneath();

            if (!IsDead && transform.position.y < fallDeathThreshold)
            {
                Die();
            }

            if (IsDead && RespawnTimer.Expired(Runner))
            {
                Respawn();
            }
        }

        if (IsDead) return;

        if (GetInput(out NetworkInputData data))
        {
            CurrentTargetId = data.TargetId;

            Quaternion cameraRotation = Quaternion.Euler(0, data.CameraYaw, 0);
            
            Vector3 moveDirection = cameraRotation * new Vector3(data.Movement.x, 0, data.Movement.y);

            _ncc.Move(moveDirection);

            if (data.Jump && _ncc.Grounded)
            {
                _ncc.Jump( true, jumpForce );
                _characterAnimation.TriggerJump();
            }
            
            if (data.UseItem)
            {
                // Nếu đang khoá mục tiêu, lập tức xoay mặt nhân vật về phía mục tiêu
                if (CurrentTargetId.IsValid)
                {
                    NetworkObject targetObj = Runner.FindObject(CurrentTargetId);
                    if (targetObj != null)
                    {
                        Vector3 dirToTarget = targetObj.transform.position - transform.position;
                        dirToTarget.y = 0; // Cố định trục Y để nhân vật không bị ngửa ra sau
                        if (dirToTarget != Vector3.zero)
                        {
                            transform.rotation = Quaternion.LookRotation(dirToTarget);
                        }
                    }
                }

                Collider[] hitColliders = Physics.OverlapSphere(transform.position, 2f);
                foreach (var hitCollider in hitColliders)
                {
                    if (hitCollider.TryGetComponent<Crazy_Lobby.Item.Items>(out var nearbyItem))
                    {
                        nearbyItem.Use(this);
                        break; 
                    }
                }
            }

            if (moveDirection.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDirection), Runner.DeltaTime * 10f);
            }
        }
    }

    private void Die()
    {
        IsDead = true;
        RespawnTimer = TickTimer.CreateFromSeconds(Runner, respawnDelay);
        
        _ncc.Velocity = Vector3.zero;
        
        RPC_OnDeath();
    }

    private void Respawn()
    {
        IsDead = false;
        
        Vector3 respawnPos = GetNearestCheckpoint();
        _ncc.Teleport(respawnPos);
        
        RPC_OnRespawn();
    }

    private Vector3 GetNearestCheckpoint()
    {
        Vector3 bestPos = transform.position;
        bestPos.y = 5f; // Điểm rơi mặc định phòng hờ

        if (_cachedCheckpoints == null || _cachedCheckpoints.Length == 0)
        {
            _cachedCheckpoints = GameObject.FindGameObjectsWithTag("Respawn");
        }

        if (_cachedCheckpoints != null && _cachedCheckpoints.Length > 0)
        {
            float closestDist = float.MaxValue;
            foreach (var cp in _cachedCheckpoints)
            {
                if (cp == null) continue; // Bỏ qua nếu checkpoint đã bị hủy khỏi scene
                float dist = Vector3.Distance(transform.position, cp.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    bestPos = cp.transform.position;
                }
            }
        }
        else
        {
            // Fallback: Tìm thông qua script SpawnPlayer (vị trí sinh ngẫu nhiên ở sảnh)
            if (_cachedSpawnPlayer == null) _cachedSpawnPlayer = FindObjectOfType<SpawnPlayer>();

            if (_cachedSpawnPlayer != null && _cachedSpawnPlayer.spawnPoints != null && _cachedSpawnPlayer.spawnPoints.Length > 0)
            {
                bestPos = _cachedSpawnPlayer.spawnPoints[UnityEngine.Random.Range(0, _cachedSpawnPlayer.spawnPoints.Length)].position;
            }
        }

        return bestPos;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_OnDeath()
    {
        SwitchCameraToAnotherPlayer();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_OnRespawn()
    {
        ResetCameraToSelf();
    }

    private void SwitchCameraToAnotherPlayer()
    {
        if (UnityEngine.Camera.main == null) return;
        
        // Gọi script Camera toàn cục để kích hoạt chế độ Spectator
        var customCamera = UnityEngine.Camera.main.GetComponent<global::Camera>();
        if (customCamera != null)
        {
            Debug.Log("[Camera] Nhân vật đã chết, chuyển sang chế độ Spectator.");
            customCamera.OnPlayerDied();
        }
    }

    private void ResetCameraToSelf()
    {
        if (UnityEngine.Camera.main == null) return;
        
        var customCamera = UnityEngine.Camera.main.GetComponent<global::Camera>();
        if (customCamera != null)
        {
            Debug.Log("[Camera] Đã hồi sinh, chuyển lại góc nhìn về nhân vật chính.");
            customCamera.OnPlayerRespawned();
        }
    }

    public override void Render()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (r.enabled == IsDead) r.enabled = !IsDead;
        }

        if (IsDead) return; // Nếu đã chết thì ngừng cập nhật Animation di chuyển

        _characterAnimation.UpdateMoveAnimation(_ncc.Velocity, maxSpeed);
        _characterAnimation.UpdateJumpState(_ncc.Grounded, _ncc.Velocity.y, Time.deltaTime);
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
        data.UseItem = _useItemPressed;
        
        var cameraLock = GetComponent<CameraTargetLock>();
        if (cameraLock != null && cameraLock.TargetPlayer != null)
        {
            var targetNetObj = cameraLock.TargetPlayer.GetComponentInParent<NetworkObject>();
            if (targetNetObj != null)
                data.TargetId = targetNetObj.Id;
        }

        input.Set(data); 
        _jumpPressed = false;
        _useItemPressed = false;
    }

    void CheckPlatformBeneath()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);
        
        if (Physics.Raycast(ray, out RaycastHit hit, 1.5f, platformLayer))
        {
            FragilePlatform platform = hit.collider.GetComponent<FragilePlatform>();
            
            if (platform != null && platform != currentPlatform)
            {
                currentPlatform = platform;
                
                if(MapManager.Instance != null)
                {
                    MapManager.Instance.RPC_TriggerPlatformBreak(platform.platformID);
                }
            }
        }
        else
        {
            currentPlatform = null; // Đang bay trên không
        }
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