using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject startButton;
    public GameObject tutorialButton;
    public GameObject modeSelectPanel;
    public GameObject staticLevelPanel;
    public GameObject dynamicLevelPanel;

    [Header("Tooltip")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;
    public Vector3 tooltipOffset = new Vector3(20f, -20f, 0f); // Offset from cursor

    private bool isTooltipActive = false;

    private void Awake()
    {
        tooltipPanel.SetActive(false);
    }

    private void Update()
    {
        if (isTooltipActive)
        {
            FollowMouse();
        }
    }

    private void FollowMouse()
    {
        Vector3 mousePos = Input.mousePosition + tooltipOffset;
        tooltipPanel.transform.position = mousePos;
    }

    // ------------------- Tooltip Control ------------------- //
    public void ShowTooltip(string content)
    {
        tooltipText.text = content;
        tooltipPanel.SetActive(true);
        isTooltipActive = true;
        FollowMouse(); // Set initial position immediately
    }

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
        isTooltipActive = false;
    }

    // ------------------- Tutorial Button ------------------- //
    public void TutorialScene()
    {
        SceneManager.LoadScene("Tutorial");
    }

    // ------------------- Back menu Scene Button ------------------- //
    public void Back()
    {
        SceneManager.LoadScene(0);
    }

    // ------------------- Start Button ------------------- //
    public void ShowGameModes()
    {
        startButton.SetActive(false);
        tutorialButton.SetActive(false);
        modeSelectPanel.SetActive(true);
    }

    public void BackToMainMenu()
    {
        startButton.SetActive(true);
        tutorialButton.SetActive(true);
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
