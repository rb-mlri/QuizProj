using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public GameObject mainButtonsPanel;
    public GameObject levelPanel;

    public void ShowLevelPanel()
    {
        mainButtonsPanel.SetActive(false);
        levelPanel.SetActive(true);
    }

    public void BackToMainMenu()
    {
        levelPanel.SetActive(false);
        mainButtonsPanel.SetActive(true);
    }

    public void SelectLevel(int level)
    {
        PlayerPrefs.SetInt("SelectedLevel", level);
        SceneManager.LoadScene("StaticQuiz");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
