using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class BootstrapUIManager : MonoBehaviour
{

    public static BootstrapUIManager Instance;
    [Header("UI Panels")]
    public GameObject IngamePanel;

    [Header("UI Buttons")]
    public Button hostButton;
    public Button clientButton;
    public Button serverButton;
    public Button playButton;

    [Header("Settings")]
    public string gameSceneName = "Map";

    
    private Bootstrap _bootstrap;
    void Awake() { if(Instance == null) { Instance = this; }}
    private void Start()
    {
        _bootstrap = FindObjectOfType<Bootstrap>();
        if (_bootstrap == null)
        {
            Debug.LogError("BootstrapUI component not found in scene. Please add it.", this);
            return;
        }

        if (hostButton != null) hostButton.onClick.AddListener(() => HandleStartRoom(GameMode.Host));
        if (clientButton != null) clientButton.onClick.AddListener(() => HandleStartRoom(GameMode.Client));
        if (serverButton != null) serverButton.onClick.AddListener(() => HandleStartRoom(GameMode.Server));
        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);

        ShowConnectionUI();
    }

    private void HandleStartRoom(GameMode mode)
    {
        _bootstrap.StartRoom(mode);
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

    public void ShowLobby(bool isHost)
    {
        if (IngamePanel != null) IngamePanel.SetActive(true);
        if (playButton != null) playButton.gameObject.SetActive(isHost);
    }

    public void ShowConnectionUI()
    {
        if (IngamePanel != null) IngamePanel.SetActive(false);
    }
}