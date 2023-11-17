using UnityEngine;
using UnityEngine.UI;
using System.Collections;


public class transition : MonoBehaviour 
{ 
[SerializeField]
private Image fadeImage;

public float fadeDuration = 1.0f;
public float delayBetweenTransitions = 2.0f;

void Start()
{
    StartCoroutine(TransitionLoop());
}

IEnumerator TransitionLoop()
{
    while (true)
    {
        yield return StartCoroutine(FadeIn());
        yield return new WaitForSeconds(delayBetweenTransitions);
        yield return StartCoroutine(FadeOut());
        yield return new WaitForSeconds(delayBetweenTransitions);
    }
}

IEnumerator FadeIn()
{
    float elapsedTime = 0f;
    Color startColor = fadeImage.color;
    Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 1f);

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
}

