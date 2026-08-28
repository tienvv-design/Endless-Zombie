using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LoadingScreenController : MonoBehaviour
{
    [SerializeField] private string _nextSceneName = "GameScene";
    [SerializeField, Min(0f)] private float _minimumDisplayTime = 1.75f;
    [SerializeField] private RectTransform _progressFill;
    [SerializeField] private Text _progressLabel;

    private IEnumerator Start()
    {
        SetProgress(0f);

        // Let the loading artwork reach the screen before starting the heavier scene load.
        yield return null;

        AsyncOperation load = SceneManager.LoadSceneAsync(_nextSceneName, LoadSceneMode.Single);
        if (load == null)
        {
            Debug.LogError($"Loading screen could not start scene '{_nextSceneName}'.", this);
            yield break;
        }

        load.allowSceneActivation = false;
        float elapsed = 0f;
        float displayedProgress = 0f;

        while (load.progress < 0.9f || elapsed < _minimumDisplayTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float sceneProgress = Mathf.Clamp01(load.progress / 0.9f);
            float timeProgress = _minimumDisplayTime <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / _minimumDisplayTime);
            float target = Mathf.Min(sceneProgress, timeProgress);
            displayedProgress = Mathf.MoveTowards(displayedProgress, target, Time.unscaledDeltaTime * 0.9f);
            SetProgress(displayedProgress);
            yield return null;
        }

        while (displayedProgress < 1f)
        {
            displayedProgress = Mathf.MoveTowards(displayedProgress, 1f, Time.unscaledDeltaTime * 2.5f);
            SetProgress(displayedProgress);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.12f);
        load.allowSceneActivation = true;
    }

    private void SetProgress(float value)
    {
        value = Mathf.Clamp01(value);
        if (_progressFill != null)
            _progressFill.anchorMax = new Vector2(value, 1f);
        if (_progressLabel != null)
            _progressLabel.text = $"LOADING  {Mathf.RoundToInt(value * 100f)}%";
    }
}
