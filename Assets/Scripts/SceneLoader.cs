using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Optional: allow dynamic scene name assignment from Inspector
    [SerializeField] private string sceneToLoad = "Game";

    public void PlayGame()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("Scene name is not set!");
            return;
        }

        Debug.Log($"Loading Scene: {sceneToLoad}");
        SceneManager.LoadScene(sceneToLoad);
    }
}
