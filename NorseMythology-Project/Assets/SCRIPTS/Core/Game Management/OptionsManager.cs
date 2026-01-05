using UnityEngine;

public class OptionsManager : MonoBehaviour
{
    public static OptionsManager Instance { get; private set; }

    // --- Settings Data ---
    public bool EnableAbilitySwapping { get; private set; } = true;
    
    public float MasterVolume { get; private set; } = 1f;

    private const string PREF_ABILITY_SWAP = "Opt_AbilitySwap";
    private const string PREF_MASTER_VOLUME = "Opt_MasterVolume";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadOptions();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetAbilitySwapping(bool isEnabled)
    {
        EnableAbilitySwapping = isEnabled;
        PlayerPrefs.SetInt(PREF_ABILITY_SWAP, isEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        
        // AudioListener.volume controls the global volume of the scene
        AudioListener.volume = MasterVolume;

        PlayerPrefs.SetFloat(PREF_MASTER_VOLUME, MasterVolume);
        PlayerPrefs.Save();
    }

    private void LoadOptions()
    {
        EnableAbilitySwapping = PlayerPrefs.GetInt(PREF_ABILITY_SWAP, 1) == 1;
        
        // Load saved volume or default to 1f
        MasterVolume = PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, 1f);
        AudioListener.volume = MasterVolume;
    }
}