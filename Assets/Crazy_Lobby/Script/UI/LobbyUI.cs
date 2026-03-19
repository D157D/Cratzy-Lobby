using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Crazy_Lobby.UI
{
    public class LobbyUI : NetworkBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI roomIDText;
        public Button startGameButton;
        public Toggle publicPrivateToggle;
        public string gameSceneName = "Map"; 
        
        [Networked] public NetworkBool IsLocked { get; set; }

        public override void Spawned()
        {
            if (Runner.IsServer)
            {
                IsLocked = false;
                Runner.SessionInfo.IsOpen = true; 
                Runner.SessionInfo.IsVisible = true; 
            }

            if (roomIDText != null)
            {
                roomIDText.text = $"Room ID: {Runner.SessionInfo.Name}";
            }

            if (startGameButton != null)
            {
                startGameButton.gameObject.SetActive(Runner.IsServer); 
                startGameButton.onClick.AddListener(OnStartGameClicked);
            }

            if (publicPrivateToggle != null)
            {
                publicPrivateToggle.interactable = Runner.IsServer; 
                publicPrivateToggle.isOn = !IsLocked;
                publicPrivateToggle.onValueChanged.AddListener(OnLockToggleChanged);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (startGameButton != null && startGameButton.gameObject.activeInHierarchy)
                {
                    OnStartGameClicked();
                }
            }
        }

        private void OnLockToggleChanged(bool isPublic)
        {
            if (Runner.IsServer)
            {
                IsLocked = !isPublic;
                
                Runner.SessionInfo.IsOpen = isPublic; 
                Runner.SessionInfo.IsVisible = isPublic;
            }
        }

        public override void Render()
        {
            if (publicPrivateToggle != null && !Runner.IsServer)
            {
                if (publicPrivateToggle.isOn == IsLocked) publicPrivateToggle.isOn = !IsLocked;
            }
        }

        private void OnStartGameClicked()
        {
            if (Runner.IsServer)
            {
                Runner.SessionInfo.IsOpen = false; 
                Runner.SessionInfo.IsVisible = false;
                
                // Sử dụng Runner.LoadScene thay vì SceneManager mặc định để đồng bộ chuyển scene cho tất cả người chơi trong phòng
                int buildIndex = UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath(gameSceneName);
                if (buildIndex >= 0)
                {
                    Runner.LoadScene(SceneRef.FromIndex(buildIndex));
                }
                else
                {
                    Debug.LogError($"[LobbyUI] Không tìm thấy scene '{gameSceneName}' trong Build Settings. Vui lòng thêm scene vào Build Settings!");
                }
            }
        }
    }
}