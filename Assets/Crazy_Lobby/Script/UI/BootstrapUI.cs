using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class BootstrapUI : FusionBootstrap
{
    public GameObject networkPanel;
    public Button hostButton;
    public Button clientButton;
    public Button serverButton;
    public GameObject lobbyPanel;
    public Button playButton;
    public string gameSceneName = "Map"; 
    private NetworkRunner _runner;

    private void Start()
    {
        // Gắn sự kiện (Listener) cho các nút trên Canvas UI
        if (hostButton != null) hostButton.onClick.AddListener(() => StartRoom(GameMode.Host));
        if (clientButton != null) clientButton.onClick.AddListener(() => StartRoom(GameMode.Client));
        if (serverButton != null) serverButton.onClick.AddListener(() => StartRoom(GameMode.Server));

        // Gắn sự kiện cho nút Play
        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);

        // Đảm bảo Panel UI kết nối được bật, và ẩn Panel sảnh chờ khi bắt đầu
        if (networkPanel != null) networkPanel.SetActive(true);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
    }

    private void Update()
    {
        // Nhấn Enter để bắt đầu game nếu đang ở trong Lobby và có quyền (nút Play hiện)
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (playButton != null && playButton.gameObject.activeInHierarchy)
            {
                OnPlayClicked();
            }
        }
    }

    private async void StartRoom(GameMode mode)
    {
        // Ẩn UI kết nối ngay khi bấm
        if (networkPanel != null) networkPanel.SetActive(false);

        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
        }

        // Thêm NetworkSceneManagerDefault để có thể đồng bộ Scene cho tất cả người chơi
        var sceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>();
        if (sceneManager == null) sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        var args = new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "Room_1234", // Nếu có ô nhập ID, bạn có thể truyền ID vào đây
            SceneManager = sceneManager,
            // Đặt scene hiện tại để các Client khi tham gia không bị load lại, chờ ở sảnh
            Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex)
        };

        var result = await _runner.StartGame(args);

        if (result.Ok)
        {
            Debug.Log($"<color=green>Vào phòng thành công! Mode: {mode}</color>");
            
            if (lobbyPanel != null) lobbyPanel.SetActive(true);
            
            // Nếu là Client thì ẩn nút Play đi, chỉ Host mới có quyền bấm
            if (playButton != null) playButton.gameObject.SetActive(_runner.IsServer);
        }
        else
        {
            Debug.LogError($"Kết nối thất bại: {result.ShutdownReason}");
            if (networkPanel != null) networkPanel.SetActive(true);
        }
    }

    private void OnPlayClicked()
    {
        

        if (_runner != null && _runner.IsServer)
        {
            // (Tùy chọn) Ẩn phòng không cho ai vào thêm khi game đã bắt đầu
            _runner.SessionInfo.IsOpen = false;
            _runner.SessionInfo.IsVisible = false;

            int buildIndex = UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath(gameSceneName);
            if (buildIndex >= 0)
            {
                // Sử dụng Runner.LoadScene để ép tất cả Client đang ở Lobby chuyển sang Scene Game
                _runner.LoadScene(SceneRef.FromIndex(buildIndex));
            }
            else
            {
                Debug.LogError($"[BootstrapUI] Không tìm thấy Scene '{gameSceneName}'. Vui lòng thêm vào Build Settings!");
            }
        }
    }
}
