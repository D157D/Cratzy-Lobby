using UnityEngine;
using System; 

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioSource bgmSource; 
    public AudioSource[] sfxSources;

    private float bgmVolume = 1f;
    private float sfxVolume = 1f;
    private bool isMuted;

    private const string BGMKey = "BGMVolume";
    private const string SFXKey = "SFXVolume";
    private const string MuteKey = "GameMuted";

    public event Action<float> OnBGMVolumeChanged;
    public event Action<float> OnSFXVolumeChanged;
    public event Action<bool> OnMuteChanged;

    public event Action OnGamePaused;
    public event Action OnGameResumed;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadSettings()
    {
        bgmVolume = PlayerPrefs.GetFloat(BGMKey, 1f);
        sfxVolume = PlayerPrefs.GetFloat(SFXKey, 1f);
        isMuted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
        
        ApplyVolume(); 
    }

    public void ApplyVolume()
    {
        float currentBgmVol = isMuted ? 0f : bgmVolume;
        float currentSfxVol = isMuted ? 0f : sfxVolume;

        if (bgmSource != null)
            bgmSource.volume = currentBgmVol;

        foreach (AudioSource sfx in sfxSources)
        {
            if (sfx != null)
                sfx.volume = currentSfxVol;
        }

        OnBGMVolumeChanged?.Invoke(currentBgmVol);
        OnSFXVolumeChanged?.Invoke(currentSfxVol);
        OnMuteChanged?.Invoke(isMuted);
    }

    public void UpdateVolume(float bgm, float sfx, bool muted)
    {
        bgmVolume = bgm;
        sfxVolume = sfx;
        isMuted = muted;
        
        ApplyVolume(); 
        SaveSettings(); 
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(BGMKey, bgmVolume);
        PlayerPrefs.SetFloat(SFXKey, sfxVolume);
        PlayerPrefs.SetInt(MuteKey, isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public float GetBgmVolume() => bgmVolume; 
    public float GetSfxVolume() => sfxVolume; 
    public bool IsMuted() => isMuted;
    
    public float GetEffectiveBGMVolume() => isMuted ? 0f : bgmVolume;
    public float GetEffectiveSFXVolume() => isMuted ? 0f : sfxVolume;



    public void PlaySFX(AudioClip clip, Vector3 position)
    {
        if (clip == null || isMuted) return;

        GameObject sfxObject = new GameObject("SFX_" + clip.name);
        sfxObject.transform.position = position;

        AudioSource source = sfxObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = isMuted ? 0f : sfxVolume; 
        source.Play();

        Destroy(sfxObject, clip.length);
    }

    public void PlayMusic(AudioClip clip, bool loop = false) 
    {
        if (bgmSource == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return; 

        bgmSource.clip = clip;
        bgmSource.loop = loop; 
        bgmSource.volume = isMuted ? 0f : bgmVolume; 
        bgmSource.Play();
    }

    public bool IsMusicPlaying()
    {
        if (bgmSource == null) return false;
        return bgmSource.isPlaying;
    }

    public void PauseMusic()
    {
        if (bgmSource != null && bgmSource.isPlaying) bgmSource.Pause();
        
        OnGamePaused?.Invoke();
    }

    public void ResumeMusic()
    {
        if (bgmSource != null) bgmSource.UnPause();
        
        OnGameResumed?.Invoke();
    }

    public void StopMusic()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    public void SyncExternalMusic(AudioSource externalSource)
    {
        if (externalSource == null) return;
        externalSource.volume = isMuted ? 0f : bgmVolume;
        externalSource.mute = isMuted; 
    }
}