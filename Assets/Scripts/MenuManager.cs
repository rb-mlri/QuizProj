using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public GameObject levelSelectPanel;

    public void ShowLevelPanel()
    {
        levelSelectPanel.SetActive(true);
    }

    public void BackToMainMenu()
    {
        levelSelectPanel.SetActive(false);
    }

    public void SelectLevel(int level)
    {
        PlayerPrefs.SetInt("SelectedLevel", level);

        // Dynamically load scene name based on level number
        string sceneName = $"StaticQuiz{level}";
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError($"Scene '{sceneName}' not found in Build Settings!");
        }
    }

    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
