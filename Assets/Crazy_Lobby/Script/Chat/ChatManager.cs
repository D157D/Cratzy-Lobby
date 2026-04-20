using System;
using System.Collections.Generic;
using Fusion;
using Crazy_Lobby.Chat;
using UnityEngine;

/// <summary>
/// NetworkBehaviour chứa RPC chat.
/// PHẢI được spawn bởi ChatLobbyRunner thông qua runner.Spawn().
/// Gắn script này lên một prefab, đặt prefab vào Resources hoặc gán vào ChatLobbyRunner.chatManagerPrefab.
/// </summary>
public class ChatManager : NetworkBehaviour
{
    public static ChatManager Instance { get; private set; }

    public event Action<ChatMessageData> OnMessageReceived;

    private List<ChatMessageData> _messages = new List<ChatMessageData>();
    public IReadOnlyList<ChatMessageData> Messages => _messages;

    public int maxMessages = 100;

    public override void Spawned()
    {
        // Chạy trên tất cả clients sau khi được spawn bởi Runner
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[ChatManager] Có nhiều hơn 1 ChatManager. Giữ lại cái mới nhất.");
        }
        Instance = this;
        Debug.Log("[ChatManager] Đã Spawned và sẵn sàng nhận/gửi RPC!");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ============================
    // GỬI TIN NHẮN
    // ============================

    public void SendMessage(string rawMessage)
    {
        if (string.IsNullOrWhiteSpace(rawMessage)) return;

        if (Object == null || !Object.IsValid)
        {
            Debug.LogWarning("[ChatManager] Chưa có ChatManager network object. Hãy chờ kết nối xong...");
            return;
        }

        string senderName = "Player";
        if (BackendManager.Instance != null && !string.IsNullOrEmpty(BackendManager.Instance.CurrentDisplayName))
            senderName = BackendManager.Instance.CurrentDisplayName;

        RPC_SendMessage(senderName, rawMessage);
    }

    // ============================
    // FUSION RPC  
    // ============================

    /// <summary>
    /// Gửi lên tất cả mọi người (Shared Mode: dùng RpcTargets.All thay vì StateAuthority)
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SendMessage(string senderName, string message, RpcInfo info = default)
    {
        var msgData = new ChatMessageData
        {
            senderDisplayName = senderName,
            message = message
        };

        _messages.Add(msgData);

        while (_messages.Count > maxMessages)
            _messages.RemoveAt(0);

        OnMessageReceived?.Invoke(msgData);
    }

    // ============================
    // TIỆN ÍCH
    // ============================

    public void ClearMessages() => _messages.Clear();
}
