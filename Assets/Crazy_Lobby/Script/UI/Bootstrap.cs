using Fusion;
using UnityEngine;

using Crazy_Lobby.Enemy; // Thêm namespace cho EnemySpawnManager
public class Bootstrap : MonoBehaviour 
{
    private BootstrapUIManager _uiManager;
    private NetworkRunner _runner;

    private bool _isConnecting = false;
    private float _connectionStartTime = 0f;

    [Header("Prefabs")]
    public NetworkPrefabRef enemySpawnManagerPrefab; // Kéo prefab EnemySpawnManager vào đây

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

    private void Update()
    {
        if (_isConnecting && _uiManager != null)
        {
            float timeElapsed = Time.time - _connectionStartTime;
            _uiManager.UpdateLoadingTime(timeElapsed);
        }
    }

    public async void StartRoom(GameMode mode, string sessionName, bool isPrivate)
    {
        if (_runner != null)
        {
            _runner.Shutdown();
            _runner = null;
        }

        GameObject runnerGO = new GameObject("FusionNetworkRunner");
        DontDestroyOnLoad(runnerGO);

        _runner = runnerGO.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var sceneManager = runnerGO.AddComponent<NetworkSceneManagerDefault>();

        var args = new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionName,
            IsVisible = !isPrivate, // Phòng riêng tư thì không hiển thị trong danh sách
            IsOpen = !isPrivate,     // Phòng riêng tư thì không mở để quick join
            SceneManager = sceneManager,
            Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex)
        };

        _isConnecting = true;
        _connectionStartTime = Time.time;
        
        if (_uiManager != null) 
        {
            _uiManager.ShowLoadingPanel(true); 
        }

        var result = await _runner.StartGame(args);

        _isConnecting = false;
        
        if (_uiManager != null) 
        {
            _uiManager.ShowLoadingPanel(false);
        }

        if (result.Ok)
        {
            string currentRoomId = _runner.SessionInfo.Name;

            Debug.Log($"<color=green>Vào phòng thành công! Mode: {mode} - RoomID: {currentRoomId}. Thời gian kết nối: {Time.time - _connectionStartTime:F1}s</color>");

            // Nếu là Host/Server, spawn EnemySpawnManager
            if (_runner.IsServer && enemySpawnManagerPrefab.IsValid)
            {
                NetworkObject spawnedManager = _runner.Spawn(enemySpawnManagerPrefab, Vector3.zero, Quaternion.identity, _runner.LocalPlayer);
                if (spawnedManager != null)
                {
                    spawnedManager.GetComponent<EnemySpawnManager>().IsPrivateRoom = isPrivate;
                }
            }
            
            if (_uiManager != null) _uiManager.ShowLobby(_runner.IsServer, currentRoomId);
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
                Debug.LogError($"[Bootstrap] Không tìm thấy Scene '{gameSceneName}'. Vui lòng thêm vào Build Settings!");
            }
        }
    }
}