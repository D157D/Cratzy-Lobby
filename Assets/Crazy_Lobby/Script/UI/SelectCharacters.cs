using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Crazy_Lobby.UI
{
    public class SelectCharacterUI : MonoBehaviour
    {
        public CharacterDatabase database;
        public Transform buttonContainer;
        public GameObject characterButtonPrefab;
        public Button confirmButton;
        public TextMeshProUGUI confirmButtonText;

        public TextMeshProUGUI selectedCharacterName;
        public Image selectedCharacterIcon;

        public Color normalColor = new Color(0.2f, 0.2f, 0.3f, 0.8f);
        public Color selectedColor = new Color(0.3f, 0.7f, 1f, 1f);
        public Color hoverColor = new Color(0.4f, 0.5f, 0.8f, 0.9f);

        private CharacterType _selectedType = CharacterType.Mage;
        private PlayerCharacterHandler _localHandler;
        private readonly List<CharacterButtonSlot> _slots = new List<CharacterButtonSlot>();

        private void OnEnable()
        {
            PlayerCharacterHandler.OnLocalPlayerSpawned += OnLocalPlayerReady;
            PlayerCharacterHandler.OnAnyCharacterChanged += OnAnyCharacterChanged;
        }

        private void OnDisable()
        {
            PlayerCharacterHandler.OnLocalPlayerSpawned -= OnLocalPlayerReady;
            PlayerCharacterHandler.OnAnyCharacterChanged -= OnAnyCharacterChanged;
        }

        private void Start()
        {
            if (database == null)
                database = FindObjectOfType<CharacterDatabase>();

            // Load nhân vật đã lưu từ lần chọn trước
            _selectedType = CharacterSaveManager.Load(CharacterType.Mage);

            BuildButtons();
            UpdateSelection(_selectedType);

            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        private void BuildButtons()
        {
            if (database == null || characterButtonPrefab == null || buttonContainer == null)
            {
                Debug.LogWarning("[SelectCharacterUI] Thiếu reference.");
                return;
            }

            foreach (Transform child in buttonContainer)
            {
                Destroy(child.gameObject);
            }
            _slots.Clear();

            var entries = database.GetAllEntries();
            if (entries == null) return;

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                var btnObj = Instantiate(characterButtonPrefab, buttonContainer);
                btnObj.name = $"Btn_{entry.Type}";

                var slot = new CharacterButtonSlot();
                slot.Type = entry.Type;
                slot.ButtonObject = btnObj;
                slot.Button = btnObj.GetComponent<Button>();
                slot.Background = btnObj.GetComponent<Image>();

                // Tìm Image để gán icon
                var iconTransform = btnObj.transform.Find("Icon");
                if (iconTransform != null)
                {
                    slot.Icon = iconTransform.GetComponent<Image>();
                }
                else
                {
                    foreach (Transform child in btnObj.transform)
                    {
                        var img = child.GetComponent<Image>();
                        if (img != null)
                        {
                            slot.Icon = img;
                            break;
                        }
                    }
                    if (slot.Icon == null)
                        slot.Icon = slot.Background;
                }

                slot.NameText = btnObj.GetComponentInChildren<TextMeshProUGUI>();

                if (slot.Icon != null && entry.Icon != null)
                    slot.Icon.sprite = entry.Icon;

                string displayName = !string.IsNullOrEmpty(entry.DisplayName) ? entry.DisplayName : entry.Type.ToString();
                if (slot.NameText != null)
                    slot.NameText.text = displayName;

                if (slot.Background != null)
                    slot.Background.color = normalColor;

                CharacterType capturedType = entry.Type;
                if (slot.Button != null)
                {
                    slot.Button.onClick.AddListener(() => OnCharacterButtonClicked(capturedType));
                    AddHoverEffect(slot);
                }

                _slots.Add(slot);
            }
        }

        private void OnCharacterButtonClicked(CharacterType type)
        {
            _selectedType = type;
            UpdateSelection(type);
        }

        private void OnConfirmClicked()
        {
            CharacterSaveManager.Save(_selectedType);

            if (_localHandler == null)
            {
                Debug.LogWarning("[SelectCharacterUI] Player chưa spawn, đã lưu lựa chọn.");
                return;
            }

            _localHandler.RequestChangeCharacter(_selectedType);
            Debug.Log($"[SelectCharacterUI] Xác nhận chọn nhân vật: {_selectedType}");
        }

        private void OnLocalPlayerReady(PlayerCharacterHandler handler)
        {
            _localHandler = handler;

            if (handler.CurrentCharacter != default)
            {
                _selectedType = handler.CurrentCharacter;
            }
            UpdateSelection(_selectedType);
        }

        private void OnAnyCharacterChanged(PlayerCharacterHandler handler)
        {
            if (handler == _localHandler)
            {
                _selectedType = handler.CurrentCharacter;
                UpdateSelection(handler.CurrentCharacter);
            }
        }

        private void UpdateSelection(CharacterType selected)
        {
            foreach (var slot in _slots)
            {
                bool isSelected = slot.Type == selected;

                if (slot.Background != null)
                    slot.Background.color = isSelected ? selectedColor : normalColor;

                slot.ButtonObject.transform.localScale = isSelected ? Vector3.one * 1.1f : Vector3.one;
            }

            if (database != null)
            {
                var entry = database.GetEntry(selected);
                string displayName = !string.IsNullOrEmpty(entry.DisplayName) ? entry.DisplayName : selected.ToString();

                if (selectedCharacterName != null)
                    selectedCharacterName.text = displayName;

                if (selectedCharacterIcon != null && entry.Icon != null)
                    selectedCharacterIcon.sprite = entry.Icon;
            }
        }

        private void AddHoverEffect(CharacterButtonSlot slot)
        {
            var trigger = slot.ButtonObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null)
                trigger = slot.ButtonObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var entryEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data) =>
            {
                if (slot.Type != _selectedType)
                {
                    if (slot.Background != null)
                        slot.Background.color = hoverColor;
                    slot.ButtonObject.transform.localScale = Vector3.one * 1.05f;
                }
            });
            trigger.triggers.Add(entryEnter);

            var entryExit = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data) =>
            {
                bool isSelected = slot.Type == _selectedType;
                if (slot.Background != null)
                    slot.Background.color = isSelected ? selectedColor : normalColor;
                slot.ButtonObject.transform.localScale = isSelected ? Vector3.one * 1.1f : Vector3.one;
            });
            trigger.triggers.Add(entryExit);
        }

        private class CharacterButtonSlot
        {
            public CharacterType Type;
            public GameObject ButtonObject;
            public Button Button;
            public Image Background;
            public Image Icon;
            public TextMeshProUGUI NameText;
        }
    }
}