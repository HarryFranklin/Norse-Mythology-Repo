using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("The source used for short, one-shot sound effects (UI clicks, hits, gunshots).")]
    [SerializeField] private AudioSource sfxSource;
    
    [Tooltip("The source used for background music.")]
    [SerializeField] private AudioSource musicSource;

    [Tooltip("The source used for sustained looping effects (Time Freeze hum, Beam attacks).")]
    [SerializeField] private AudioSource loopSource;

    [Header("UI Defaults")]
    [SerializeField] private AudioClip defaultButtonSound;
    [SerializeField] private AudioClip defaultToggleSound;

    // Internal state
    private Coroutine loopFadeCoroutine;
    private float baseLoopVolume = 1f; // Stores the volume set in the Inspector to use as a "Max" cap

    private void Awake()
    {
        // Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Capture the initial volume set in the Inspector for the Loop Source.
            // This prevents code from accidentally blasting volume at 100% if you wanted it quiet.
            if (loopSource != null)
            {
                baseLoopVolume = loopSource.volume;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- SFX & UI METHODS ---

    public void PlayButtonSound()
    {
        PlaySFX(defaultButtonSound);
    }

    public void PlayToggleSound(bool isOn)
    {
        if (defaultToggleSound == null || sfxSource == null) return;
        
        // Pitch shift logic: Normal pitch for ON, Lower/Quieter for OFF
        sfxSource.pitch = isOn ? 1f : 0.8f;
        sfxSource.PlayOneShot(defaultToggleSound, isOn ? 1f : 0.7f);
        
        // Reset pitch immediately so future sounds aren't weird
        sfxSource.pitch = 1f; 
    }

    // 1. Randomised / Default Pitch Version
    public void PlaySFX(AudioClip clip, float volume = 1f, bool useRandomPitch = false)
    {
        if (clip == null || sfxSource == null) return;

        if (useRandomPitch)
        {
            // Randomise between -1 and +1 semitones (5.946%)
            float randomSemitone = Random.Range(-1f, 1f);
            sfxSource.pitch = Mathf.Pow(1.05946f, randomSemitone);
        }
        else
        {
            sfxSource.pitch = 1f; // Reset to normal
        }

        sfxSource.PlayOneShot(clip, volume);
    }

    // 2. Specific Pitch Version (For the Hammer)
    public void PlaySFX(AudioClip clip, float volume, float specificPitch)
    {
        if (clip == null || sfxSource == null) return;

        sfxSource.pitch = specificPitch;
        sfxSource.PlayOneShot(clip, volume);
    }

    // --- LOOPING METHODS (Time Freeze, etc) ---

    public void PlayLoop(AudioClip clip, float volumeFraction = 1f)
    {
        if (loopSource == null || clip == null) return;

        // Stop any active fade-out to prevent fighting
        if (loopFadeCoroutine != null) StopCoroutine(loopFadeCoroutine);

        loopSource.clip = clip;
        
        // Calculate actual volume based on the Inspector setting
        loopSource.volume = baseLoopVolume * Mathf.Clamp01(volumeFraction);
        
        loopSource.loop = true;
        loopSource.Play();
    }

    public void SetLoopVolume(float volumeFraction)
    {
        if (loopSource != null) 
        {
            loopSource.volume = baseLoopVolume * Mathf.Clamp01(volumeFraction);
        }
    }

    public void SetLoopPitch(float pitch)
    {
        if (loopSource != null) 
        {
            loopSource.pitch = pitch;
        }
    }

    public void StopLoop(bool fadeOut = true, float fadeDuration = 0.5f)
    {
        if (loopSource == null || !loopSource.isPlaying) return;

        if (fadeOut)
        {
            if (loopFadeCoroutine != null) StopCoroutine(loopFadeCoroutine);
            loopFadeCoroutine = StartCoroutine(FadeOutRoutine(loopSource, fadeDuration));
        }
        else
        {
            loopSource.Stop();
            loopSource.volume = 0f;
        }
    }

    // --- MUSIC METHODS ---

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;
        
        // Don't restart the track if it's already playing
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        
        musicSource.clip = clip;
        musicSource.Play();
    }

    // --- INTERNAL HELPERS ---

    private IEnumerator FadeOutRoutine(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Use unscaledDeltaTime so sound fades even if Time.timeScale is 0!
            elapsed += Time.unscaledDeltaTime; 
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        source.Stop();
        source.volume = startVolume; // Reset volume for the next time it's used
    }
}