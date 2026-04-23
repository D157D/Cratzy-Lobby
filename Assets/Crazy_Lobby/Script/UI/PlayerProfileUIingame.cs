using Fusion;
using UnityEngine;
using TMPro;

namespace CrazyLobby.UI
{

    public class PlayerProfileUIingame : NetworkBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI nameText;

        public GameObject nameCanvasObject;

        [Header("Settings")]
        public bool isAIEnemy = false;

        [Networked]
        private NetworkString<_32> AIName { get; set; }

        private Transform _mainCameraTransform;

        private static readonly string[] RandomAINames = new string[]
        {
            "Shadow", "Blaze", "Frost", "Venom", "Storm",
            "Phantom", "Spike", "Crimson", "Thunder", "Nova",
            "Jinx", "Raven", "Fury", "Clash", "Drift",
            "Turbo", "Neon", "Bolt", "Havoc", "Glitch",
            "Pixel", "Chaos", "Flash", "Onyx", "Viper",
            "Blitz", "Ember", "Fang", "Hex", "Dash"
        };

        public override void Spawned()
        {
            if (isAIEnemy)
            {
                if (HasStateAuthority)
                {
                    AIName = RandomAINames[Random.Range(0, RandomAINames.Length)];
                }

                if (nameCanvasObject != null) nameCanvasObject.SetActive(true);
                UpdateNameDisplay();

                if (Camera.main != null) _mainCameraTransform = Camera.main.transform;
                return;
            }


            if (Object.HasInputAuthority)
            {
                if (nameCanvasObject != null) nameCanvasObject.SetActive(false);
                return;
            }

            if (nameCanvasObject != null) nameCanvasObject.SetActive(true);
            UpdateNameDisplay();

            if (Camera.main != null) _mainCameraTransform = Camera.main.transform;
        }

        public override void Render()
        {
            if (_mainCameraTransform == null)
            {
                if (Camera.main != null) _mainCameraTransform = Camera.main.transform;
                return;
            }

            if (nameCanvasObject != null)
            {
                nameCanvasObject.transform.rotation = _mainCameraTransform.rotation;
            }

            UpdateNameDisplay();
        }

        private void UpdateNameDisplay()
        {
            if (nameText == null) return;

            if (isAIEnemy)
            {
                string aiName = AIName.ToString();
                if (!string.IsNullOrEmpty(aiName))
                {
                    string displayName = $"{aiName}";
                    if (nameText.text != displayName)
                    {
                        nameText.text = displayName;
                    }
                }
                else
                {
                    nameText.text = "...";
                }
            }
            else
            {
                var playerNameUI = GetComponentInParent<Crazy_Lobby.UI.PlayerNameUI>();
                if (playerNameUI == null)
                {
                    playerNameUI = GetComponent<Crazy_Lobby.UI.PlayerNameUI>();
                }

                if (playerNameUI != null)
                {
                    string playerName = playerNameUI.PlayerName.ToString();
                    if (!string.IsNullOrEmpty(playerName))
                    {
                        if (nameText.text != playerName)
                        {
                            nameText.text = playerName;
                        }
                    }
                    else
                    {
                        nameText.text = "...";
                    }
                }
            }
        }
    }
}