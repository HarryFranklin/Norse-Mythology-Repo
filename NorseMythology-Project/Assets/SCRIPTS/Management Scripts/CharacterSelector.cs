using UnityEngine;

public class CharacterSelector : MonoBehaviour
{
    public void OnContinueButton()
    {
        // Tell the GameManager to proceed into the main game
        if (GameManager.Instance != null)
        {
            // When you implement classes, you'll apply the chosen class's stats here first.
            GameManager.Instance.StartGameFromSelector();
        }
        else
        {
            Debug.LogError("GameManager not found! Make sure the Boot scene runs first.");
        }
    }
}