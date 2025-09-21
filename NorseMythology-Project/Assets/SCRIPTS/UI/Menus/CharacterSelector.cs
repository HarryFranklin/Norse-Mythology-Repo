using UnityEngine;

public class CharacterSelector : MonoBehaviour
{
    // We will drag our UI controller GameObject here in the Inspector
    [SerializeField] private ClassSelectorUI classSelectorUI;

    public void OnContinueButton()
    {
        if (GameManager.Instance != null && classSelectorUI != null && classSelectorUI.selectedClass != null)
        {
            // 1. Pass the final selected class to the GameManager
            GameManager.Instance.SetSelectedClass(classSelectorUI.selectedClass);
            
            // 2. Proceed to the main game scene
            GameManager.Instance.StartGameFromSelector();
        }
        else
        {
            Debug.LogError("GameManager or ClassSelectorUI not found, or no class was selected!");
        }
    }
}