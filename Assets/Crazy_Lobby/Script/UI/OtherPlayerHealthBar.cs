using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class OtherPlayerHealthBar : NetworkBehaviour
{
    public Image _healthBarFill; 
    public GameObject canvasObject;
    
    private PlayerHealth _playerHealth;
    private Transform _mainCameraTransform;

    public override void Spawned()
    {
        _playerHealth = GetComponentInParent<PlayerHealth>();

        if (Object.HasInputAuthority || Runner.Mode == SimulationModes.Server)
        {
            canvasObject.SetActive(false);
            return;
        }

        canvasObject.SetActive(true);

        _playerHealth.OnHealthUpdated += UpdateHealthUI;

        var currentStats = _playerHealth.playerHealthStruct;
        UpdateHealthUI(currentStats.currentHealth, currentStats.maxHealth);

        if (Camera.main != null) _mainCameraTransform = Camera.main.transform;
    }

    private void UpdateHealthUI(int currentHp, int maxHp)
    {
        if (_healthBarFill == null)
        {
            return;
        }

        if (maxHp <= 0) return; 

        _healthBarFill.fillAmount = (float)currentHp / maxHp;
    }

    public override void Render()
    {
        if (_mainCameraTransform == null)
        {
            if (Camera.main != null) _mainCameraTransform = Camera.main.transform;
            return;
        }

        transform.rotation = _mainCameraTransform.rotation;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthUpdated -= UpdateHealthUI;
        }
    }
}