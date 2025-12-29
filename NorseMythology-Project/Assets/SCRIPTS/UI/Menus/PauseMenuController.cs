using UnityEngine;

public class PauseMenuController : MonoBehaviour
{
    [Tooltip("The Panel that holds the menu buttons. If empty, the script will grab the first child object.")]
    [SerializeField] private GameObject pauseMenuPanel;

    private void Awake()
    {
        if (pauseMenuPanel == null)
        {
            if (transform.childCount > 0)
            {
                pauseMenuPanel = transform.GetChild(0).gameObject;
            }
            else
            {
                Debug.LogError("PauseMenuController: No child object found to act as the Panel!");
                return;
            }
        }

        pauseMenuPanel.SetActive(false);
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPausePanel(pauseMenuPanel);
        }
    }

    public void Resume()
    {
        if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
    }

    public void QuitGame()
    {
        if (GameManager.Instance != null) GameManager.Instance.QuitToMainMenu();
    }
}