using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenManager : MonoBehaviour
{
	[SerializeField] Image _loadingScreenPanel;
	[SerializeField] List<Image> _loadingIcons = new List<Image>();

	public static LoadingScreenManager Instance { get; private set; }
	private void Awake()
	{
		if(Instance != null && Instance != this)
		{
			Destroy(this);
			return;
		}
		Instance = this;
	}

	public void DisplayLoadingPanel(float fadeDuration = 0.5f, Action onCompleteFade = null, float duration = 0.0f, Action onCompleteDuration = null)
	{
		StartCoroutine(DisplayLoadingPanel_Coroutine(fadeDuration, onCompleteFade, duration, onCompleteDuration));
	}
	public void DisplayLoadingIcon(float fadeDuration = 0.5f, Action onCompleteFade = null, float duration = 0.0f, Action onCompleteDuration = null)
	{
		StartCoroutine(DisplayLoadingIcon_Coroutine(fadeDuration, onCompleteFade, duration, onCompleteDuration));
	}
	public void DisplayLoadingPanelAndIcon(float fadeDuration = 0.5f, Action onCompleteFade = null, float duration = 0.0f, Action onCompleteDuration = null)
	{
		StartCoroutine(DisplayLoadingPanel_Coroutine(fadeDuration, onCompleteFade, duration, onCompleteDuration));
		StartCoroutine(DisplayLoadingIcon_Coroutine(fadeDuration, null, duration, null));
	}

	public void HideLoadingPanel(float fadeDuration = 0.5f, Action onCompleteFade = null, float duration = 0.0f, Action onCompleteDuration = null)
	{
		StartCoroutine(HideLoadingPanel_Coroutine(fadeDuration, onCompleteFade, duration, onCompleteDuration));
	}
	public void HideLoadingIcon(float fadeDuration = 0.5f, Action onCompleteFade = null, float duration = 0.0f, Action onCompleteDuration = null)
	{
		StartCoroutine(HideLoadingIcon_Coroutine(fadeDuration, onCompleteFade, duration, onCompleteDuration));
	}
	public void HideLoadingPanelAndIcon(float fadeDuration = 0.5f, Action onCompleteFade = null, float duration = 0.0f, Action onCompleteDuration = null)
	{
		StartCoroutine(HideLoadingPanel_Coroutine(fadeDuration, onCompleteFade, duration, onCompleteDuration));
		StartCoroutine(HideLoadingIcon_Coroutine(fadeDuration, null, duration, null));
	}

	public void DisplayAndHideLoadingPanel(float fadeInDuration = 0.5f, Action onCompleteFadeIn = null,
		float displayDuration = 0.5f, Action onCompleteDisplayDuration = null,
		float fadeOutDuration = 0.5f, Action onCompleteFadeOut = null)
	{
		DisplayLoadingPanel(fadeInDuration, onCompleteFadeIn, displayDuration, () => { onCompleteDisplayDuration?.Invoke(); HideLoadingPanel(fadeOutDuration, onCompleteFadeOut); });
	}
	public void DisplayAndHideLoadingIcon(float fadeInDuration = 0.5f, Action onCompleteFadeIn = null,
		float displayDuration = 0.5f, Action onCompleteDisplayDuration = null,
		float fadeOutDuration = 0.5f, Action onCompleteFadeOut = null)
	{
		DisplayLoadingIcon(fadeInDuration, onCompleteFadeIn, displayDuration, () => { onCompleteDisplayDuration?.Invoke(); HideLoadingIcon(fadeOutDuration, onCompleteFadeOut); });
	}
	public void DisplayAndHideLoadingPanelAndIcon(float fadeInDuration = 0.5f, Action onCompleteFadeIn = null,
		float displayDuration = 0.5f, Action onCompleteDisplayDuration = null,
		float fadeOutDuration = 0.5f, Action onCompleteFadeOut = null)
	{
		DisplayLoadingPanelAndIcon(fadeInDuration, onCompleteFadeIn, displayDuration, () => { onCompleteDisplayDuration?.Invoke(); HideLoadingPanelAndIcon(fadeOutDuration, onCompleteFadeOut); });
	}


	// Display loading panel
	private IEnumerator DisplayLoadingPanel_Coroutine(float fadeDuration, Action onCompleteFade = null, float duration = 0.0f, Action onCompleteDuration = null)
	{
		float fadeTime = 0.0f;
		_loadingScreenPanel.gameObject.SetActive(true);
		_loadingScreenPanel.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);

		// Fade in
		while(fadeTime < fadeDuration)
		{
			_loadingScreenPanel.color += new Color(0.0f, 0.0f, 0.0f, fadeDuration * Time.deltaTime);
			fadeTime += Time.deltaTime;
			yield return null;
		}

		// Make sure it's opaque
		_loadingScreenPanel.color = new Color(0.0f, 0.0f, 0.0f, 1.0f);

		onCompleteFade?.Invoke();

		if(duration > 0.0f)
			yield return new WaitForSeconds(duration);

		onCompleteDuration?.Invoke();
	}

	// Hide loading panel
	private IEnumerator HideLoadingPanel_Coroutine(float fadeDuration, Action onCompleteFade = null, float duration = 0.0f, Action onCompleteDuration = null)
	{
		float fadeTime = 0.0f;
		_loadingScreenPanel.gameObject.SetActive(true);

		// Fade out
		while(fadeTime < fadeDuration)
		{
			_loadingScreenPanel.color -= new Color(0.0f, 0.0f, 0.0f, fadeDuration * Time.deltaTime);
			fadeTime += Time.deltaTime;
			yield return null;
		}

		// Make sure it's transparent
		_loadingScreenPanel.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);

		_loadingScreenPanel.gameObject.SetActive(false);

		onCompleteFade?.Invoke();

		if(duration > 0.0f)
			yield return new WaitForSeconds(duration);

		onCompleteDuration?.Invoke();
	}

	// Display Loading Icon
	private IEnumerator DisplayLoadingIcon_Coroutine(float fadeDuration, Action onCompleteFade = null, float duration = 0.0f, Action onCompleteDuration = null)
	{
		float fadeTime = 0.0f;
		foreach(Image loadingIcon in _loadingIcons)
		{
			loadingIcon.gameObject.SetActive(true);
			loadingIcon.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
		}

		// Fade in
		while(fadeTime < fadeDuration)
		{
			foreach(Image loadingIcon in _loadingIcons)
				loadingIcon.color += new Color(0.0f, 0.0f, 0.0f, fadeDuration * Time.deltaTime);
			fadeTime += Time.deltaTime;
			yield return null;
		}

		foreach(Image loadingIcon in _loadingIcons)
			// Make sure it's opaque
			loadingIcon.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);

		onCompleteFade?.Invoke();

		if(duration > 0.0f)
			yield return new WaitForSeconds(duration);

		onCompleteDuration?.Invoke();
	}

	// Hide Loading Icon
	private IEnumerator HideLoadingIcon_Coroutine(float fadeDuration, Action onCompleteFade = null, float duration = 0.0f, Action onCompleteDuration = null)
	{
		float fadeTime = 0.0f;
		foreach(Image loadingIcon in _loadingIcons)
			loadingIcon.gameObject.SetActive(true);

		// Fade out
		while(fadeTime < fadeDuration)
		{
			foreach(Image loadingIcon in _loadingIcons)
				loadingIcon.color -= new Color(0.0f, 0.0f, 0.0f, fadeDuration * Time.deltaTime);
			fadeTime += Time.deltaTime;
			yield return null;
		}

		// Make sure it's transparent
		foreach(Image loadingIcon in _loadingIcons)
		{
			loadingIcon.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
			loadingIcon.gameObject.SetActive(false);
		}

		onCompleteFade?.Invoke();

		if(duration > 0.0f)
			yield return new WaitForSeconds(duration);

		onCompleteDuration?.Invoke();
	}
}
