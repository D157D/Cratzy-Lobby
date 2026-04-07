using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SocialManagerUI : MonoBehaviour
{
    public static SocialManagerUI Instance;
    [Header("UI Panels")]
    public GameObject socialPanel;
    public Transform friendListContainer;
    public Transform requestListContainer;

    
    [Header("Prefabs")]
    public GameObject friendItemPrefab;
    public GameObject requestItemPrefab;
    [Header("Data")]
    public CharacterDatabase characterDatabase;

    [Header("Search")]
    public TMP_InputField searchInputField;
    public List<Button> addFriendButtons;
    public TMP_Text statusText;

    [Header("Tabs")]
    public GameObject friendListView;
    public GameObject requestListView;
    public GameObject inviteListView;
    public Transform inviteListContainer;
    public GameObject inviteItemPrefab;
    public Button openFriendsButtons;
    public Button openRequestsButtons;
    public Button myFriendsButton; // Nút hiển thị danh sách bạn bè (thường nằm trong Panel)

    [Header("Buttons")]
    public List<Button> toggleSocialButtons;
    public List<Button> refreshButtons;

    [Header("Own Profile")]
    public TMP_Text ownNameText;
    public Image ownAvatarImage;

    [Header("Auto Invite Detection")]
    public float pollInterval = 10f; // Kiểm tra mỗi 10 giây
    public GameObject inviteNotificationPanel; // Panel thông báo nhỏ lúc mới nhận
    public Transform notificationContainer; // Nơi chứa item thông báo
    private HashSet<int> knownInviteIds = new HashSet<int>();
    private Coroutine pollInvitesCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    private void Start()
    {
        Debug.Log("[SocialManagerUI] Initializing...");
        socialPanel.SetActive(false);
        
        foreach (var btn in toggleSocialButtons) RegisterToggleButton(btn);
        foreach (var btn in refreshButtons) RegisterRefreshButton(btn);
        foreach (var btn in addFriendButtons) RegisterAddFriendButton(btn);
        
        if(openFriendsButtons != null) openFriendsButtons.onClick.AddListener(() => {
            socialPanel.SetActive(true);
            ShowFriendsTab();
            RefreshAll(); // Refresh để đảm bảo dữ liệu mới nhất khi mở
        });
        if(openRequestsButtons != null) openRequestsButtons.onClick.AddListener(ShowRequestsTab);
        if(myFriendsButton != null) myFriendsButton.onClick.AddListener(ShowFriendsTab);
        
        if (searchInputField != null)
        {
            searchInputField.onValueChanged.AddListener(OnSearchValueChanged);
        }

        // Bắt đầu kiểm tra lời mời sau khi Login thành công
        BackendManager.OnLoginSuccess += StartInvitePolling;
        
        if (BackendManager.Instance != null && BackendManager.Instance.IsLoggedIn)
        {
            StartInvitePolling();
        }
    }

    private void OnDestroy()
    {
        BackendManager.OnLoginSuccess -= StartInvitePolling;
        if (pollInvitesCoroutine != null) StopCoroutine(pollInvitesCoroutine);
    }

    private void StartInvitePolling()
    {
        if (pollInvitesCoroutine != null) StopCoroutine(pollInvitesCoroutine);
        pollInvitesCoroutine = StartCoroutine(PollInvitesRoutine());

        // Tự động tải lại danh sách bạn bè khi vừa login xong để "Crazy_Lobby" hiện ra sớm
        RefreshAll(); 
    }

    private IEnumerator PollInvitesRoutine()
    {
        while (true)
        {
            if (BackendManager.Instance != null && BackendManager.Instance.IsLoggedIn)
            {
                BackendManager.Instance.GetGameInvites((success, invites) =>
                {
                    if (success && invites != null && invites.Length > 0)
                    {
                        bool hasNewInvite = false;
                        foreach (var invite in invites)
                        {
                            if (!knownInviteIds.Contains(invite.inviteId))
                            {
                                knownInviteIds.Add(invite.inviteId);
                                hasNewInvite = true;
                                ShowFloatingNotification(invite);
                            }
                        }
                        
                        if (hasNewInvite)
                        {
                            // Nếu bảng social đang mở thì refresh luôn list
                            if (socialPanel.activeSelf) RefreshInviteList();
                        }
                    }
                });
            }
            yield return new WaitForSeconds(pollInterval);
        }
    }

    private void ShowFloatingNotification(GameInviteData invite)
    {
        if (inviteNotificationPanel == null)
        {
            Debug.LogError("[SocialManagerUI] inviteNotificationPanel is not assigned in the Inspector!");
            return;
        }
        
        inviteNotificationPanel.SetActive(true);
        
        if (notificationContainer == null)
        {
            Debug.LogError("[SocialManagerUI] notificationContainer is not assigned in the Inspector!");
            return;
        }

        if (inviteItemPrefab == null)
        {
            Debug.LogError("[SocialManagerUI] inviteItemPrefab is missing! Cannot show notification.");
            return;
        }
        
        // Xóa thông báo cũ nếu muốn chỉ hiện 1 lời mời mới nhất
        foreach (Transform t in notificationContainer) Destroy(t.gameObject);

        GameObject go = Instantiate(inviteItemPrefab, notificationContainer);
        var itemUI = go.GetComponent<GameInviteItemUI>();
        
        if (itemUI != null)
        {
            itemUI.Setup(invite, this);
        }
        else
        {
            Debug.LogError("[SocialManagerUI] inviteItemPrefab is missing GameInviteItemUI component!");
        }
        
        // Tự động đóng sau 15 giây
        StartCoroutine(AutoCloseNotificationRoutine(go));
    }

    private IEnumerator AutoCloseNotificationRoutine(GameObject item)
    {
        yield return new WaitForSeconds(15f);
        if (item != null) Destroy(item);
        
        // Kiểm tra xem còn thông báo nào khác không, nếu không thì ẩn panel thông báo
        yield return new WaitForEndOfFrame();
        if (notificationContainer != null && notificationContainer.childCount == 0)
        {
            if (inviteNotificationPanel != null) inviteNotificationPanel.SetActive(false);
        }
    }

    private void OnSearchValueChanged(string value)
    {
        // Khi gõ vào ô search, ta lọc danh sách đang hiển thị (chỉ friends)
        FilterFriendList(value);
    }

    private void FilterFriendList(string filter)
    {
        if (friendListContainer == null) return;
        filter = filter.ToLower();

        foreach (Transform child in friendListContainer)
        {
            var item = child.GetComponent<FriendItemUI>();
            if (item != null)
            {
                bool matches = string.IsNullOrEmpty(filter) || item.GetDisplayName().ToLower().Contains(filter);
                child.gameObject.SetActive(matches);
            }
        }
    }

    public void RegisterToggleButton(Button btn)
    {
        if (btn != null)
        {
            Debug.Log($"[SocialManagerUI] Registering Toggle Button: {btn.name}");
            btn.onClick.AddListener(ToggleSocialPanel);
        }
    }

    public void RegisterRefreshButton(Button btn)
    {
        if (btn != null)
        {
            Debug.Log($"[SocialManagerUI] Registering Refresh Button: {btn.name}");
            btn.onClick.AddListener(RefreshAll);
        }
    }

    public void RegisterAddFriendButton(Button btn)
    {
        if (btn != null)
        {
            Debug.Log($"[SocialManagerUI] Registering Add Friend Button: {btn.name}");
            btn.onClick.AddListener(OnAddFriendClicked);
        }
    }

    public void ToggleSocialPanel()
    {
        bool isOpen = !socialPanel.activeSelf;
        socialPanel.SetActive(isOpen);
        if (isOpen)
        {
            RefreshAll();
        }
    }

    public void ShowFriendsTab()
    {
        Debug.Log("[SocialManagerUI] Switching to Friends Tab");
        if(friendListView != null) friendListView.SetActive(true);
        if(requestListView != null) requestListView.SetActive(false);
        if(inviteListView != null) inviteListView.SetActive(false);
        RefreshFriendList();
    }

    public void ShowRequestsTab()
    {
        Debug.Log("[SocialManagerUI] Switching to Requests Tab");
        if(friendListView != null) friendListView.SetActive(false);
        if(requestListView != null) requestListView.SetActive(true);
        if(inviteListView != null) inviteListView.SetActive(false);
        RefreshRequestList();
    }

    public void ShowInvitesTab()
    {
        Debug.Log("[SocialManagerUI] Switching to Invites Tab");
        if(friendListView != null) friendListView.SetActive(false);
        if(requestListView != null) requestListView.SetActive(false);
        if(inviteListView != null) inviteListView.SetActive(true);
        RefreshInviteList();
    }

    public void RefreshAll()
    {
        RefreshOwnProfile();
        RefreshFriendList();
        RefreshRequestList();
        RefreshInviteList();
    }

    private void RefreshOwnProfile()
    {
        if (ownNameText != null) ownNameText.text = BackendManager.Instance.CurrentDisplayName;
        
        if (ownAvatarImage != null && !string.IsNullOrEmpty(BackendManager.Instance.CurrentCharacterType) && characterDatabase != null)
        {
            if (System.Enum.TryParse(BackendManager.Instance.CurrentCharacterType, out CharacterType type))
            {
                var entry = characterDatabase.GetEntry(type);
                if (entry.Icon != null) ownAvatarImage.sprite = entry.Icon;
                else Debug.LogWarning($"[SocialManagerUI] Missing avatar icon for type {type} in database!");
            }
        }
    }

    public void RefreshFriendList()
    {
        if (friendListContainer == null) 
        {
            Debug.LogWarning("[SocialManagerUI] friendListContainer is null!");
            return;
        }
        foreach (Transform child in friendListContainer) Destroy(child.gameObject);

        statusText.text = "Đang tải danh sách bạn bè...";
        Debug.Log("[SocialManagerUI] Fetching Friends...");
        BackendManager.Instance.GetFriends((success, friends) =>
        {
            if (success)
            {
                if (friends != null && friends.Length > 0)
                {
                    Debug.Log($"[SocialManagerUI] JSON trả về: {JsonUtility.ToJson(new { friends = friends })}"); // Log để kiểm tra dữ liệu
                    Debug.Log($"[SocialManagerUI] Found {friends.Length} friends.");
                    foreach (var friend in friends)
                    {
                        if (friendItemPrefab == null) continue;
                        GameObject go = Instantiate(friendItemPrefab, friendListContainer);
                        go.GetComponent<FriendItemUI>().Setup(friend, this);
                    }
                    statusText.text = ""; // Clear on success
                }
                else
                {
                    Debug.Log("[SocialManagerUI] Friend list is empty.");
                    statusText.text = "Bạn chưa có người bạn nào.";
                }
            }
            else
            {
                Debug.LogError("[SocialManagerUI] Failed to fetch friends. Check API URL!");
                statusText.text = "Lỗi: Không thể kết nối danh sách bạn bè (404).";
            }
        });
    }

    public void RefreshRequestList()
    {
        if (requestListContainer == null) 
        {
            Debug.LogWarning("[SocialManagerUI] requestListContainer is null!");
            return;
        }
        foreach (Transform child in requestListContainer) Destroy(child.gameObject);

        statusText.text = "Đang tải yêu cầu kết bạn...";
        Debug.Log("[SocialManagerUI] Fetching Pending Requests...");
        BackendManager.Instance.GetPendingRequests((success, requests) =>
        {
            if (success)
            {
                if (requests != null && requests.Length > 0)
                {
                    Debug.Log($"[SocialManagerUI] Found {requests.Length} pending requests.");
                    foreach (var req in requests)
                    {
                        if (requestItemPrefab == null) continue;
                        GameObject go = Instantiate(requestItemPrefab, requestListContainer);
                        go.GetComponent<FriendRequestItemUI>().Setup(req, this);
                    }
                    statusText.text = "";
                }
                else
                {
                    statusText.text = "Không có yêu cầu kết bạn nào.";
                }
            }
            else
            {
                statusText.text = "Lỗi: Không thể tải yêu cầu kết bạn.";
            }
        });
    }

    public void RefreshInviteList()
    {
        if (inviteListContainer == null) 
        {
            Debug.LogWarning("[SocialManagerUI] inviteListContainer is null!");
            return;
        }
        foreach (Transform child in inviteListContainer) Destroy(child.gameObject);

        statusText.text = "Đang tải lời mời chơi game...";
        Debug.Log("[SocialManagerUI] Fetching Game Invites...");
        BackendManager.Instance.GetGameInvites((success, invites) =>
        {
            if (success)
            {
                if (invites != null && invites.Length > 0)
                {
                    Debug.Log($"[SocialManagerUI] Found {invites.Length} game invites.");
                    foreach (var invite in invites)
                    {
                        if (inviteItemPrefab == null) continue;
                        GameObject go = Instantiate(inviteItemPrefab, inviteListContainer);
                        go.GetComponent<GameInviteItemUI>().Setup(invite, this);
                    }
                    statusText.text = "";
                }
                else
                {
                    statusText.text = "Không có lời mời nào.";
                }
            }
            else
            {
                statusText.text = "Lỗi: Không thể tải lời mời.";
            }
        });
    }

    private void OnAddFriendClicked()
    {
        string username = searchInputField.text.Trim();
        if (string.IsNullOrEmpty(username)) return;
        AddFriend(username);
    }

    public void AddFriend(string username)
    {
        if (string.IsNullOrEmpty(username)) return;

        statusText.text = $"Đang gửi yêu cầu kết bạn tới {username}...";
        BackendManager.Instance.AddFriend(username, (success, message) =>
        {
            statusText.text = message;
            if (success)
            {
                if (searchInputField != null && searchInputField.text.Trim() == username)
                {
                    searchInputField.text = "";
                }
                RefreshRequestList();
            }
        });
    }

    public void ShowStatus(string msg)
    {
        statusText.text = msg;
    }
}
