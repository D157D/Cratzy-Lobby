using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // 👉 Thêm thư viện quản lý Scene
using System.IO; // 👉 Thêm thư viện để tách lấy tên Scene

public class BootstrapUIManager : MonoBehaviour
{
    public static BootstrapUIManager Instance;

    public GameObject IngamePanel;
    public Button btnQuickJoin;
    public Button btnCreateRoom;
    public Button btnJoinByID;
    public Button playButton;
    
    [Header("Room Type Settings")]
    public Toggle privateRoomToggle; // Toggle để chọn phòng riêng tư
    public TextMeshProUGUI roomTypeDisplayText; // Text hiển thị trạng thái phòng (Public/Private)

    public TMP_InputField roomIDInput;
    public TextMeshProUGUI IDText;
    public TextMeshProUGUI roomIdDisplayText;

    [SerializeField] private GameObject loadingPanel; 
    [SerializeField] private TextMeshProUGUI loadingTimeText;

    // Đã xóa biến gameSceneName cố định ở đây
    private Bootstrap _bootstrap;

    void Awake() 
    { 
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        _bootstrap = FindObjectOfType<Bootstrap>();
        if (_bootstrap == null)
        {
            Debug.LogError("Bootstrap component not found in scene.", this);
            return;
        }

        if (btnQuickJoin != null) btnQuickJoin.onClick.AddListener(OnQuickJoinClicked);
        if (btnCreateRoom != null) btnCreateRoom.onClick.AddListener(OnCreateRoomClicked);
        if (btnJoinByID != null) btnJoinByID.onClick.AddListener(OnJoinByIDClicked);
        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);

        // Cấu hình input: chỉ số, tối đa 3 ký tự
        if (roomIDInput != null)
        {
            roomIDInput.characterLimit = 3;
            roomIDInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            roomIDInput.onValueChanged.AddListener(delegate { ShowID(); });
        }

        if (privateRoomToggle != null) privateRoomToggle.onValueChanged.AddListener(OnPrivateRoomToggleChanged);

        ShowConnectionUI();
        ShowID();
    }

    private void Update()
    {
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            if (playButton != null && playButton.gameObject.activeInHierarchy)
            {
                OnPlayClicked();
            }
        }
        // Bấm Tab để chuyển đổi giữa phòng riêng tư và công khai
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (privateRoomToggle != null)
            {
                privateRoomToggle.isOn = !privateRoomToggle.isOn; // Toggle the state
            }
        }
    }

    // 👉 HÀM ĐƯỢC NÂNG CẤP: TỰ ĐỘNG TÌM SCENE TIẾP THEO
    private void OnPlayClicked()
    {
        if (_bootstrap == null) return;

        // Lấy vị trí (index) của Scene hiện tại (Ví dụ: Lobby đang là 0)
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1; // Scene tiếp theo sẽ là 1

        // Kiểm tra xem trong Build Settings có Scene tiếp theo không
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            // Lấy đường dẫn của Scene tiếp theo (vd: "Assets/Scenes/Map1.unity")
            string nextScenePath = SceneUtility.GetScenePathByBuildIndex(nextSceneIndex);
            
            // Tách lấy đúng cái tên (vd: "Map1")
            string nextSceneName = Path.GetFileNameWithoutExtension(nextScenePath);
            
            Debug.Log($"[BootstrapUIManager] Chuyển map tự động: Chuyển tới Scene -> {nextSceneName}");
            _bootstrap.OnPlayClicked(nextSceneName);
        }
        else
        {
            Debug.LogError("[BootstrapUIManager] LỖI: Không tìm thấy Scene tiếp theo! Bạn đã thêm Scene Map chơi vào File -> Build Settings chưa?");
        }
    }

    private void OnPrivateRoomToggleChanged(bool isPrivate)
    {
        ShowID(); // Cập nhật hiển thị ID
    }

    private string GenerateRoomID()
    {
        return Random.Range(100, 1000).ToString(); // 100 -> 999
    }

    private void OnQuickJoinClicked()
    {
        if (_bootstrap == null) return;

        if (roomIDInput != null && string.IsNullOrEmpty(roomIDInput.text))
        {
            roomIDInput.text = GenerateRoomID();
        }

        ShowID();

        string roomID = roomIDInput != null ? roomIDInput.text : "000";

        ShowLoadingPanel(true);
        _bootstrap.StartRoom(GameMode.AutoHostOrClient, roomID, privateRoomToggle.isOn);
    }

    private void OnCreateRoomClicked()
    {
        if (_bootstrap == null) return;

        if (roomIDInput != null && string.IsNullOrEmpty(roomIDInput.text))
        {
            roomIDInput.text = GenerateRoomID();
        }

        ShowID();

        string roomID = roomIDInput != null ? roomIDInput.text : "000";

        ShowLoadingPanel(true);
        _bootstrap.StartRoom(GameMode.Host, roomID, privateRoomToggle.isOn);
    }

    private void OnJoinByIDClicked()
    {
        if (_bootstrap == null) return;

        string roomID = roomIDInput != null ? roomIDInput.text : "";

        if (string.IsNullOrEmpty(roomID))
        {
            Debug.LogWarning("Vui lòng nhập ID phòng để Join!");
            return;
        }

        ShowID();

        ShowLoadingPanel(true);
        _bootstrap.StartRoom(GameMode.Client, roomID, true); // Luôn là phòng riêng tư khi join bằng ID
    }

    public void ShowLobby(bool isHost, string roomName)
    {
        if (IngamePanel != null) IngamePanel.SetActive(true);
        if (playButton != null) playButton.gameObject.SetActive(isHost);

        if (roomIdDisplayText != null)
        {
            roomIdDisplayText.text = $"Room ID: {roomName}";
        }
    }

    public void ShowConnectionUI()
    {
        OnPrivateRoomToggleChanged(privateRoomToggle != null && privateRoomToggle.isOn); // Cập nhật trạng thái ban đầu của UI
        if (IngamePanel != null) IngamePanel.SetActive(false);
    }

    public void ShowID()
    {
        if (IDText != null && roomIDInput != null)
        {
            string id = roomIDInput.text;

            if (string.IsNullOrEmpty(id))
                IDText.text = "ID : ---";
            else
                IDText.text = $"ID : {id}";
        }

        // Cập nhật text hiển thị loại phòng (Public/Private)
        if (roomTypeDisplayText != null && privateRoomToggle != null)
        {
            if (privateRoomToggle.isOn)
                roomTypeDisplayText.text = "Private";
            else
                roomTypeDisplayText.text = "Public";
        }
    }

    public void ShowLoadingPanel(bool isShow)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(isShow);
        }

        if (isShow && loadingTimeText != null)
        {
            loadingTimeText.text = "Connecting: 0.0s...";
        }
    }

    public void UpdateLoadingTime(float timeElapsed)
    {
        if (loadingTimeText != null)
        {
            loadingTimeText.text = $"Connecting: {timeElapsed:F1}s...";
        }
    }
}