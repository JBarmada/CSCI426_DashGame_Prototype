using UnityEngine;
using System.Collections;

namespace Cainos.PixelArtTopDown_Basic
{
    /// <summary>
    /// Creates a red vignette overlay that appears when taking damage.
    /// Uses OnGUI for rendering (no UI package required).
    /// </summary>
    public class DamageVignette : MonoBehaviour
    {
        public static DamageVignette Instance { get; private set; }

        [Header("Damage Vignette (Negative - Pillars)")]
        [Tooltip("Color for damage/negative effects (usually red)")]
        public Color damageColor = new Color(0.8f, 0f, 0f, 0.9f);
        
        [Tooltip("How quickly the damage vignette fades in (seconds)")]
        public float damageFadeInDuration = 0.1f;
        
        [Tooltip("How quickly the damage vignette fades out (seconds)")]
        public float damageFadeOutDuration = 0.5f;

        [Header("Hit Vignette (Positive - Boxes/Pots)")]
        [Tooltip("Color for hit/positive effects (e.g., white, yellow, or light blue)")]
        public Color hitColor = new Color(1f, 1f, 1f, 0.7f);
        
        [Tooltip("How quickly the hit vignette fades in (seconds, 0 = instant)")]
        public float hitFadeInDuration = 0f;
        
        [Tooltip("How quickly the hit vignette fades out (seconds)")]
        public float hitFadeOutDuration = 0.1f;
        
        [Header("Shape Settings")]
        [Tooltip("Size of the transparent center (0-1, larger = more visibility in center)")]
        [Range(0f, 0.8f)]
        public float innerRadius = 0.4f;
        
        [Tooltip("How soft the edge is (0-1, larger = softer fade)")]
        [Range(0.1f, 0.8f)]
        public float edgeSoftness = 0.5f;

        private Texture2D vignetteTexture;
        private float currentAlpha = 0f;
        private Color currentColor;
        private Coroutine fadeCoroutine;

        private void Awake()
        {
            Instance = this;
            currentColor = damageColor;
            GenerateVignetteTexture();
        }

        private void GenerateVignetteTexture()
        {
            int textureSize = 512;
            vignetteTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            vignetteTexture.filterMode = FilterMode.Bilinear;
            
            Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);
            float maxDistance = textureSize / 2f;
            
            Color[] pixels = new Color[textureSize * textureSize];
            
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    // Calculate distance from center (normalized 0-1)
                    float distance = Vector2.Distance(new Vector2(x, y), center) / maxDistance;
                    
                    // Calculate alpha based on distance
                    // Inside innerRadius = fully transparent
                    // Outside (innerRadius + edgeSoftness) = fully opaque
                    float alpha = 0f;
                    
                    if (distance > innerRadius)
                    {
                        // Smooth interpolation from inner to outer edge
                        float t = (distance - innerRadius) / edgeSoftness;
                        alpha = Mathf.Clamp01(t);
                        
                        // Apply easing for smoother look
                        alpha = alpha * alpha; // Quadratic ease-in
                    }
                    
                    // Use white - we'll tint it with GUI.color at render time
                    pixels[y * textureSize + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            
            vignetteTexture.SetPixels(pixels);
            vignetteTexture.Apply();
        }

        private void OnGUI()
        {
            if (currentAlpha <= 0f || vignetteTexture == null) return;
            
            // Set the GUI color with current color and alpha
            GUI.color = new Color(currentColor.r, currentColor.g, currentColor.b, currentAlpha * currentColor.a);
            
            // Draw the vignette texture to fill the entire screen
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignetteTexture, ScaleMode.StretchToFill);
            
            // Reset GUI color
            GUI.color = Color.white;
        }

        /// <summary>
        /// Show the vignette with a fade in (uses damage color)
        /// </summary>
        public void Show()
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            
            currentColor = damageColor;
            fadeCoroutine = StartCoroutine(FadeTo(1f, damageFadeInDuration));
        }

        /// <summary>
        /// Hide the vignette with a fade out (uses damage fade out)
        /// </summary>
        public void Hide()
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            
            fadeCoroutine = StartCoroutine(FadeTo(0f, damageFadeOutDuration));
        }

        /// <summary>
        /// Instantly show the vignette (no fade)
        /// </summary>
        public void ShowInstant()
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            
            currentAlpha = 1f;
        }

        /// <summary>
        /// Instantly hide the vignette (no fade)
        /// </summary>
        public void HideInstant()
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            
            currentAlpha = 0f;
        }

        /// <summary>
        /// Quick impact flash for positive feedback (destroying objects).
        /// Uses hit color and hit fade settings from Inspector.
        /// </summary>
        public void Flash()
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            
            currentColor = hitColor;
            fadeCoroutine = StartCoroutine(DoFlash(hitFadeInDuration, hitFadeOutDuration));
        }

        /// <summary>
        /// Quick impact flash with a custom color.
        /// </summary>
        public void Flash(Color color)
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            
            currentColor = color;
            fadeCoroutine = StartCoroutine(DoFlash(hitFadeInDuration, hitFadeOutDuration));
        }

        private IEnumerator DoFlash(float fadeInTime, float fadeOutTime)
        {
            // Fade in (or instant if fadeInTime is 0)
            if (fadeInTime > 0f)
            {
                float elapsed = 0f;
                while (elapsed < fadeInTime)
                {
                    elapsed += Time.deltaTime;
                    currentAlpha = Mathf.Lerp(0f, 1f, elapsed / fadeInTime);
                    yield return null;
                }
            }
            currentAlpha = 1f;
            
            // Fade out
            float elapsedOut = 0f;
            while (elapsedOut < fadeOutTime)
            {
                elapsedOut += Time.deltaTime;
                currentAlpha = Mathf.Lerp(1f, 0f, elapsedOut / fadeOutTime);
                yield return null;
            }
            
            currentAlpha = 0f;
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            float startAlpha = currentAlpha;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                currentAlpha = Mathf.Lerp(startAlpha, target, elapsed / duration);
                yield return null;
            }
            
            currentAlpha = target;
        }

        private void OnDestroy()
        {
            if (vignetteTexture != null)
            {
                Destroy(vignetteTexture);
            }
        }
    }
}
