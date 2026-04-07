using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameInviteItemUI : MonoBehaviour
{
    public TMP_Text senderText;
    public TMP_Text statusText;
    public Button acceptButton;
    public Button rejectButton;

    private GameInviteData inviteData;
    private SocialManagerUI manager;

    public void Setup(GameInviteData data, SocialManagerUI socialManager)
    {
        inviteData = data;
        manager = socialManager;
        
        senderText.text = $"Mời từ: {data.senderUsername}";
        statusText.text = $"Phòng: {data.roomId}";

        acceptButton.onClick.AddListener(OnAcceptClicked);
        rejectButton.onClick.AddListener(OnRejectClicked);
    }

    private void OnAcceptClicked()
    {
        manager.ShowStatus("Đang chấp nhận lời mời...");
        BackendManager.Instance.RespondToInvite(inviteData.inviteId, "Accepted", (success, msg) =>
        {
            manager.ShowStatus(msg);
            if (success)
            {
                // Sau khi chấp nhận, tự động vào phòng
                manager.RefreshInviteList();
                
                // Vào phòng với mode Client và tên phòng từ invite
                Bootstrap.Instance.StartRoom(Fusion.GameMode.Client, inviteData.roomId);
            }
        });
    }

    private void OnRejectClicked()
    {
        manager.ShowStatus("Đang từ chối lời mời...");
        BackendManager.Instance.RespondToInvite(inviteData.inviteId, "Rejected", (success, msg) =>
        {
            manager.ShowStatus(msg);
            if (success)
            {
                manager.RefreshInviteList();
            }
        });
    }
}
