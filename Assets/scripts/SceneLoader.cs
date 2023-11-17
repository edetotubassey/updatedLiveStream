using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{

    public int nextSceneBuildIndex; // Set this in the Inspector

    public void LoadNextSceneAsync()
    {
        StartCoroutine(LoadSceneAsync(nextSceneBuildIndex));
    }

    private IEnumerator LoadSceneAsync(int sceneIndex)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);

        while (!asyncLoad.isDone)
        {
            // You can implement a loading screen or other updates here
            yield return null;
        }
    }
}