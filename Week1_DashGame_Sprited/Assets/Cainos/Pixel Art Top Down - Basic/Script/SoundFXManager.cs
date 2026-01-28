using UnityEngine;

public class SoundFXManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static SoundFXManager Instance;
    [SerializeField] private AudioSource soundFXObject;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public AudioSource PlaySound(AudioClip clip, Transform spawnTransform, float volume = 1f)
    {
        // spawn the gameobject
        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);
        // assign audioclip
        audioSource.clip = clip;
        // assign volume
        audioSource.volume = volume;
        // play the sound
        audioSource.Play();
        // get length of clip
        float clipLength = audioSource.clip.length;
        // destroy gameobject after clip length
        Destroy(audioSource.gameObject, clipLength);

        return audioSource;
    }

     public AudioSource PlayRandomSound(AudioClip[] clips, Transform spawnTransform, float volume = 1f)
    {
        int rand = Random.Range(0, clips.Length);
        // spawn the gameobject
        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);
        // assign audioclip
        audioSource.clip = clips[rand];
        // assign volume
        audioSource.volume = volume;
        // play the sound
        audioSource.Play();
        // get length of clip
        float clipLength = audioSource.clip.length;
        // destroy gameobject after clip length
        Destroy(audioSource.gameObject, clipLength);

        return audioSource;
    }

    public AudioSource PlaySoundSegment(AudioClip clip, Transform spawnTransform, float startTime, float duration, float volume = 1f)
    {
        // spawn the gameobject
        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);
        // assign audioclip
        audioSource.clip = clip;
        // assign volume
        audioSource.volume = volume;
        // assign current time
        audioSource.time = startTime;
        // play the sound
        audioSource.Play();
        
        StartCoroutine(StopSoundDelayed(audioSource, duration));

        return audioSource;
    }

    private System.Collections.IEnumerator StopSoundDelayed(AudioSource audioSource, float duration)
    {
        // fade aggressively at the end
        float fadeDuration = 0.1f;
        if (duration < 0.2f) fadeDuration = duration * 0.5f; // Handle very short clips

        float runTime = duration - fadeDuration;
        
        if (runTime > 0)
            yield return new WaitForSeconds(runTime);

        if (audioSource != null)
        {
            float startVol = audioSource.volume;
            float t = 0;
            while (t < fadeDuration)
            {
                if (audioSource == null) yield break;
                t += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVol, 0f, t / fadeDuration);
                yield return null;
            }
            
            if (audioSource != null)
                Destroy(audioSource.gameObject);
        }
    }

    
}
