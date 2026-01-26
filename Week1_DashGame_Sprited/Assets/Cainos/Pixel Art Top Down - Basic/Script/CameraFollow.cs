using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    //let camera follow target
    public class CameraFollow : MonoBehaviour
    {
        public static CameraFollow Instance { get; private set; }
        
        public Transform target;
        public float lerpSpeed = 1.0f;

        private Vector3 offset;
        private Vector3 targetPos;
        
        // Screen shake
        private float shakeTimer;
        private float shakeDuration;
        private float shakeIntensity;
        
        // Impact jolt (applied directly, bypasses lerp)
        private Vector3 impactOffset;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (target == null) return;

            offset = transform.position - target.position;
        }

        private void Update()
        {
            if (target == null) return;

            targetPos = target.position + offset;
            Vector3 finalPos = Vector3.Lerp(transform.position, targetPos, lerpSpeed * Time.deltaTime);
            
            // Apply screen shake with rapid oscillation
            if (shakeTimer > 0)
            {
                shakeTimer -= Time.deltaTime;
                
                // Calculate progress (1 = just started, 0 = ending)
                float progress = shakeTimer / shakeDuration;
                
                // Use rapid sine wave oscillation for consistent shake regardless of duration
                // High frequency (80) ensures visible shake even in short durations
                float shakeX = Mathf.Sin(Time.time * 80f) * shakeIntensity * progress;
                float shakeY = Mathf.Cos(Time.time * 90f) * shakeIntensity * progress;
                
                finalPos += new Vector3(shakeX, shakeY, 0f);
            }
            
            // Apply impact offset directly (bypasses lerp for instant jolt)
            finalPos += impactOffset;
            
            transform.position = finalPos;
        }

        /// <summary>
        /// Shake the screen for a duration with given intensity.
        /// For short "hit" effects, use duration ~0.08-0.15 with intensity ~0.2-0.3
        /// </summary>
        public void Shake(float duration, float intensity)
        {
            shakeTimer = duration;
            shakeDuration = duration;
            shakeIntensity = intensity;
        }
        
        /// <summary>
        /// Impact shake - rapid decaying vibration.
        /// Good for hit effects. Intensity around 0.3-0.6 works well.
        /// </summary>
        /// <param name="intensity">How strong the shake is</param>
        /// <param name="duration">How long the shake lasts (default 0.15s)</param>
        public void ImpactShake(float intensity, float duration = 0.15f)
        {
            StartCoroutine(DoImpactShake(intensity, duration));
        }
        
        private IEnumerator DoImpactShake(float intensity, float duration)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                // Decay the intensity over time (starts strong, fades out)
                float currentIntensity = intensity * (1f - progress);
                
                // Rapid oscillating shake
                float shakeX = Mathf.Sin(Time.time * 120f) * currentIntensity;
                float shakeY = Mathf.Cos(Time.time * 130f) * currentIntensity;
                
                impactOffset = new Vector3(shakeX, shakeY, 0f);
                
                yield return null;
            }
            
            // Reset
            impactOffset = Vector3.zero;
        }
    }
}
