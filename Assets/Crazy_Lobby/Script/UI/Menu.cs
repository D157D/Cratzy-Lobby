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
        
        [Header("Main Panel (Room Management)")]
        public GameObject _CrazyLobby;
        public GameObject _main_panel;
        public Button _btnQuickJoin;
        public Button _btnCreateRoom;
        public Button _btnJoinByID;
        public TMP_InputField _roomIDInput;

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
            if (_main_panel != null) _main_panel.SetActive(false);
            if (_Chose_login != null) _Chose_login.gameObject.SetActive(false);
            if (_Chose_register != null) _Chose_register.gameObject.SetActive(false);
            if (_StartButton != null) _StartButton.gameObject.SetActive(true);
            if (_StartButton != null) _StartButton.onClick.AddListener(StartGame);
            if (_Chose_register != null) _Chose_register.onClick.AddListener(ShowRegisterPanel);
            if (_Chose_login != null) _Chose_login.onClick.AddListener(ShowLoginPanel);
            if (_LoginButton != null) _LoginButton.onClick.AddListener(OnLoginClicked);
            if (_RegisterButton != null) _RegisterButton.onClick.AddListener(OnRegisterClicked);
            if (_btnQuickJoin != null) _btnQuickJoin.onClick.AddListener(OnQuickJoinClicked);
            if (_btnCreateRoom != null) _btnCreateRoom.onClick.AddListener(OnCreateRoomClicked);
            if (_btnJoinByID != null) _btnJoinByID.onClick.AddListener(OnJoinByIDClicked);
        }

        private void Update()
        {
            // Kiểm tra nếu người chơi nhấn phím Enter
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

            Debug.Log("Đang gửi yêu cầu đăng nhập...");
            BackendManager.Instance.Login(username, password, (isSuccess, message) => 
            {
                Debug.Log(message);
                if (isSuccess)
                {
                    ShowMainPanel();
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
            BackendManager.Instance.Register(username, password, (isSuccess, message) => 
            {
                Debug.Log(message);
                if (isSuccess)
                {
                    ShowLoginPanel(); 
                }
            });
        }

        public void ShowMainPanel()
        {
            if (_login_panel != null) _login_panel.SetActive(false);
            if (_register_panel != null) _register_panel.SetActive(false);
            if (_Chose_login != null) _Chose_login.gameObject.SetActive(false);
            if (_Chose_register != null) _Chose_register.gameObject.SetActive(false);
            if (_main_panel != null) _main_panel.SetActive(true);
            
            if (_GameName != null) _GameName.gameObject.SetActive(false); 
        }

        private void OnQuickJoinClicked()
        {
            // Tìm và tham gia ngẫu nhiên một phòng Public đang mở (truyền chuỗi rỗng)
            StartRoom(GameMode.Client, string.Empty); 
        }

        private void OnCreateRoomClicked()
        {
            // Lấy tên phòng từ ô nhập, nếu không nhập thì tạo ngẫu nhiên
            string roomID = _roomIDInput != null && !string.IsNullOrEmpty(_roomIDInput.text) 
                            ? _roomIDInput.text 
                            : "Room_" + Random.Range(1000, 9999);
            StartRoom(GameMode.Host, roomID);
        }

        private void OnJoinByIDClicked()
        {
            string roomID = _roomIDInput != null ? _roomIDInput.text : "";
            if (string.IsNullOrEmpty(roomID))
            {
                Debug.LogWarning("Vui lòng nhập ID phòng để Join!");
                return;
            }
            StartRoom(GameMode.Client, roomID);
        }

        private async void StartRoom(GameMode mode, string roomID)
        {
            
            NetworkRunner runner = FindObjectOfType<NetworkRunner>();
            if (runner == null) runner = gameObject.AddComponent<NetworkRunner>();

            runner.ProvideInput = true;
            
            var sceneManager = runner.gameObject.GetComponent<NetworkSceneManagerDefault>();
            if (sceneManager == null) sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

            if (string.IsNullOrEmpty(roomID))
                Debug.Log("Đang tìm phòng Public ngẫu nhiên... Vui lòng chờ.");
            else
                Debug.Log($"Đang kết nối vào phòng {roomID}... Vui lòng chờ.");

            var args = new StartGameArgs()
            {
                GameMode = mode,
                SessionName = roomID, // Gán trực tiếp roomID (nếu rỗng, Fusion tự động Matchmaking)
                SceneManager = sceneManager,
                Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex)
            };

            var result = await runner.StartGame(args);

            if (result.Ok)
            {
                Debug.Log("<color=green>Vào phòng thành công!</color>");

                gameObject.SetActive(false);
            }
            else
            {
                // Bật lại panel UI nếu quá trình tìm/tạo phòng thất bại
                if (_main_panel != null) _main_panel.SetActive(true);

                // Nếu là Quick Play mà không có phòng nào thì báo lỗi cụ thể
                if (mode == GameMode.Client && string.IsNullOrEmpty(roomID))
                {
                    Debug.LogError("<color=red>Không tìm thấy phòng Public nào đang mở! Hãy tự tạo phòng mới.</color>");
                }
                else
                {
                    Debug.LogError($"<color=red>Lỗi không thể tham gia: {result.ShutdownReason}</color>");
                }
            }
        }
    }
}