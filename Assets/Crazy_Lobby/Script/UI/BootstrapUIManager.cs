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
    public TextMeshProUGUI IDText;
    public TextMeshProUGUI roomIdDisplayText;

    [SerializeField] private GameObject loadingPanel; 
    [SerializeField] private TextMeshProUGUI loadingTimeText;

    private string gameSceneName = "Map2";
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
    }

    private void OnPlayClicked()
    {
        if (_bootstrap == null) return;
        _bootstrap.OnPlayClicked(gameSceneName);
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
        _bootstrap.StartRoom(GameMode.AutoHostOrClient, roomID);
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
        _bootstrap.StartRoom(GameMode.Host, roomID);
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
        _bootstrap.StartRoom(GameMode.Client, roomID);
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
    }

    public void ShowLoadingPanel(bool isShow)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(isShow);
        }

        if (isShow && loadingTimeText != null)
        {
            loadingTimeText.text = "Đang kết nối: 0.0s...";
        }
    }

    public void UpdateLoadingTime(float timeElapsed)
    {
        if (loadingTimeText != null)
        {
            loadingTimeText.text = $"Đang kết nối: {timeElapsed:F1}s...";
        }
    }
}