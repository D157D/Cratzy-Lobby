using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
namespace Crazy_Lobby.UI
{
    public class FriendsPanel : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject FriendsPanels;
        [Header("FindFriends")]
        public TMP_InputField FindFriendsInput;
        public GameObject FindFriendsResult;
        public Transform FriendsContainer;
        public GameObject FriendItemPrefab;
        [Header("Request")]
        public Transform FriendsRequestContainer;
        public GameObject FriendsRequestItemPrefab;

        [Header("Button")]
        public Button FindFriendsButton;
        public Button OpenFriendsPanel;
        public Button CloseFriendsPanel;
        public Button OpenFriendsRequestPanels;
        public Button OpenMyFriendsList;
        void Awake()
        {
            FriendsPanels.SetActive(false);
        }
        private Coroutine searchCoroutine;

        private void Start()
        {
            OpenFriendsPanel.onClick.AddListener(OpenFriendsPanels);
            CloseFriendsPanel.onClick.AddListener(CloseFriendsPanels);
            OpenMyFriendsList.onClick.AddListener(LoadFriendsList);
            OpenMyFriendsList.onClick.AddListener(OpenCurrentFriendsList);
            FindFriendsButton.onClick.AddListener(FindFriends);
            OpenFriendsRequestPanels.onClick.AddListener(OpenFriendsRequest);

            FindFriendsInput.onValueChanged.AddListener(OnSearchInputChanged);
        }

        private void OnSearchInputChanged(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                if (searchCoroutine != null) StopCoroutine(searchCoroutine);
                LoadFriendsList();
                if (FindFriendsResult != null) FindFriendsResult.SetActive(false);
                return;
            }

            if (searchCoroutine != null) StopCoroutine(searchCoroutine);
            searchCoroutine = StartCoroutine(SearchRoutine(text));
        }

        private System.Collections.IEnumerator SearchRoutine(string text)
        {
            yield return new WaitForSeconds(0.5f); // Debounce
            FindFriends(text);
        }

        private void OpenFriendsPanels()
        {
            FriendsPanels.SetActive(true);
            OpenCurrentFriendsList();
        }

        private void CloseFriendsPanels()
        {
            FriendsPanels.SetActive(false);
        }

        private void OpenFriendsRequest()
        {
            FriendsRequestContainer.gameObject.SetActive(true);
            FriendsContainer.gameObject.SetActive(false);
            OpenMyFriendsList.gameObject.SetActive(true);
            LoadFriendsRequest();
        }
        public void OpenCurrentFriendsList()
        {
            FriendsRequestContainer.gameObject.SetActive(false);
            FriendsContainer.gameObject.SetActive(true);
            FindFriendsInput.gameObject.SetActive(true);   
            OpenMyFriendsList.gameObject.SetActive(false);
            FindFriendsInput.text = "";
        }
        
        public void FindFriends()
        {
            FindFriends(FindFriendsInput.text);
        }

        private void FindFriends(string query)
        {
            if (string.IsNullOrEmpty(query)) return;

            BackendManager.Instance.FindFriendsInDB(query, (isSuccess, friends) => 
            {
                if (isSuccess && friends != null)
                {
                    foreach (Transform child in FriendsContainer)
                    {
                        Destroy(child.gameObject);
                    }

                    if (friends.Count > 0)
                    {
                        FindFriendsResult?.SetActive(true);
                        foreach (var friend in friends)
                        {
                            GameObject obj = Instantiate(FriendItemPrefab, FriendsContainer);
                            CrazyLobby.Friends.FriendsPef item = obj.GetComponent<CrazyLobby.Friends.FriendsPef>();
                            if (item != null)
                            {
                                item.SetData(friend.displayName, friend.status);
                                if (item.removeButton != null) item.removeButton.gameObject.SetActive(false);
                                if (item.addButton != null) item.addButton.gameObject.SetActive(true);
                            }
                        }
                    }
                    else
                    {
                        FindFriendsResult?.SetActive(false);
                        Debug.Log($"Không tìm thấy người chơi nào có tên: {query}");
                    }
                }
                else
                {
                    FindFriendsResult?.SetActive(false);
                    Debug.Log($"Không tìm thấy người chơi nào có tên: {query} hoặc đã xảy ra lỗi.");
                }   
            });
        }

        public void LoadFriendsList()
        {
            BackendManager.Instance.GetFriendsList((isSuccess, friends) => 
            {
                if (isSuccess && friends != null)
                {
                    foreach (Transform child in FriendsContainer)
                    {
                        Destroy(child.gameObject);
                    }

                    foreach (var friend in friends)
                    {
                        GameObject obj = Instantiate(FriendItemPrefab, FriendsContainer);
                        CrazyLobby.Friends.FriendsPef item = obj.GetComponent<CrazyLobby.Friends.FriendsPef>();
                        if (item != null)
                        {
                            item.SetData(friend.displayName, friend.isOnline ? "Online" : "Offline");
                            if (item.removeButton != null) item.removeButton.gameObject.SetActive(true);
                            if (item.addButton != null) item.addButton.gameObject.SetActive(false);
                        }
                    }
                }
                else
                {
                    Debug.LogError("Failed to load friends list.");
                }
            });
        }

        public void LoadFriendsRequest()
        {
            BackendManager.Instance.GetFriendRequestsList((isSuccess, requests) => 
            {
                if (isSuccess && requests != null)
                {
                    foreach (Transform child in FriendsRequestContainer)
                    {
                        Destroy(child.gameObject);
                    }

                    foreach (var request in requests)
                    {
                        GameObject obj = Instantiate(FriendsRequestItemPrefab, FriendsRequestContainer);
                        CrazyLobby.Friends.FriendsRequest item = obj.GetComponent<CrazyLobby.Friends.FriendsRequest>();
                        if (item != null)
                        {
                            item.SetData(request.displayName, "Pending");
                        }
                    }
                }
                else
                {
                    Debug.LogError("Failed to load friend requests.");
                }
            });
        }
    }
}