using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;
    
    [Header("Fade Settings")]
    public Image fadeImage; // Black image covering screen
    public float fadeSpeed = 1.4f;
    public float blackScreenHoldTime = 0.25f;
    
    private bool isFading = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Ensure fade image starts transparent
        if (fadeImage != null)
        {
            fadeImage.color = new Color(0, 0, 0, 0);
            fadeImage.gameObject.SetActive(false);
        }
    }
    
    public IEnumerator FadeToBlack(System.Action onComplete)
    {
        if (isFading || fadeImage == null) yield break;
        isFading = true;
        
        fadeImage.gameObject.SetActive(true);
        float alpha = 0f;
        
        while (alpha < 1f)
        {
            alpha += Time.deltaTime * fadeSpeed;
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        
        fadeImage.color = new Color(0, 0, 0, 1);
        
        // Do the action
        onComplete?.Invoke();
        
        // Fade back
        yield return new WaitForSeconds(blackScreenHoldTime);
        
        while (alpha > 0f)
        {
            alpha -= Time.deltaTime * fadeSpeed;
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        
        fadeImage.color = new Color(0, 0, 0, 0);
        fadeImage.gameObject.SetActive(false);
        
        isFading = false;
    }

    // Same as FadeToBlack, but keeps screen black for a custom hold duration.
    public IEnumerator FadeToBlackWithHold(System.Action onComplete, float holdDuration)
    {
        if (isFading || fadeImage == null) yield break;
        isFading = true;

        fadeImage.gameObject.SetActive(true);
        float alpha = 0f;

        while (alpha < 1f)
        {
            alpha += Time.deltaTime * fadeSpeed;
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, 1);

        onComplete?.Invoke();

        float hold = Mathf.Max(0f, holdDuration);
        if (hold > 0f)
        {
            yield return new WaitForSeconds(hold);
        }

        while (alpha > 0f)
        {
            alpha -= Time.deltaTime * fadeSpeed;
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, 0);
        fadeImage.gameObject.SetActive(false);

        isFading = false;
    }
}
