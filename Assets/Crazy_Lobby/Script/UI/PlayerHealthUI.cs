using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    public Slider healthSlider;
    private PlayerHealth _localPlayerHealth;

    private void OnEnable()
    {
        PlayerHealth.OnLocalPlayerSpawned += SetupHealthUI;
        
        if (PlayerHealth.LocalPlayer != null)
        {
            SetupHealthUI(PlayerHealth.LocalPlayer);
        }
    }

    private void OnDisable()
    {
        PlayerHealth.OnLocalPlayerSpawned -= SetupHealthUI;
        if (_localPlayerHealth != null)
        {
            _localPlayerHealth.OnHealthUpdated -= UpdateHealthBar;
        }
    }

    private void SetupHealthUI(PlayerHealth spawnedHealthScript)
    {
        if (_localPlayerHealth != null)
            _localPlayerHealth.OnHealthUpdated -= UpdateHealthBar;

        _localPlayerHealth = spawnedHealthScript;
        _localPlayerHealth.OnHealthUpdated += UpdateHealthBar;

        UpdateHealthBar(
            _localPlayerHealth.playerHealthStruct.currentHealth, 
            _localPlayerHealth.playerHealthStruct.maxHealth
        );
    }

    private void UpdateHealthBar(int currentHp, int maxHp)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHp;
            healthSlider.value = currentHp;
            Debug.Log($"UI: Cập nhật máu thành {currentHp}/{maxHp}");
        }
    }
}