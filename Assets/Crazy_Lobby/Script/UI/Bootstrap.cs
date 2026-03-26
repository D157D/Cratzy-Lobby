using Fusion;
using UnityEngine;

public class Bootstrap : FusionBootstrap
{
    private BootstrapUIManager _uiManager;
    private NetworkRunner _runner;

    private void Start()
    {
        if(BootstrapUIManager.Instance != null)
        {
            _uiManager = BootstrapUIManager.Instance;
        }
    }
    public async void StartRoom(GameMode mode)
    {
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
            if (_uiManager != null) _uiManager.ShowLobby(_runner.IsServer);
        }
        else
        {
            Debug.LogError($"Kết nối thất bại: {result.ShutdownReason}");
            if (_uiManager != null) _uiManager.ShowConnectionUI();
        }
    }

    public void OnPlayClicked(string gameSceneName)
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
