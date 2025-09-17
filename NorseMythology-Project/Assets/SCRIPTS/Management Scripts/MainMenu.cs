using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public void OnStartButton()
    {
        // Tell the GameManager to begin the 'new game' process
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
        }
        else
        {
            Debug.LogError("GameManager not found! Make sure the Boot scene runs first.");
        }
    }
}