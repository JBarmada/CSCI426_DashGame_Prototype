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

        [Header("Vignette Settings")]
        [Tooltip("Color of the vignette (usually red)")]
        public Color vignetteColor = new Color(0.8f, 0f, 0f, 0.9f);
        
        [Tooltip("How quickly the vignette fades in (seconds)")]
        public float fadeInDuration = 0.1f;
        
        [Tooltip("How quickly the vignette fades out (seconds)")]
        public float fadeOutDuration = 0.5f;
        
        [Tooltip("Size of the transparent center (0-1, larger = more visibility in center)")]
        [Range(0f, 0.8f)]
        public float innerRadius = 0.4f;
        
        [Tooltip("How soft the edge is (0-1, larger = softer fade)")]
        [Range(0.1f, 0.8f)]
        public float edgeSoftness = 0.5f;

        private Texture2D vignetteTexture;
        private float currentAlpha = 0f;
        private float targetAlpha = 0f;
        private Coroutine fadeCoroutine;

        private void Awake()
        {
            Instance = this;
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
                    
                    pixels[y * textureSize + x] = new Color(
                        vignetteColor.r,
                        vignetteColor.g,
                        vignetteColor.b,
                        alpha * vignetteColor.a
                    );
                }
            }
            
            vignetteTexture.SetPixels(pixels);
            vignetteTexture.Apply();
        }

        private void OnGUI()
        {
            if (currentAlpha <= 0f || vignetteTexture == null) return;
            
            // Set the GUI color with current alpha
            GUI.color = new Color(1f, 1f, 1f, currentAlpha);
            
            // Draw the vignette texture to fill the entire screen
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignetteTexture, ScaleMode.StretchToFill);
            
            // Reset GUI color
            GUI.color = Color.white;
        }

        /// <summary>
        /// Show the vignette with a quick fade in
        /// </summary>
        public void Show()
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            
            fadeCoroutine = StartCoroutine(FadeTo(1f, fadeInDuration));
        }

        /// <summary>
        /// Hide the vignette with a fade out
        /// </summary>
        public void Hide()
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            
            fadeCoroutine = StartCoroutine(FadeTo(0f, fadeOutDuration));
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
