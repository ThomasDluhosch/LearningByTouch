using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource instructionSource;
    [SerializeField] private AudioSource backgroundMusicSource;

    public static AudioManager Instance { get; private set; }


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void PlayInstruction(AudioClip clip)
    {
        if (clip == null || instructionSource == null) return;

        instructionSource.Stop();
        instructionSource.clip = clip;
        instructionSource.Play();
    }


    public void PlayBackgroundMusic(AudioClip musicTrack)
    {
        if (musicTrack == null || backgroundMusicSource == null) return;

        backgroundMusicSource.clip = musicTrack;
        backgroundMusicSource.loop = true;
        backgroundMusicSource.Play();
    }
}
