using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    public void OnStartNewGameClicked()
    {
        // Find the persistent GameManager instance and tell it to start the new game process.
        // The GameManager will handle resetting data and loading the Character Selector scene.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
        }
        else
        {
            // This error should only appear if you run the MainMenu scene directly
            // without running the Boot scene first.
            Debug.LogError("GameManager not found! The Boot scene must be run first.");
        }
    }

    public void OnQuitGameClicked()
    {
        Application.Quit();
    }
}