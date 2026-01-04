using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("The source used for UI and gameplay sound effects.")]
    [SerializeField] private AudioSource sfxSource;
    
    [Tooltip("The source used for background music.")]
    [SerializeField] private AudioSource musicSource;

    [Header("UI Defaults")]
    [SerializeField] private AudioClip defaultButtonSound;
    [SerializeField] private AudioClip defaultToggleSound;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- Public Methods ---

    public void PlayButtonSound()
    {
        PlaySFX(defaultButtonSound);
    }

    // Handles the pitch shifting logic for toggles
    public void PlayToggleSound(bool isOn)
    {
        if (defaultToggleSound == null || sfxSource == null) return;

        if (isOn)
        {
            // ON: Normal Pitch, Normal Volume
            sfxSource.pitch = 1f;
            sfxSource.PlayOneShot(defaultToggleSound, 0.8f);
        }
        else
        {
            // OFF: Lower Pitch, Quieter
            sfxSource.pitch = 0.8f; 
            sfxSource.PlayOneShot(defaultToggleSound, 0.6f);
        }
    }

    public void PlaySFX(AudioClip clip, float volume = 1f, float pitchVariation = 0f)
    {
        if (clip == null || sfxSource == null) return;

        // Always reset or apply pitch
        if (pitchVariation > 0)
            sfxSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        else
            sfxSource.pitch = 1f;

        sfxSource.PlayOneShot(clip, volume);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        
        musicSource.clip = clip;
        musicSource.Play();
    }
}