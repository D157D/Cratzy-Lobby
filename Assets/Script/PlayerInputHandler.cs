using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System;
using Fusion.Sockets;

public class PlayerInputHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private PlayerItem playerItem;

    private Vector2 moveInput;
    private bool jumpPressed;
    private bool useItemPressed;

    private NetworkObject networkObject;

    private void Awake()
    {
        networkObject = GetComponent<NetworkObject>();

        if (playerItem == null)
            playerItem = GetComponent<PlayerItem>();
    }

    private void OnEnable()
    {
        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null)
            runner.AddCallbacks(this);
    }

    private void Update()
    {
        // 🔥 CHỈ player local mới đọc input
        if (networkObject == null || !networkObject.HasInputAuthority)
            return;

        // Movement
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        // Jump
        if (Input.GetKeyDown(KeyCode.Space))
            jumpPressed = true;

        // Use Item (1 lần click)
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.E))
        {
            useItemPressed = true;
            Debug.Log("Use Item Input!");
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        MyInputData data = new MyInputData();

        data.move = moveInput;
        data.jump = jumpPressed;

        // 🔥 CHỈ gửi 1 lần
        data.useItemPressed = useItemPressed;

        // Slot đang chọn
        data.selectedSlot = 0;
        if (playerItem != null)
            data.selectedSlot = playerItem.GetLocalSelectedSlot();

        input.Set(data);

        // Reset
        jumpPressed = false;
        useItemPressed = false;
    }

    // ===== CALLBACKS =====
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}