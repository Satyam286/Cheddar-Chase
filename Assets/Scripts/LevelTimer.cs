using TMPro;
using UnityEngine;

public class LevelTimer : MonoBehaviour
{
    public float timeLimit = 60f;
    public TMP_Text timerText;

    [Header("Low Time Warning")]
    public float warningTime = 10f;
    public Color normalColor = Color.white;
    public Color warningColor = Color.red;
    public float pulseSpeed = 6f;
    public AudioSource tickSource;
    public AudioClip tickClip;

    private float timeRemaining;
    private bool timerRunning = true;
    private int lastTickSecond = -1;

    void Start()
    {
        timeRemaining = timeLimit;
        UpdateTimerText();
    }

    void Update()
    {
        if (!timerRunning)
            return;

        timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);

        UpdateTimerText();
        HandleWarning();

        if (timeRemaining <= 0f)
        {
            timerRunning = false;

            if (LevelEndManager.Instance != null)
                LevelEndManager.Instance.GameOver("Time's up!");
        }
    }

    void UpdateTimerText()
    {
        if (timerText == null)
            return;

        // Ceil so the clock only reads 00:00 when time has really run out.
        int total = Mathf.CeilToInt(timeRemaining);
        timerText.text = $"Time: {total / 60:00}:{total % 60:00}";
    }

    void HandleWarning()
    {
        bool warning = timeRemaining <= warningTime;

        if (timerText != null)
        {
            if (warning)
            {
                float pulse = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
                timerText.color = Color.Lerp(warningColor, normalColor, pulse * 0.5f);
            }
            else
            {
                timerText.color = normalColor;
            }
        }

        // One tick per second while in the warning window.
        int wholeSecond = Mathf.CeilToInt(timeRemaining);
        if (warning && wholeSecond > 0 && wholeSecond != lastTickSecond)
        {
            lastTickSecond = wholeSecond;

            if (tickSource != null && tickClip != null)
                tickSource.PlayOneShot(tickClip);
        }
    }

    public void StopTimer()
    {
        timerRunning = false;
    }

    public float GetTimeRemaining()
    {
        return timeRemaining;
    }
}