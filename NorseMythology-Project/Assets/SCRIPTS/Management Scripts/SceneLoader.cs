using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Point this to your Main Menu scene
    [SerializeField] private string sceneToLoad = "MainMenu";

    void Start()
    {
        Thread.Sleep(2000);
        SceneManager.LoadScene(sceneToLoad);
    }
}