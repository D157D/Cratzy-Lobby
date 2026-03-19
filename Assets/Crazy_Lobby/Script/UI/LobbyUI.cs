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
        public string gameSceneName = "Lobby"; 
        
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
                
                UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
            }
        }
    }
}