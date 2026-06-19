using System.Collections;
using TMPro;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("UI Text Elements (assign in Inspector)")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI trialCounterText;
    public TextMeshProUGUI timerText;

    [Header("Results Panel")]
    public GameObject      resultsPanel;
    public TextMeshProUGUI resultsText;

    private int _total;
    private int _done;
    private Coroutine _timerRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (resultsPanel != null) resultsPanel.SetActive(false);
        ShowMessage("Welcome! Press Start to begin assessment.");
    }

    public void ShowMessage(string msg)
    {
        if (instructionText != null) instructionText.text = msg;
        Debug.Log($"[UI] {msg}");   // also log so you can see it without VR
    }

    public void SetTotalTrials(int total) => _total = total;

    public void OnTrialEnded(TrialData trial)
    {
        _done++;
        if (trialCounterText != null) trialCounterText.text = $"Trial {_done} / {_total}";
        ShowMessage(trial.ReachedTarget ? "✓ Got it! Return to rest." : "Missed. Return to rest.");
        if (_timerRoutine != null) { StopCoroutine(_timerRoutine); _timerRoutine = null; }
        if (timerText != null) timerText.text = "";
    }

    public void StartTrialTimer(float duration)
    {
        if (_timerRoutine != null) StopCoroutine(_timerRoutine);
        _timerRoutine = StartCoroutine(CountDown(duration));
    }

    public void ShowAssessmentComplete(AbilityZoneResult result)
    {
        if (resultsPanel != null) resultsPanel.SetActive(true);
        string summary =
            $"Assessment Complete!\n\n" +
            $"Trials:        {result.TotalTrials}\n" +
            $"Successful:    {result.SuccessfulTrials}\n" +
            $"Success Rate:  {result.SuccessRate * 100f:F0}%\n" +
            $"Max Reach:     {result.MaxReach3D * 100f:F1} cm";

        if (resultsText != null) resultsText.text = summary;
        ShowMessage("Assessment complete!");
        Debug.Log(summary);
    }

    IEnumerator CountDown(float duration)
    {
        float t = duration;
        while (t > 0f)
        {
            if (timerText != null) timerText.text = $"{t:F1}s";
            t -= Time.deltaTime;
            yield return null;
        }
        if (timerText != null) timerText.text = "Time!";
    }
}
