using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace CrazyLobby.Friends
{
    public class FriendsPef : MonoBehaviour
    {
        [SerializeField] public TextMeshProUGUI nameText;
        public TextMeshProUGUI statusText;
        public Button addButton;
        public Button removeButton;
        public Button inviteButton;

        private string _displayName;

         public void SetData(string name, string status)
        {
            _displayName = name;
            nameText.text = name;
            statusText.text = status;

            addButton?.onClick.RemoveAllListeners();
            addButton?.onClick.AddListener(() => {
                BackendManager.Instance.AddFriend(_displayName, (success, message) => {
                    Debug.Log(message);
                    if (success) addButton.gameObject.SetActive(false); // Ẩn nút sau khi gửi yêu cầu thành công
                });
            });

            removeButton?.onClick.RemoveAllListeners();
            removeButton?.onClick.AddListener(() => {
                BackendManager.Instance.RemoveFriend(_displayName, (success, message) => {
                    Debug.Log(message);
                    if (success) Destroy(gameObject);
                });
            });
        }

        public void GetFriendsFromBackEnd()
        {
            BackendManager.Instance.GetFriendsList((success, friends) => {
                // Logic to update this specific prefab if needed
                if (success && friends != null)
                {
                    foreach (var friend in friends)
                    {
                        if (friend.displayName == _displayName)
                        {
                            statusText.text = friend.status;
                            break;
                        }
                    }
                }
            });
        }
    }
}