using UnityEngine;
using UnityEngine.UI;

public class UIButtonSoundApplier : MonoBehaviour
{
    [SerializeField] private AudioClip clickSfx;
    public static UIButtonSoundApplier Instance;
    void Awake()
    {
        Instance = this;
    }
    private void OnEnable()
    {
        ApplySoundToAllButtons();
    }

    private void ApplySoundToAllButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        foreach (Button btn in buttons)
        {
            btn.onClick.RemoveListener(PlayClickSound);
            btn.onClick.AddListener(PlayClickSound);
        }
    }

    public void PlayClickSound()
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.PlaySFX(clickSfx, transform.position);
    }
}
