using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

/// <summary>
/// Gắn lên một GameObject thường trong scene (KHÔNG phải NetworkBehaviour).
/// Script này tạo NetworkRunner, kết nối vào phòng chat lobby,
/// rồi spawn ChatManager để dùng RPC.
/// </summary>
public class ChatLobbyRunner : MonoBehaviour, INetworkRunnerCallbacks
{
    public static ChatLobbyRunner Instance { get; private set; }

    [Tooltip("Prefab có component ChatManager + NetworkObject. Gán vào đây.")]
    public GameObject chatManagerPrefab;

    private NetworkRunner _runner;
    private bool _isStarting = false;
    private bool _isConnected = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Gọi hàm này để bắt đầu kết nối Fusion cho lobby chat.
    /// Được gọi từ ChatUI khi người dùng bật chat.
    /// </summary>
    public async void StartLobbyChat()
    {
        if (_isConnected || _isStarting) return;
        _isStarting = true;

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = false;

        Debug.Log("[ChatLobbyRunner] Đang kết nối phòng chat lobby...");

        try
        {
            _runner.AddCallbacks(this);

            var result = await _runner.StartGame(new StartGameArgs
            {
                GameMode    = GameMode.Shared,
                SessionName = "CrazyLobby_Chat_Room",
            });

            if (result.Ok)
            {
                Debug.Log("[ChatLobbyRunner] Kết nối thành công!");
                _isConnected = true;
                TrySpawnChatManager();
            }
            else
            {
                Debug.LogError($"[ChatLobbyRunner] Lỗi: {result.ShutdownReason}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ChatLobbyRunner] Exception: {ex.Message}");
        }

        _isStarting = false;
    }

    private void TrySpawnChatManager()
    {
        if (_runner == null || !_runner.IsSharedModeMasterClient) return;
        if (ChatManager.Instance != null) return;
        if (chatManagerPrefab == null)
        {
            Debug.LogError("[ChatLobbyRunner] chatManagerPrefab chưa được gán trong Inspector!");
            return;
        }
        _runner.Spawn(chatManagerPrefab, Vector3.zero, Quaternion.identity);
        Debug.Log("[ChatLobbyRunner] Đã spawn ChatManager.");
    }

    // ============================
    // INetworkRunnerCallbacks — copy đúng signature từ dự án
    // ============================

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { _isConnected = false; }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { TrySpawnChatManager(); }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { _isConnected = false; }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}
