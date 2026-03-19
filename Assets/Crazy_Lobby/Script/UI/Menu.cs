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
        public GameObject _login_panel;
        public GameObject _register_panel;
        
        [Header("Animation Settings")]
        private Vector2 gameNameTargetPos = new Vector2(0, 300); 
        private Vector3 gameNameTargetScale = new Vector3(0.5f, 0.5f, 1f); 
        private float animationDuration = 1f;

        private void Start()
        {
            if (_login_panel != null) _login_panel.SetActive(false);
            if (_register_panel != null) _register_panel.SetActive(false);
            if (_StartButton != null) _StartButton.gameObject.SetActive(true);
            if (_StartButton != null) _StartButton.onClick.AddListener(StartGame);
            if (_Chose_register != null) _Chose_register.onClick.AddListener(ShowRegisterPanel);
            if (_Chose_login != null) _Chose_login.onClick.AddListener(ShowLoginPanel);
            if (_LoginButton != null) _LoginButton.onClick.AddListener(OnLoginClicked);
            if (_RegisterButton != null) _RegisterButton.onClick.AddListener(OnRegisterClicked);
        }

        public void StartGame()
        {
            if (_StartButton != null) _StartButton.gameObject.SetActive(false);
            
            StartCoroutine(AnimateGameName());
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
            if (_register_panel != null) _register_panel.SetActive(false);
            if (_login_panel != null) _login_panel.SetActive(true);
        }

        public void ShowRegisterPanel()
        {
            if (_login_panel != null) _login_panel.SetActive(false);
            if (_register_panel != null) _register_panel.SetActive(true);
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

            Debug.Log("Đang gửi yêu cầu đăng nhập...");
            BackendManager.Instance.Login(username, password, (isSuccess, message) => 
            {
                Debug.Log(message);
                if (isSuccess)
                {
                    gameObject.SetActive(false); // Ẩn Menu UI hoặc kích hoạt logic load Lobby/Menu chính
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

            Debug.Log("Đang gửi yêu cầu đăng ký...");
            // Tách biệt logic Authentication: Gọi sang BackendManager
            BackendManager.Instance.Register(username, password, (isSuccess, message) => 
            {
                Debug.Log(message);
                if (isSuccess)
                {
                    ShowLoginPanel(); // Tự động chuyển về tab Đăng nhập khi thành công
                }
            });
        }
    }
}