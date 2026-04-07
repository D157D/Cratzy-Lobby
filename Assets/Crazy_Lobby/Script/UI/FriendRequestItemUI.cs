using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FriendRequestItemUI : MonoBehaviour
{
    public TMP_Text senderNameText;
    public Image avatarImage;
    public Button acceptButton;
    public Button rejectButton;

    private FriendRequestData requestData;
    private SocialManagerUI manager;

    public void Setup(FriendRequestData data, SocialManagerUI socialManager)
    {
        requestData = data;
        manager = socialManager;

        senderNameText.text = string.IsNullOrEmpty(data.senderDisplayName) 
                                ? data.senderUsername 
                                : data.senderDisplayName;

        if (!string.IsNullOrEmpty(data.characterType) && avatarImage != null && manager.characterDatabase != null)
        {
            if (System.Enum.TryParse(data.characterType, out CharacterType type))
            {
                var entry = manager.characterDatabase.GetEntry(type);
                if (entry.Icon != null) avatarImage.sprite = entry.Icon;
            }
        }

        acceptButton.onClick.AddListener(OnAcceptClicked);
        rejectButton.onClick.AddListener(OnRejectClicked);
    }

    private void OnAcceptClicked()
    {
        if (manager != null) manager.ShowStatus($"Đang chấp nhận lời mời từ {requestData.senderUsername}...");
        
        BackendManager.Instance.AcceptFriend(requestData.senderUsername, (success, msg) =>
        {
            if (manager != null)
            {
                manager.ShowStatus(msg);
                if (success) manager.RefreshAll();
            }
        });
    }

    private void OnRejectClicked()
    {
        if (manager != null) manager.ShowStatus("Đang từ chối lời mời...");
        
        BackendManager.Instance.RejectFriend(requestData.senderUsername, (success, msg) =>
        {
            if (manager != null)
            {
                manager.ShowStatus(msg);
                if (success) manager.RefreshRequestList();
            }
        });
    }
}
