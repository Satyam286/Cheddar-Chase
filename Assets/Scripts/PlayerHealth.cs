using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Lives")]
    public int maxLives = 3;

    [Header("Heart UI")]
    [Tooltip("Drag your heart images here, left to right. Hearts beyond Max Lives are hidden.")]
    public Image[] hearts;
    public Color heartFullColor = Color.red;
    public Color heartEmptyColor = Color.black;

    [Header("Damage Feedback")]
    public Image damageFlash;
    public float flashDuration = 0.25f;
    public AudioClip hurtClip;

    [Header("Invincibility After a Hit")]
    public float damageCooldown = 1f;
    [Tooltip("How fast the ball blinks while invincible.")]
    public float blinkInterval = 0.1f;

    private int currentLives;
    private bool invincible;
    private Renderer[] renderers;
    private Coroutine flashRoutine;

    private bool LevelOver =>
        LevelEndManager.Instance != null && LevelEndManager.Instance.LevelEnded;

    void Start()
    {
        currentLives = maxLives;
        renderers = GetComponentsInChildren<Renderer>();

        SetFlashAlpha(0f);
        UpdateHearts();
    }

    /// <summary>
    /// Returns true if a life was actually lost (false while invincible or after the level ended),
    /// so hazards know whether to apply knockback.
    /// </summary>
    public bool LoseLife()
    {
        if (invincible || currentLives <= 0 || LevelOver)
            return false;

        currentLives--;
        UpdateHearts();

        if (hurtClip != null)
            AudioSource.PlayClipAtPoint(hurtClip, transform.position);

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(DamageFlash());

        if (currentLives > 0)
        {
            StartCoroutine(InvincibilityRoutine());
        }
        else if (LevelEndManager.Instance != null)
        {
            LevelEndManager.Instance.GameOver("Out of lives!");
        }

        return true;
    }

    /// <summary>For future heart pickups.</summary>
    public void AddLife()
    {
        if (currentLives <= 0 || currentLives >= maxLives)
            return;

        currentLives++;
        UpdateHearts();
    }

    public int GetCurrentLives()
    {
        return currentLives;
    }

    void UpdateHearts()
    {
        if (hearts == null)
            return;

        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null) continue;

            hearts[i].gameObject.SetActive(i < maxLives);
            hearts[i].color = i < currentLives ? heartFullColor : heartEmptyColor;
        }
    }

    void SetFlashAlpha(float alpha)
    {
        if (damageFlash == null)
            return;

        Color color = damageFlash.color;
        color.a = alpha;
        damageFlash.color = color;
    }

    IEnumerator DamageFlash()
    {
        // Unscaled time so the flash still finishes during the death slow-motion / freeze.
        for (float t = 0f; t < flashDuration; t += Time.unscaledDeltaTime)
        {
            SetFlashAlpha(Mathf.Lerp(0.5f, 0f, t / flashDuration));
            yield return null;
        }

        SetFlashAlpha(0f);
    }

    IEnumerator InvincibilityRoutine()
    {
        invincible = true;

        float elapsed = 0f;
        while (elapsed < damageCooldown && !LevelOver)
        {
            bool visible = Mathf.FloorToInt(elapsed / blinkInterval) % 2 == 1;
            SetVisible(visible);

            elapsed += Time.deltaTime;
            yield return null;
        }

        SetVisible(true);
        invincible = false;
    }

    void SetVisible(bool visible)
    {
        foreach (Renderer r in renderers)
        {
            if (r != null)
                r.enabled = visible;
        }
    }
}