using System.Collections;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

namespace Crazy_Lobby.UI
{
    public class Menu : NetworkBehaviour
    {
        [Header("UI Buttons")]
        public Button _StartButton;
        public Button _LoginButton;
        public Button _RegisterButton;
        public Button _Chose_register;
        public Button _Chose_login;

        [Header("UI Elements")]
        public RectTransform _GameName; 
        public TMP_InputField _username_login; 
        public TMP_InputField _pass_login;
        public TMP_InputField _username_register;
        public TMP_InputField _pass_register;
        public GameObject _login_panel;
        public GameObject _register_panel;
        
        [Header("Room Panel (Room Management)")]
        public GameObject _CrazyLobby;
        public GameObject _room_panel;

        [Header("Animation Settings")]
        private Vector2 gameNameTargetPos = new Vector2(0, 300); 
        private Vector3 gameNameTargetScale = new Vector3(0.5f, 0.5f, 1f); 
        private float animationDuration = 1f;
        
        [Header("Panel Transition")]
        private float panelTransitionDuration = 0.3f; 
        private bool isTransitioning = false; 

        private void Start()
        {
            if (_login_panel != null) _login_panel.SetActive(false);
            if (_register_panel != null) _register_panel.SetActive(false);
            if (_room_panel != null) _room_panel.SetActive(false);
            if (_Chose_login != null) _Chose_login.gameObject.SetActive(false);
            if (_Chose_register != null) _Chose_register.gameObject.SetActive(false);
            if (_StartButton != null) _StartButton.onClick.AddListener(StartGame);
            if (_Chose_register != null) _Chose_register.onClick.AddListener(ShowRegisterPanel);
            if (_Chose_login != null) _Chose_login.onClick.AddListener(ShowLoginPanel);
            if (_LoginButton != null) _LoginButton.onClick.AddListener(OnLoginClicked);
            if (_RegisterButton != null) _RegisterButton.onClick.AddListener(OnRegisterClicked);
          

            
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (_StartButton != null && _StartButton.gameObject.activeInHierarchy)
                {
                    StartGame();
                }
                else if (_login_panel != null && _login_panel.activeInHierarchy)
                {
                    OnLoginClicked();
                }
                else if (_register_panel != null && _register_panel.activeInHierarchy)
                {
                    OnRegisterClicked();
                }
            }
        }

        public void StartGame()
        {
            if (_StartButton != null) _StartButton.gameObject.SetActive(false);
            
            /* Tạm thời comment phần tự động đăng nhập
            string savedUser = PlayerPrefs.GetString("SavedUsername", "");
            string savedPass = PlayerPrefs.GetString("SavedPassword", "");

            if (!string.IsNullOrEmpty(savedUser) && !string.IsNullOrEmpty(savedPass))
            {
                if (_GameName != null)
                {
                    _GameName.anchoredPosition = gameNameTargetPos;
                    _GameName.localScale = gameNameTargetScale;
                }

                // // Điền sẵn thông tin và hiện bảng đăng nhập lên trong lúc chờ Server phản hồi
                // if (_username_login != null) _username_login.text = savedUser;
                // if (_pass_login != null) _pass_login.text = savedPass;
                // ShowLoginPanel();

                Debug.Log("Phát hiện tài khoản đã lưu, đang tự động đăng nhập...");
                BackendManager.Instance.Login(savedUser, savedPass, (isSuccess, message) => 
                {
                    if (isSuccess) ShowRoomPanel();
                    else ShowLoginPanel();
                });
            }
            else
            */
            {
                // Chưa có tài khoản lưu sẵn, chạy hoạt ảnh kéo tên game lên và gọi ShowLoginPanel()
                StartCoroutine(AnimateGameName());
            }
        }

        private IEnumerator AnimateGameName()
        {
            if (_GameName == null) yield break;

            Vector2 initialPos = _GameName.anchoredPosition;
            Vector3 initialScale = _GameName.localScale;
            float elapsedTime = 0f;

            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsedTime / animationDuration);

                _GameName.anchoredPosition = Vector2.Lerp(initialPos, gameNameTargetPos, t);
                _GameName.localScale = Vector3.Lerp(initialScale, gameNameTargetScale, t);

                yield return null;
            }

            _GameName.anchoredPosition = gameNameTargetPos;
            _GameName.localScale = gameNameTargetScale;

            ShowLoginPanel();
        }
        public void ShowLoginPanel()
        {
            if (isTransitioning || (_login_panel != null && _login_panel.activeSelf)) return;
            
            if (_Chose_login != null) _Chose_login.gameObject.SetActive(false);
            if (_Chose_register != null) _Chose_register.gameObject.SetActive(true);

            StartCoroutine(TransitionPanels(_register_panel, _login_panel));
        }

        public void ShowRegisterPanel()
        {
            if (isTransitioning || (_register_panel != null && _register_panel.activeSelf)) return;
            
            if (_Chose_login != null) _Chose_login.gameObject.SetActive(true);
            if (_Chose_register != null) _Chose_register.gameObject.SetActive(false);

            StartCoroutine(TransitionPanels(_login_panel, _register_panel));
        }

        private IEnumerator TransitionPanels(GameObject fromPanel, GameObject toPanel)
        {
            isTransitioning = true;

            if (fromPanel != null && fromPanel.activeSelf)
            {
                CanvasGroup fromCG = GetOrAddCanvasGroup(fromPanel);
                fromCG.interactable = false;
                fromCG.blocksRaycasts = false;

                float elapsed = 0f;
                while (elapsed < panelTransitionDuration)
                {
                    elapsed += Time.deltaTime;
                    fromCG.alpha = Mathf.Lerp(1f, 0f, elapsed / panelTransitionDuration);
                    yield return null;
                }
                fromPanel.SetActive(false);
            }

            if (toPanel != null)
            {
                toPanel.SetActive(true);
                CanvasGroup toCG = GetOrAddCanvasGroup(toPanel);
                toCG.alpha = 0f;

                float elapsed = 0f;
                while (elapsed < panelTransitionDuration)
                {
                    elapsed += Time.deltaTime;
                    toCG.alpha = Mathf.Lerp(0f, 1f, elapsed / panelTransitionDuration);
                    yield return null;
                }
                toCG.alpha = 1f;
                toCG.interactable = true;
                toCG.blocksRaycasts = true;
            }

            isTransitioning = false;
        }

        private CanvasGroup GetOrAddCanvasGroup(GameObject panel)
        {
            if (panel == null) return null;
            CanvasGroup cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();
            return cg;
        }

        private void OnLoginClicked()
        {
            string username = _username_login != null ? _username_login.text : "";
            string password = _pass_login != null ? _pass_login.text : "";

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Debug.LogWarning("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
                return;
            }

            if (BackendManager.Instance == null)
            {
                Debug.LogError("Lỗi: Không tìm thấy BackendManager trong Scene. Vui lòng tạo một GameObject và gắn script BackendManager vào.");
                return;
            }

            BackendManager.Instance.Login(username, password, (isSuccess, message) => 
            {
                Debug.Log(message);
                if (isSuccess)
                {
                    PlayerPrefs.SetString("SavedUsername", username);
                    PlayerPrefs.SetString("SavedPassword", password);
                    PlayerPrefs.Save();
                    ShowRoomPanel();
                }
            });
        }

        private void OnRegisterClicked()
        {
            string username = _username_register != null ? _username_register.text : "";
            string password = _pass_register != null ? _pass_register.text : "";

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Debug.LogWarning("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
                return;
            }

            if (BackendManager.Instance == null)
            {
                Debug.LogError("Lỗi: Không tìm thấy BackendManager trong Scene. Vui lòng tạo một GameObject và gắn script BackendManager vào.");
                return;
            }

            Debug.Log("Đang gửi yêu cầu đăng ký...");
            BackendManager.Instance.Register(username, password, (isSuccess, message) => 
            {
                Debug.Log(message);
                if (isSuccess)
                {
                    ShowLoginPanel(); 
                }
            });
        }

        public void ShowRoomPanel()
        {
            if (_login_panel != null) _login_panel.SetActive(false);
            if (_register_panel != null) _register_panel.SetActive(false);
            if (_Chose_login != null) _Chose_login.gameObject.SetActive(false);
            if (_Chose_register != null) _Chose_register.gameObject.SetActive(false);
            if (_room_panel != null) _room_panel.SetActive(true);
            
            if (_GameName != null) _GameName.gameObject.SetActive(false); 
        }

        
    }
}