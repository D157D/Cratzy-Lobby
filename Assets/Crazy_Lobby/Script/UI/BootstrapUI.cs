using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class BootstrapUI : FusionBootstrap
{
    [Header("Custom Game UI")]
    [Tooltip("Panel chứa các nút kết nối mạng")]
    public GameObject networkPanel;
    
    public Button hostButton;
    public Button clientButton;
    public Button serverButton;

    protected override void Start()
    {
        // Chuyển StartMode sang Manual để ẩn giao diện IMGUI mặc định của Fusion
        StartMode = StartModes.Manual;
        
        // Gọi Start của lớp cha để thực hiện các khởi tạo cơ bản (như sinh ra RunnerPrefab)
        base.Start();

        // Gắn sự kiện (Listener) cho các nút trên Canvas UI
        if (hostButton != null) hostButton.onClick.AddListener(OnHostClicked);
        if (clientButton != null) clientButton.onClick.AddListener(OnClientClicked);
        if (serverButton != null) serverButton.onClick.AddListener(OnServerClicked);

        // Đảm bảo Panel UI kết nối đang được bật khi bắt đầu
        if (networkPanel != null) networkPanel.SetActive(true);
    }

    private void OnHostClicked()
    {
        HideUI();
        StartHost(); // Gọi hàm StartHost kế thừa từ FusionBootstrap
    }

    private void OnClientClicked()
    {
        HideUI();
        StartClient(); // Gọi hàm StartClient kế thừa từ FusionBootstrap
    }

    private void OnServerClicked()
    {
        HideUI();
        StartServer(); // Gọi hàm StartServer kế thừa từ FusionBootstrap
    }

    private void HideUI()
    {
        // Ẩn panel UI sau khi người chơi đã chọn kết nối
        if (networkPanel != null) networkPanel.SetActive(false);
    }
}
