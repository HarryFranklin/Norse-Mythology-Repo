using UnityEngine;

public class OptionsManager : MonoBehaviour
{
    public static OptionsManager Instance { get; private set; }

    // --- Settings Data ---
    public bool EnableAbilitySwapping { get; private set; } = true;
    
    // public float MasterVolume { get; private set; } = 1f;

    private const string PREF_ABILITY_SWAP = "Opt_AbilitySwap";

    private void Awake()
    {
        // Singleton Logic
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadOptions();
        }
        else
        {
            // If one already exists, destroy this new one
            Destroy(gameObject);
        }
    }

    public void SetAbilitySwapping(bool isEnabled)
    {
        EnableAbilitySwapping = isEnabled;
        PlayerPrefs.SetInt(PREF_ABILITY_SWAP, isEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadOptions()
    {
        // Load saved data, default to 1 (true) if key doesn't exist
        EnableAbilitySwapping = PlayerPrefs.GetInt(PREF_ABILITY_SWAP, 1) == 1;
    }
}