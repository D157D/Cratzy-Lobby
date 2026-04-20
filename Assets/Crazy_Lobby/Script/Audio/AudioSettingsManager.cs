using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsManager : MonoBehaviour
{
    [Header("Slider")]
    public Slider BGM;
    public Slider SFX;

    [Header("Fill Image")]
    public Image bgmFill;
    public Image sfxFill;

    [Header("Gradient")]
    public Gradient bgmGradient;
    public Gradient sfxGradient;

    public Button applyButton;

    private float tempBgm;
    private float tempSfx;

    private void Start()
    {
        InitUI();
    }

    private void InitUI()
    {
        if (AudioManager.Instance == null) return;

        tempBgm = AudioManager.Instance.GetBgmVolume();
        tempSfx = AudioManager.Instance.GetSfxVolume();

        BGM.value = tempBgm;
        SFX.value = tempSfx;

        UpdateFillColor();

        BGM.onValueChanged.RemoveAllListeners();
        SFX.onValueChanged.RemoveAllListeners();
        // applyButton.onClick.RemoveAllListeners();

        BGM.onValueChanged.AddListener(_ => OnSliderChanged());
        SFX.onValueChanged.AddListener(_ => OnSliderChanged());
        applyButton.onClick.AddListener(Apply);
    }

    private void OnSliderChanged()
    {
        bool muted = BGM.value <= 0.001f && SFX.value <= 0.001f;

        AudioManager.Instance.UpdateVolume(BGM.value, SFX.value, muted);
        UpdateFillColor();
    }

    private void UpdateFillColor()
    {
        if (bgmFill != null)
            bgmFill.color = bgmGradient.Evaluate(BGM.value);

        if (sfxFill != null)
            sfxFill.color = sfxGradient.Evaluate(SFX.value);
    }

    private void Apply()
    {
        AudioManager.Instance.SaveSettings();
        tempBgm = BGM.value;
        tempSfx = SFX.value;
    }
}
