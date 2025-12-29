using UnityEngine;
using UnityEngine.SceneManagement;

public class EndgameManager : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("The name of the scene to load when restarting.")]
    [SerializeField] private string characterSelectorSceneName = "3_CharacterSelector";

    [Tooltip("The name of the main menu scene.")]
    [SerializeField] private string mainMenuSceneName = "2_MainMenu";

    [Header("UI References")]
    [Tooltip("Assign the GameObject containing the stats UI if you want to toggle it.")]
    [SerializeField] private GameObject statsPanel;

    public void OnStatsClicked()
    {
        // Placeholder functionality.
        Debug.Log("Stats button clicked. Functionality to be developed later.");
        
        if (statsPanel != null)
        {
            bool isActive = statsPanel.activeSelf;
            statsPanel.SetActive(!isActive);
        }
    }

    public void OnRestartClicked()
    {
        if (string.IsNullOrEmpty(characterSelectorSceneName))
        {
            Debug.LogError("Gameplay Scene Name is not set in the Inspector!");
            return;
        }

        // Loads the gameplay scene to restart the game
        SceneManager.LoadScene(characterSelectorSceneName);
    }

    public void OnQuitToMainMenuClicked()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("Main Menu Scene Name is not set in the Inspector!");
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnQuitToDesktopClicked()
    {
        // If it's a build
        #if UNITY_STANDALONE
            //Quit the application
            Application.Quit();
        #endif
    
        //If we are running in the editor
        #if UNITY_EDITOR
            //Stop playing the scene
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}