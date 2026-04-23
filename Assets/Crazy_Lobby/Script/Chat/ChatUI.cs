using System.Collections;
using System.Collections.Generic;
using Crazy_Lobby.Chat;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatUI : MonoBehaviour
{
    [Header("=== UI References ===")]
    public ScrollRect chatScrollRect;
    public RectTransform chatContent;
    public GameObject messagePrefab;
    public TMP_InputField chatInputField;
    public Button sendButton;
    public Button toggleButton;
    public GameObject chatPanel;

    [Header("=== Cài đặt hiển thị ===")]
    public int maxDisplayMessages = 50;

    private List<GameObject> _displayedMessages = new List<GameObject>();
    private bool _isChatOpen = false;
    private bool _isSubscribed = false;

    private void Start()
    {
        if (sendButton != null)
            sendButton.onClick.AddListener(OnSendButtonClicked);

        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleChat);

        if (chatInputField != null)
            chatInputField.onSubmit.AddListener(OnInputSubmit);

        if (chatPanel != null)
        {
            chatPanel.SetActive(false);
            _isChatOpen = false;
        }

        // Khởi động Runner ngầm ngay khi vào scene để tránh bị chậm khi mở chat
        if (ChatLobbyRunner.Instance != null)
        {
            ChatLobbyRunner.Instance.StartLobbyChat();
        }
        else
        {
            Debug.LogWarning("[ChatUI] ChatLobbyRunner chưa có trong scene. Hãy thêm GameObject với component ChatLobbyRunner.");
        }
    }

    private void Update()
    {
        // Đăng ký sự kiện an toàn 1 lần duy nhất khi ChatManager đã sẵn sàng
        if (ChatManager.Instance != null && !_isSubscribed)
        {
            ChatManager.Instance.OnMessageReceived += OnNewMessage;
            _isSubscribed = true;
        }
    }

    public void ToggleChat()
    {
        _isChatOpen = !_isChatOpen;

        if (chatPanel != null)
            chatPanel.SetActive(_isChatOpen);

        if (_isChatOpen)
        {
            if (chatInputField != null)
            {
                chatInputField.ActivateInputField();
                chatInputField.Select();
            }
        }
    }

    private void OnSendButtonClicked()
    {
        SendCurrentMessage();
    }

    private void OnInputSubmit(string text)
    {
        SendCurrentMessage();
    }

    private void SendCurrentMessage()
    {
        if (chatInputField == null) return;

        string message = chatInputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        if (ChatManager.Instance != null)
        {
            ChatManager.Instance.SendMessage(message);
        }
        else
        {
            Debug.LogWarning("[ChatUI] ChatManager chưa sẵn sàng.");
        }

        chatInputField.text = "";
        chatInputField.ActivateInputField();
        chatInputField.Select();
    }

    private void OnNewMessage(ChatMessageData messageData)
    {
        DisplayMessage(messageData);
    }

    private void DisplayMessage(ChatMessageData messageData)
    {
        if (messagePrefab == null || chatContent == null) return;

        // Kiểm tra tin nhắn của mình
        bool isMyMessage = false;
        if (BackendManager.Instance != null)
        {
            isMyMessage = messageData.senderDisplayName == BackendManager.Instance.CurrentDisplayName;
        }

        // Spawn prefab
        GameObject msgObj = Instantiate(messagePrefab, chatContent);
        ChatMessageItem item = msgObj.GetComponent<ChatMessageItem>();

        if (item != null)
        {
            item.Setup(messageData, isMyMessage);
        }
        else
        {
            // Fallback: nếu prefab chỉ có 1 TMP_Text
            TMP_Text fallbackText = msgObj.GetComponent<TMP_Text>();
            if (fallbackText != null)
            {
                fallbackText.text = $"{messageData.senderDisplayName}: {messageData.message}";
            }
        }

        _displayedMessages.Add(msgObj);

        // Xóa tin nhắn cũ nếu vượt quá giới hạn
        while (_displayedMessages.Count > maxDisplayMessages)
        {
            GameObject oldMsg = _displayedMessages[0];
            _displayedMessages.RemoveAt(0);
            Destroy(oldMsg);
        }

        StartCoroutine(ScrollToBottom());
    }

    private IEnumerator ScrollToBottom()
    {
        yield return null;
        if (chatScrollRect != null)
            chatScrollRect.verticalNormalizedPosition = 0f;
    }

    private void OnDestroy()
    {
        // Hủy đăng ký sự kiện an toàn
        if (ChatManager.Instance != null && _isSubscribed)
        {
            ChatManager.Instance.OnMessageReceived -= OnNewMessage;
            _isSubscribed = false;
        }

        if (sendButton != null)
            sendButton.onClick.RemoveListener(OnSendButtonClicked);

        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(ToggleChat);
    }
}