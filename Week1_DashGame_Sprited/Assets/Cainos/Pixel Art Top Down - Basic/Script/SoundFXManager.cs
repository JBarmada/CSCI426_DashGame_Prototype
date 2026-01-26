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

    
}
