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
        private float shakeIntensity;

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
            
            // Apply screen shake
            if (shakeTimer > 0)
            {
                shakeTimer -= Time.deltaTime;
                float shakeX = Random.Range(-shakeIntensity, shakeIntensity);
                float shakeY = Random.Range(-shakeIntensity, shakeIntensity);
                finalPos += new Vector3(shakeX, shakeY, 0f);
            }
            
            transform.position = finalPos;
        }

        /// <summary>
        /// Shake the screen for a duration with given intensity
        /// </summary>
        public void Shake(float duration, float intensity)
        {
            shakeTimer = duration;
            shakeIntensity = intensity;
        }
    }
}
