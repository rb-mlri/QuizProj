using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject startButton;   // <--- Add this in the Inspector
    public GameObject modeSelectPanel;
    public GameObject staticLevelPanel;
    public GameObject dynamicLevelPanel;

    [Header("Tooltip")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;
    public Vector3 tooltipOffset = new Vector3(100f, -30f, 0f);

    private void Update()
    {
        if (tooltipPanel.activeSelf)
        {
            Vector3 newPos = Input.mousePosition + tooltipOffset;

            RectTransform rt = tooltipPanel.GetComponent<RectTransform>();
            float tooltipWidth = rt.rect.width;
            float tooltipHeight = rt.rect.height;

            float clampedX = Mathf.Min(newPos.x, Screen.width - tooltipWidth / 2f);
            float clampedY = Mathf.Max(newPos.y, tooltipHeight / 2f);

            tooltipPanel.transform.position = new Vector3(clampedX, clampedY, 0f);
        }
    }

    // ------------------- Tooltip Control ------------------- //
    public void ShowTooltip(string content)
    {
        tooltipText.text = content;
        tooltipPanel.SetActive(true);
    }

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }

    // ------------------- Start Button ------------------- //
    public void ShowGameModes()
    {
        startButton.SetActive(false);   // hide Start button
        modeSelectPanel.SetActive(true);
    }

    public void BackToMainMenu()
    {
        startButton.SetActive(true);    // show Start button again
        modeSelectPanel.SetActive(false);
        staticLevelPanel.SetActive(false);
        dynamicLevelPanel.SetActive(false);
    }

    // ------------------- Panel Show/Hide ------------------- //
    public void ShowStaticLevelPanel()
    {
        staticLevelPanel.SetActive(true);
        modeSelectPanel.SetActive(false);
    }

    public void BackToModeSelectFromStatic()
    {
        staticLevelPanel.SetActive(false);
        modeSelectPanel.SetActive(true);
    }

    public void ShowDynamicLevelPanel()
    {
        dynamicLevelPanel.SetActive(true);
        modeSelectPanel.SetActive(false);
    }

    public void BackToModeSelectFromDynamic()
    {
        dynamicLevelPanel.SetActive(false);
        modeSelectPanel.SetActive(true);
    }

    // ------------------- Level Selection ------------------- //
    public void SelectStaticLevel(int level)
    {
        PlayerPrefs.SetInt("SelectedLevel", level);
        string sceneName = $"StaticQuiz{level}";
        LoadScene(sceneName);
    }

    public void SelectDynamicLevel(int level)
    {
        PlayerPrefs.SetInt("SelectedLevel", level);
        string sceneName = $"DynamicQuiz{level}";
        LoadScene(sceneName);
    }

    private void LoadScene(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError($"Scene '{sceneName}' not found in Build Settings!");
        }
    }

    // ------------------- Exit Game ------------------- //
    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
