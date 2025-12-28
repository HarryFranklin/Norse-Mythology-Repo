using UnityEngine;

public class PauseMenuController : MonoBehaviour
{
    public void OnResumePressed()
    {
        if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
    }

    public void OnQuitPressed()
    {
        if (GameManager.Instance != null) GameManager.Instance.QuitToMainMenu();
    }
}