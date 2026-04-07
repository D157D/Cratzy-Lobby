using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FriendItemUI : MonoBehaviour
{
    public TMP_Text displayNameText;
    public TMP_Text statusText;
    public Image avatarImage;
    public Button deleteButton;
    public Button inviteButton;

    private FriendData friendData;
    private SocialManagerUI manager;

    public void Setup(FriendData data, SocialManagerUI socialManager)
    {
        friendData = data;
        manager = socialManager;
        
        displayNameText.text = string.IsNullOrEmpty(data.displayName) ? data.username : data.displayName;
        statusText.text = data.status == "Online" ? "<color=green>Online</color>" : "<color=red>Offline</color>";

        if (!string.IsNullOrEmpty(data.characterType) && avatarImage != null && manager.characterDatabase != null)
        {
            if (System.Enum.TryParse(data.characterType, out CharacterType type))
            {
                var entry = manager.characterDatabase.GetEntry(type);
                if (entry.Icon != null) avatarImage.sprite = entry.Icon;
            }
        }

        deleteButton.onClick.AddListener(OnDeleteClicked);
        inviteButton.onClick.AddListener(OnInviteClicked);
        
        inviteButton.gameObject.SetActive(data.status == "Online"); // Chỉ hiển thị mời khi online
    }

    public string GetDisplayName()
    {
        return string.IsNullOrEmpty(friendData.displayName) ? friendData.username : friendData.displayName;
    }

    private void OnDeleteClicked()
    {
        if (manager != null) manager.ShowStatus("Đang xóa bạn bè...");

        BackendManager.Instance.DeleteFriend(friendData.username, (success, msg) =>
        {
            if (manager != null)
            {
                manager.ShowStatus(msg);
                if (success) manager.RefreshFriendList();
            }
        });
    }

    private void OnInviteClicked()
    {
        string currentRoom = BackendManager.Instance.CurrentRoomId;
        if (string.IsNullOrEmpty(currentRoom))
        {
            if (manager != null) manager.ShowStatus("Bạn cần ở trong phòng để mời!");
            return;
        }

        if (manager != null) manager.ShowStatus($"Đang mời {friendData.username}...");

        BackendManager.Instance.InviteToGame(friendData.username, currentRoom, (success, msg) =>
        {
            if (manager != null) manager.ShowStatus(msg);
        });
    }
}
