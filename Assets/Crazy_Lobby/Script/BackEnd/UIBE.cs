using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Register")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public Button RegisterButton;

    [Header("Login")]

    public TMP_InputField loginUsernameInput;
    public TMP_InputField loginPasswordInput;
    public Button LoginButton;
    
    [Header("Debug")]
    public TextMeshProUGUI ResgiterDebug;
    public TextMeshProUGUI LoginDebug;

    void Start()
    {
        RegisterButton.onClick.AddListener(OnRegisterButtonClicked);
        LoginButton.onClick.AddListener(OnLoginButtonClicked);
        // BackendManager.OnForcedLogout += HandleForcedLogout;
    }

    private void OnDestroy()
    {
        // Huỷ đăng ký sự kiện để tránh lỗi
        // BackendManager.OnForcedLogout -= HandleForcedLogout;
    }

    void OnRegisterButtonClicked()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
            if (ResgiterDebug != null) ResgiterDebug.text = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";
            return;
        }

        if (username.Length < 5)
        {
            Debug.LogWarning("Tên đăng nhập phải có ít nhất 5 ký tự.");
            if (ResgiterDebug != null) ResgiterDebug.text = "Tên đăng nhập phải có ít nhất 5 ký tự.";
            return;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(username, "^[a-zA-Z0-9]+$"))
        {
            Debug.LogWarning("Tên đăng nhập không được chứa ký tự đặc biệt.");
            if (ResgiterDebug != null) ResgiterDebug.text = "Tên đăng nhập không được chứa ký tự đặc biệt.";
            return;
        }

        if (ResgiterDebug != null) ResgiterDebug.text = "Đang xử lý đăng ký...";
        BackendManager.Instance.Register(username, password, (isSuccess, message) => 
        {
            if (ResgiterDebug != null) ResgiterDebug.text = message;
        });
    }

    void OnLoginButtonClicked()
    {
        string username = loginUsernameInput.text;
        string password = loginPasswordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
            if (LoginDebug != null) LoginDebug.text = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";
            return;
        }

        if (LoginDebug != null) LoginDebug.text = "Đang xử lý đăng nhập...";
        BackendManager.Instance.Login(username, password, (isSuccess, message) => 
        {
            if (LoginDebug != null) LoginDebug.text = message;
        });
    }

    void HandleForcedLogout()
    {
        // Hàm này được gọi khi token không còn hợp lệ (ví dụ: đăng nhập từ thiết bị khác)
        Debug.LogWarning("Tài khoản đã được đăng nhập ở một nơi khác. Quay về màn hình đăng nhập.");
        // Tại đây, bạn sẽ triển khai logic để hiển thị lại màn hình đăng nhập
        // Ví dụ: loginPanel.SetActive(true); lobbyPanel.SetActive(false);
    }
}