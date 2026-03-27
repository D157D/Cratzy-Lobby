using Fusion;
using UnityEngine;

public class Bootstrap : MonoBehaviour 
{
    private BootstrapUIManager _uiManager;
    private NetworkRunner _runner;

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        Application.targetFrameRate = 60;

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

        var sceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>();
        if (sceneManager == null) sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        var args = new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "Room_1234",
            SceneManager = sceneManager,
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
            _runner.SessionInfo.IsOpen = false;
            _runner.SessionInfo.IsVisible = false;

            int buildIndex = UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath(gameSceneName);
            if (buildIndex >= 0)
            {
                _runner.LoadScene(SceneRef.FromIndex(buildIndex));
            }
            else
            {
                Debug.LogError($"[BootstrapUI] Không tìm thấy Scene '{gameSceneName}'. Vui lòng thêm vào Build Settings!");
            }
        }
    }
}