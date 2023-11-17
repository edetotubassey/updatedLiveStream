using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 1.0f;
    public float delayBeforeLoading = 1.0f;
    public float fadeOutDuration = 1.0f; // Duration of the fade-out after loading the next scene
    public string nextSceneName; // Set this in the Inspector

    void Start()
    {
        StartCoroutine(StartScene());
    }

    IEnumerator StartScene()
    {
        yield return StartCoroutine(FadeIn());
        yield return new WaitForSeconds(delayBeforeLoading);
        LoadNextScene();
    }

    void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        Color startColor = fadeImage.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (elapsedTime < fadeDuration)
        {
            fadeImage.color = Color.Lerp(startColor, targetColor, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        fadeImage.color = targetColor;
    }

    IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(delayBeforeLoading); // Wait before starting the fade-out

        float elapsedTime = 0f;
        Color startColor = fadeImage.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 1f);

        while (elapsedTime < fadeOutDuration)
        {
            fadeImage.color = Color.Lerp(startColor, targetColor, elapsedTime / fadeOutDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        fadeImage.color = targetColor;
    }
}
