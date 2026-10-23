using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Handles winning / losing a level: freezes gameplay, fades to the end screen,
/// shows cheese + time + stars, saves best results, plays end sounds.
/// </summary>
public class LevelEndManager : MonoBehaviour
{
    public static LevelEndManager Instance { get; private set; }

    [Header("References")]
    [Tooltip("Auto-found if left empty.")]
    public LevelTimer timer;
    [Tooltip("Hidden when the player wins. Left visible on game over.")]
    public GameObject enemy;
    [Tooltip("Parent of the in-game HUD (hearts, timer...). Hidden when the end screen shows.")]
    public GameObject hudRoot;

    [Header("UI Panels")]
    public GameObject gameOverUI;
    public GameObject gameWinUI;

    [Header("Transition")]
    public Image transitionOverlay;
    public float transitionDuration = 0.5f;

    [Header("Game Over")]
    [Tooltip("Time scale during the short slow-motion beat before the fade starts.")]
    [Range(0f, 1f)] public float deathSlowMoScale = 0.2f;
    [Tooltip("Real-time seconds of slow motion before fading out.")]
    public float deathBeatDuration = 0.7f;
    [Tooltip("Optional. Shows why you lost (Out of lives! / Time's up!).")]
    public TMP_Text gameOverReasonText;

    [Header("HUD")]
    [Tooltip("In-game 'Cheese: 3 / 10' text. Replaces the old text on PlayerController.")]
    public TMP_Text hudCheeseText;

    [Header("Win UI")]
    public TMP_Text cheeseText;
    public TMP_Text timeText;
    public TMP_Text bestTimeText;
    public GameObject newBestLabel;
    [Tooltip("Drag your 3 star images here, in order.")]
    public Image[] starImages;
    public Color starOnColor = Color.yellow;
    public Color starOffColor = new Color(0.25f, 0.25f, 0.25f, 1f);
    public float countUpDuration = 0.8f;

    [Header("Stars")]
    [Tooltip("Star 1 = finished. Star 2 = all cheese. Star 3 = finished with at least this many seconds left.")]
    public float timeBonusForStar = 15f;

    [Header("Cheese")]
    public bool autoCountCheese = true;
    public string cheeseTag = "PickUp";
    [Tooltip("Only used if Auto Count Cheese is off.")]
    public int totalCheese = 10;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioSource musicSource;
    [Range(0f, 1f)] public float musicVolumeOnEnd = 0.2f;
    public AudioClip winClip;
    public AudioClip loseClip;
    public AudioClip starClip;

    [Header("Menu Navigation (keyboard / gamepad)")]
    public GameObject winFirstSelected;
    public GameObject loseFirstSelected;

    public bool LevelEnded { get; private set; }
    public int CollectedCheese { get; private set; }
    public int TotalCheese => totalCheese;

    private float timeLeftAtEnd;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Never start a level frozen (e.g. after restarting from an end screen).
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    void OnDestroy()
    {
        if (Instance != this)
            return;

        Instance = null;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    void Start()
    {
        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (gameWinUI != null) gameWinUI.SetActive(false);

        if (transitionOverlay != null)
        {
            Color color = transitionOverlay.color;
            color.a = 0f;
            transitionOverlay.color = color;
            transitionOverlay.gameObject.SetActive(false);
        }

        if (timer == null)
            timer = FindFirstObjectByType<LevelTimer>();

        if (autoCountCheese)
            totalCheese = GameObject.FindGameObjectsWithTag(cheeseTag).Length;

        UpdateHud();
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    public void AddCheese()
    {
        if (LevelEnded)
            return;

        CollectedCheese++;
        UpdateHud();
    }

    public void GameOver(string reason = "")
    {
        if (!BeginEnd())
            return;

        if (gameOverReasonText != null)
        {
            gameOverReasonText.text = reason;
            gameOverReasonText.gameObject.SetActive(!string.IsNullOrEmpty(reason));
        }

        StartCoroutine(GameOverRoutine());
    }

    public void GameWin()
    {
        if (!BeginEnd())
            return;

        if (enemy != null)
            enemy.SetActive(false);

        Time.timeScale = 0f; // freeze right away so nothing changes during the fade
        PlaySfx(winClip);

        StartCoroutine(WinRoutine());
    }

    // ------------------------------------------------------------------
    // Internals
    // ------------------------------------------------------------------

    /// <summary>Shared setup for both endings. Returns false if the level already ended.</summary>
    bool BeginEnd()
    {
        if (LevelEnded)
            return false;

        LevelEnded = true;

        timeLeftAtEnd = 0f;
        if (timer != null)
        {
            timeLeftAtEnd = timer.GetTimeRemaining();
            timer.StopTimer();
        }

        StartCoroutine(FadeMusic());
        return true;
    }

    void UpdateHud()
    {
        if (hudCheeseText != null)
            hudCheeseText.text = $"Cheese: {CollectedCheese} / {totalCheese}";
    }

    IEnumerator GameOverRoutine()
    {
        // Death beat: the world slows down so the hit has time to register.
        Time.timeScale = deathSlowMoScale;
        PlaySfx(loseClip);
        yield return new WaitForSecondsRealtime(deathBeatDuration);

        Time.timeScale = 0f;
        yield return ShowEndUI(gameOverUI, loseFirstSelected);
    }

    IEnumerator WinRoutine()
    {
        int stars = CalculateStars();
        bool newBest = SaveProgress(stars);

        // Start the win screen zeroed out, then count up once it is visible.
        SetCheeseText(0f);
        SetTimeText(0f);
        ResetStars();
        if (newBestLabel != null) newBestLabel.SetActive(false);

        yield return ShowEndUI(gameWinUI, winFirstSelected);

        yield return CountUp(CollectedCheese, SetCheeseText);
        yield return CountUp(timeLeftAtEnd, SetTimeText);
        yield return RevealStars(stars);

        if (newBest && newBestLabel != null)
        {
            newBestLabel.SetActive(true);
            PlaySfx(starClip);
        }
    }

    IEnumerator ShowEndUI(GameObject panel, GameObject firstSelected)
    {
        yield return Fade(0f, 1f);

        if (hudRoot != null) hudRoot.SetActive(false);
        if (panel != null) panel.SetActive(true);
        SelectButton(firstSelected);

        yield return Fade(1f, 0f);
    }

    IEnumerator Fade(float from, float to)
    {
        if (transitionOverlay == null)
            yield break;

        transitionOverlay.gameObject.SetActive(true);

        Color color = transitionOverlay.color;
        float duration = Mathf.Max(0.01f, transitionDuration);

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            color.a = Mathf.Lerp(from, to, t / duration);
            transitionOverlay.color = color;
            yield return null;
        }

        color.a = to;
        transitionOverlay.color = color;

        if (to <= 0f)
            transitionOverlay.gameObject.SetActive(false);
    }

    IEnumerator FadeMusic()
    {
        if (musicSource == null)
            yield break;

        float start = musicSource.volume;
        float duration = Mathf.Max(0.01f, transitionDuration * 2f);

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(start, musicVolumeOnEnd, t / duration);
            yield return null;
        }

        musicSource.volume = musicVolumeOnEnd;
    }

    // ---------------------------- Win screen ----------------------------

    int CalculateStars()
    {
        int stars = 1;                                    // finished the level
        if (CollectedCheese >= totalCheese) stars++;      // got all the cheese
        if (timeLeftAtEnd >= timeBonusForStar) stars++;   // finished quickly
        return stars;
    }

    /// <summary>Saves best time / stars for this scene. Returns true if the time is a new best.</summary>
    bool SaveProgress(int stars)
    {
        string key = "Best_" + SceneManager.GetActiveScene().name;

        float previousBest = PlayerPrefs.GetFloat(key + "_Time", -1f);
        int previousStars = PlayerPrefs.GetInt(key + "_Stars", 0);

        bool newBest = timeLeftAtEnd > previousBest;
        float bestToShow = newBest ? timeLeftAtEnd : previousBest;

        if (newBest) PlayerPrefs.SetFloat(key + "_Time", timeLeftAtEnd);
        if (stars > previousStars) PlayerPrefs.SetInt(key + "_Stars", stars);
        PlayerPrefs.Save();

        if (bestTimeText != null)
            bestTimeText.text = "Best: " + FormatTime(bestToShow) + " left";

        return newBest;
    }

    IEnumerator CountUp(float target, System.Action<float> apply)
    {
        if (countUpDuration <= 0f || target <= 0f)
        {
            apply(target);
            yield break;
        }

        for (float t = 0f; t < countUpDuration; t += Time.unscaledDeltaTime)
        {
            apply(Mathf.Lerp(0f, target, t / countUpDuration));
            yield return null;
        }

        apply(target);
    }

    void SetCheeseText(float value)
    {
        if (cheeseText != null)
            cheeseText.text = $"Cheese: {Mathf.RoundToInt(value)} / {totalCheese}";
    }

    void SetTimeText(float value)
    {
        if (timeText != null)
            timeText.text = "Time Left: " + FormatTime(value);
    }

    void ResetStars()
    {
        if (starImages == null)
            return;

        foreach (Image star in starImages)
        {
            if (star == null) continue;
            star.color = starOffColor;
            star.transform.localScale = Vector3.one;
        }
    }

    IEnumerator RevealStars(int earned)
    {
        if (starImages == null)
            yield break;

        for (int i = 0; i < starImages.Length && i < earned; i++)
        {
            if (starImages[i] == null) continue;

            starImages[i].color = starOnColor;
            PlaySfx(starClip);
            yield return Pop(starImages[i].transform);
        }
    }

    IEnumerator Pop(Transform target)
    {
        const float duration = 0.25f;

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float scale = 1f + 0.3f * Mathf.Sin(t / duration * Mathf.PI);
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    // ----------------------------- Helpers ------------------------------

    static string FormatTime(float seconds)
    {
        int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        return $"{total / 60:00}:{total % 60:00}";
    }

    void PlaySfx(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }

    void SelectButton(GameObject button)
    {
        if (button == null || EventSystem.current == null)
            return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button);
    }
}