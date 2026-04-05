using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerProfileUI : MonoBehaviour
{
    [Header("UI Hiển thị")]
    public TextMeshProUGUI displayNameText; 

    [Header("UI Đổi tên")]
    public TMP_InputField changeNameInput;  
    public Button btnChangeName;            
    public TextMeshProUGUI statusText;      

    private void Start()
    {
        if (btnChangeName != null)
        {
            btnChangeName.onClick.AddListener(OnChangeNameClicked);
        }

        BackendManager.OnLoginSuccess += OnLoginSuccess;

        if (BackendManager.Instance != null && BackendManager.Instance.IsLoggedIn)
        {
            FetchProfile();
        }
    }

    private void OnDestroy()
    {
        BackendManager.OnLoginSuccess -= OnLoginSuccess;
    }

    private void OnLoginSuccess()
    {
        Debug.Log("[PlayerProfileUI] Đăng nhập thành công, bắt đầu tải profile...");
        FetchProfile();
    }

    private void FetchProfile()
    {
        if (BackendManager.Instance == null)
        {
            Debug.LogWarning("[PlayerProfileUI] BackendManager.Instance chưa sẵn sàng!");
            SetStatus("BackendManager chưa khởi tạo.", Color.red);
            return;
        }

        if (!string.IsNullOrEmpty(BackendManager.Instance.CurrentDisplayName))
        {
            UpdateNameUI(BackendManager.Instance.CurrentDisplayName);
        }

        SetStatus("Đang tải thông tin...", Color.white);

        BackendManager.Instance.GetUserProfile((isSuccess, messageOrName) =>
        {
            if (isSuccess)
            {
                SetStatus("", Color.white);
                UpdateNameUI(messageOrName);
                Debug.Log($"[PlayerProfileUI] Lấy tên thành công: {messageOrName}");
            }
            else
            {
                Debug.LogWarning($"[PlayerProfileUI] Lỗi lấy profile từ API: {messageOrName}");

                if (!string.IsNullOrEmpty(BackendManager.Instance.CurrentDisplayName))
                {
                    UpdateNameUI(BackendManager.Instance.CurrentDisplayName);
                    SetStatus("(Offline - dùng tên đã lưu)", Color.yellow);
                }
                else
                {
                    SetStatus(messageOrName, Color.red);
                }
            }
        });
    }

    private void OnChangeNameClicked()
    {
        if (BackendManager.Instance == null)
        {
            SetStatus("BackendManager chưa khởi tạo.", Color.red);
            return;
        }

        string newName = changeNameInput != null ? changeNameInput.text.Trim() : "";

        if (string.IsNullOrEmpty(newName))
        {
            SetStatus("Vui lòng nhập tên mới!", Color.yellow);
            return;
        }

        if (btnChangeName != null) btnChangeName.interactable = false;
        SetStatus("Đang đổi tên...", Color.white);

        BackendManager.Instance.UpdateDisplayName(newName, (isSuccess, message) =>
        {
            if (btnChangeName != null) btnChangeName.interactable = true;

            if (isSuccess)
            {
                SetStatus(message, Color.green);
                if (changeNameInput != null) changeNameInput.text = "";
                UpdateNameUI(newName);
            }
            else
            {
                SetStatus(message, Color.red);
            }
        });
    }

    private void UpdateNameUI(string name)
    {
        if (displayNameText != null)
        {
            displayNameText.text = name;
        }
    }

    private void SetStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
    }
}