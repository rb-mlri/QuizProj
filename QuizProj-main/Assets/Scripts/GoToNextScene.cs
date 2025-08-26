using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToNextScene : MonoBehaviour
{
    public string nextScene;

    public void LoadNextScene()
    {
        StartCoroutine(LoadSceneCoroutine());
    }

    private System.Collections.IEnumerator LoadSceneCoroutine()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextScene);
        
        while (!asyncLoad.isDone)
        {
            yield return null; // Wait until the scene is fully loaded
        }

        Scene loadedScene = SceneManager.GetSceneByName(nextScene);
        SceneManager.SetActiveScene(loadedScene);
    }
}
