using TMPro;
using UnityEngine;
using Crazy_Lobby.Chat;

public class ChatMessageItem : MonoBehaviour
{
    [Header("=== UI References ===")]
    public TMP_Text senderNameText;
    public TMP_Text messageText;

    public void Setup(ChatMessageData data, bool isMyMessage)
    {
        if (data == null) return;

        if (senderNameText != null)
        {
            senderNameText.text = data.senderDisplayName;
        }

        if (messageText != null)
        {
            messageText.text = data.message;
        }
    }
}
