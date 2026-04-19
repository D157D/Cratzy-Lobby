using System.Collections;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        
        [Header("Panels")]
        public GameObject _login_panel;
        public GameObject _register_panel;
        public GameObject _CrazyLobby;
        public GameObject _room_panel;

        [Header("Animation Settings")]
        private Vector2 gameNameTargetPos = new Vector2(0, 300); 
        private Vector3 gameNameTargetScale = new Vector3(0.5f, 0.5f, 1f); 
        private float animationDuration = 1f;
        
        [Header("Panel Transition")]
        private float panelTransitionDuration = 0.3f; 
        private bool isTransitioning = false; 

        private static bool _hasStartedSession = false;
        public static bool ShouldAutoStart = false;

        private void Start()
        {
            SetActiveMultiple(false, _login_panel, _register_panel, _room_panel, _Chose_login?.gameObject, _Chose_register?.gameObject);
            
            _StartButton?.onClick.AddListener(StartGame);
            _Chose_register?.onClick.AddListener(ShowRegisterPanel);
            _Chose_login?.onClick.AddListener(ShowLoginPanel);
            _LoginButton?.onClick.AddListener(OnLoginClicked);
            _RegisterButton?.onClick.AddListener(OnRegisterClicked);

            if (_hasStartedSession)
            {
                if (_StartButton != null) _StartButton.gameObject.SetActive(false);
                ShowRoomPanel(); 
            }
            else if (ShouldAutoStart)
            {
                ShouldAutoStart = false;
                StartGame();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (_StartButton != null && _StartButton.gameObject.activeInHierarchy) StartGame();
                else if (_login_panel != null && _login_panel.activeInHierarchy) OnLoginClicked();
                else if (_register_panel != null && _register_panel.activeInHierarchy) OnRegisterClicked();
            }
        }

        public void StartGame()
        {
            if (_StartButton != null) _StartButton.gameObject.SetActive(false);
            
            // Đánh dấu là đã bấm Start trong phiên chơi này
            _hasStartedSession = true; 
            
            string savedUser = PlayerPrefs.GetString("SavedUsername", "");
            string savedPass = PlayerPrefs.GetString("SavedPassword", "");

            if (!string.IsNullOrEmpty(savedUser) && !string.IsNullOrEmpty(savedPass))
            {
                if (_GameName != null)
                {
                    _GameName.anchoredPosition = gameNameTargetPos;
                    _GameName.localScale = gameNameTargetScale;
                }

                Debug.Log("Phát hiện tài khoản đã lưu, đang tự động đăng nhập...");
                if (BackendManager.Instance != null)
                {
                    BackendManager.Instance.Login(savedUser, savedPass, (isSuccess, message) => 
                    {
                        if (isSuccess) ShowRoomPanel();
                        else ShowLoginPanel();
                    });
                }
            }
            else
            {
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
            
            SetActiveMultiple(false, _Chose_login?.gameObject);
            SetActiveMultiple(true, _Chose_register?.gameObject);

            StartCoroutine(TransitionPanels(_register_panel, _login_panel));
        }

        public void ShowRegisterPanel()
        {
            if (isTransitioning || (_register_panel != null && _register_panel.activeSelf)) return;
            
            SetActiveMultiple(true, _Chose_login?.gameObject);
            SetActiveMultiple(false, _Chose_register?.gameObject);

            StartCoroutine(TransitionPanels(_login_panel, _register_panel));
        }

        private IEnumerator TransitionPanels(GameObject fromPanel, GameObject toPanel)
        {
            isTransitioning = true;

            if (fromPanel != null && fromPanel.activeSelf)
            {
                yield return FadeCanvasGroup(fromPanel, 1f, 0f);
                fromPanel.SetActive(false);
            }

            if (toPanel != null)
            {
                toPanel.SetActive(true);
                yield return FadeCanvasGroup(toPanel, 0f, 1f);
            }

            isTransitioning = false;
        }

        private IEnumerator FadeCanvasGroup(GameObject panel, float startAlpha, float targetAlpha)
        {
            CanvasGroup cg = GetOrAddCanvasGroup(panel);
            
            cg.interactable = targetAlpha > 0.5f;
            cg.blocksRaycasts = targetAlpha > 0.5f;

            float elapsed = 0f;
            while (elapsed < panelTransitionDuration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / panelTransitionDuration);
                yield return null;
            }
            cg.alpha = targetAlpha;
        }

        private CanvasGroup GetOrAddCanvasGroup(GameObject panel)
        {
            if (!panel.TryGetComponent<CanvasGroup>(out var cg))
            {
                cg = panel.AddComponent<CanvasGroup>();
            }
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

            if (BackendManager.Instance == null) return;

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

            if (BackendManager.Instance == null) return;

            BackendManager.Instance.Register(username, password, (isSuccess, message) => 
            {
                Debug.Log(message);
                if (isSuccess) ShowLoginPanel(); 
            });
        }

        public void ShowRoomPanel()
        {
            SetActiveMultiple(false, _login_panel, _register_panel, _Chose_login?.gameObject, _Chose_register?.gameObject, _GameName?.gameObject);
            SetActiveMultiple(true, _room_panel);
        }

        private void SetActiveMultiple(bool state, params GameObject[] objects)
        {
            foreach (var obj in objects)
            {
                if (obj != null) obj.SetActive(state);
            }
        }
    }
}