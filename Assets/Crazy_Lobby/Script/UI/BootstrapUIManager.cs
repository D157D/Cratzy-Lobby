using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BootstrapUIManager : MonoBehaviour
{
    public static BootstrapUIManager Instance;
    public GameObject IngamePanel;
    public Button btnQuickJoin;
    public Button btnCreateRoom;
    public Button btnJoinByID;
    public Button playButton;
    public TMP_InputField roomIDInput;
    [SerializeField] private GameObject loadingPanel; 
    [SerializeField] private TextMeshProUGUI loadingTimeText;
    public TextMeshProUGUI roomIdDisplayText; 
    private string gameSceneName = "Map";

    private Bootstrap _bootstrap;

    void Awake() 
    { 
        if(Instance == null) { Instance = this; }
    }

    private void Start()
    {
        _bootstrap = FindObjectOfType<Bootstrap>();
        if (_bootstrap == null)
        {
            Debug.LogError("Bootstrap component not found in scene. Please add it.", this);
            return;
        }

        // Lắng nghe sự kiện từ các nút bấm mới
        if (btnQuickJoin != null) btnQuickJoin.onClick.AddListener(OnQuickJoinClicked);
        if (btnCreateRoom != null) btnCreateRoom.onClick.AddListener(OnCreateRoomClicked);
        if (btnJoinByID != null) btnJoinByID.onClick.AddListener(OnJoinByIDClicked);
        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);

        ShowConnectionUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (playButton != null && playButton.gameObject.activeInHierarchy)
            {
                OnPlayClicked();
            }
        }
    }

    private void OnPlayClicked()
    {
        _bootstrap.OnPlayClicked(gameSceneName);
    }

    // --- CÁC HÀM XỬ LÝ UI TƯƠNG TÁC VỚI BOOTSTRAP ---

    private void OnQuickJoinClicked()
    {
        // Chơi nhanh: AutoHostOrClient sẽ tự tìm phòng trống, nếu không có tự tạo phòng ngẫu nhiên
        _bootstrap.StartRoom(GameMode.AutoHostOrClient, string.Empty);
    }

    private void OnCreateRoomClicked()
    {
        // Tạo phòng: Lấy ID từ InputField. Nếu InputField trống, tạo 1 ID ngẫu nhiên.
        string roomID = roomIDInput != null && !string.IsNullOrEmpty(roomIDInput.text) 
                        ? roomIDInput.text 
                        : "Room_" + Random.Range(1000, 9999);
                        
        _bootstrap.StartRoom(GameMode.Host, roomID);
    }

    private void OnJoinByIDClicked()
    {
        string roomID = roomIDInput != null ? roomIDInput.text : "";
        
        if (string.IsNullOrEmpty(roomID))
        {
            Debug.LogWarning("Vui lòng nhập ID phòng để Join!");
            return;
        }
        
        _bootstrap.StartRoom(GameMode.Client, roomID);
    }

    // --- CÁC HÀM ĐIỀU KHIỂN GIAO DIỆN ---

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
        if (IngamePanel != null) IngamePanel.SetActive(false);
    }

    public void ShowLoadingPanel(bool isShow)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(isShow);
        }
        
        // Reset text khi bắt đầu hiện
        if (isShow && loadingTimeText != null) 
        {
            loadingTimeText.text = "Đang kết nối: 0.0s...";
        }
    }

    public void UpdateLoadingTime(float timeElapsed)
    {
        if (loadingTimeText != null)
        {
            // Hiển thị thời gian với 1 chữ số thập phân
            loadingTimeText.text = $"Đang kết nối: {timeElapsed:F1}s...";
        }
    }
}