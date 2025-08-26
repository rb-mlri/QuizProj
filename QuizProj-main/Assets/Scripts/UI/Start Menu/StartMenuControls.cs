using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenuControls : MonoBehaviour
{
    public GameObject loadingScreen;
    public Slider slider;
    public TextMeshProUGUI progressText;

    public GameObject mechanicsPanel;  // Reference to Mechanics Panel

    private void Start()
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(false);

        if (mechanicsPanel != null)
            mechanicsPanel.SetActive(false); // Hide mechanics panel initially
    }

    public void StartButtonControl()
    {
        StartCoroutine(LoadSceneAsync("Dungeon"));
    }

    public void QuitButtonControl()
    {
        Application.Quit();
    }

    public void ShowMechanicsPanel()
    {
        if (mechanicsPanel != null)
            mechanicsPanel.SetActive(true);
    }

    public void HideMechanicsPanel()
    {
        if (mechanicsPanel != null)
            mechanicsPanel.SetActive(false);
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            if (slider != null)
                slider.value = progress;

            if (progressText != null)
                progressText.text = Mathf.RoundToInt(progress * 100f) + "%";

            yield return null;
        }

        if (slider != null)
            slider.value = 1f;

        if (progressText != null)
            progressText.text = "100%";

        yield return new WaitForSeconds(0.3f);
    }
} 