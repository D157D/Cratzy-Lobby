using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CrazyLobby.Friends
{
    public class FriendsRequest : MonoBehaviour
    {
        public TextMeshProUGUI Name;
        public Button AcceptButton;
        public Button DeclineButton;

        private string _username;

        void Awake()
        {
            AcceptButton.onClick.AddListener(OnAcceptClicked);
            DeclineButton.onClick.AddListener(OnDeclineClicked);
        }
        public void SetData(string name, string status)
        {
            _username = name;
            Name.text = name;

            AcceptButton.onClick.RemoveAllListeners();
            AcceptButton.onClick.AddListener(OnAcceptClicked);

            DeclineButton.onClick.RemoveAllListeners();
            DeclineButton.onClick.AddListener(OnDeclineClicked);
        }

        private void OnAcceptClicked()
        {
            Debug.Log($"[FriendsRequest] Đang thử chấp nhận kết bạn với: {_username}");
            BackendManager.Instance.AcceptFriendRequest(_username, (success, message) => 
            {
                if (success)
                {
                    Debug.Log($"[FriendsRequest] Thành công: {message}");
                    
                    var panel = GetComponentInParent<Crazy_Lobby.UI.FriendsPanel>();
                    if (panel != null)
                    {
                        Debug.Log("[FriendsRequest] Đã tìm thấy FriendsPanel, đang chuyển tab...");
                        panel.OpenCurrentFriendsList();
                        panel.LoadFriendsList();
                    }
                    else
                    {
                        Debug.LogWarning("[FriendsRequest] Không tìm thấy FriendsPanel trong component cha để chuyển tab!");
                    }

                    Destroy(gameObject);
                }
                else
                {
                    Debug.LogError($"[FriendsRequest] Lỗi khi chấp nhận kết bạn: {message}");
                }
            });
        }

        private void OnDeclineClicked()
        {
            Debug.Log($"[FriendsRequest] Đang thử từ chối kết bạn với: {_username}");
            BackendManager.Instance.DeclineFriendRequest(_username, (success, message) => 
            {
                if (success)
                {
                    Debug.Log($"[FriendsRequest] Đã từ chối: {message}");
                    Destroy(gameObject);
                }
                else
                {
                    Debug.LogError($"[FriendsRequest] Lỗi khi từ chối kết bạn: {message}");
                }
            });
        }
    }
}