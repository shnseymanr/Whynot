using UnityEngine;

public class GameSoundManager : MonoBehaviour
{
    public static GameSoundManager Instance;

    [Header("--- Audio Sources ---")]
    [SerializeField] private AudioSource musicSource; 
    [SerializeField] private AudioSource sfxSource;   
    [SerializeField] private AudioSource walkSource;  

    [Header("--- Clips ---")]
    public AudioClip backgroundMusic;
    public AudioClip jumpClip;
    public AudioClip walkClip;
    public AudioClip normalShootClip;
    public AudioClip ultimateShootClip;
    public AudioClip collectSeedClip;
    public AudioClip plantSeedClip;
    public AudioClip wateringClip;
    public AudioClip damageClip;
    public AudioClip deathClip;

    private void Awake()
    {
        // SINGLETON VE SAHNE GEÇİŞİ KONTROLÜ
        if (Instance == null)
        {
            Instance = this;
            // Bu objenin sahneler arası yok olmasını engeller (Hata almanı önleyen asıl kısım)
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            // Eğer sahnede zaten bir Instance varsa, yeni geleni yok et
            Destroy(gameObject);
            return; // Kodun geri kalanının çalışmasını engelle
        }
    }

    void Start()
    {
        // Müzik ayarlarını ve yürüme klibini sadece bir kez hazırla
        InitializeAudio();
    }

    private void InitializeAudio()
    {
        if (backgroundMusic != null && musicSource != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            if (!musicSource.isPlaying) musicSource.Play();
        }

        if (walkSource != null)
        {
            walkSource.clip = walkClip;
            walkSource.loop = true;
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        // NULL CHECK: Obje veya Source yok edilmişse hatayı engelle
        if (clip == null || sfxSource == null) return;
        
        sfxSource.PlayOneShot(clip);
    }

    public void PlayWalk(bool isMoving)
    {
        // SAFE ACCESS: Source'un varlığını kontrol et (MissingReference hatasını bitirir)
        if (walkSource == null) return;

        if (isMoving)
        {
            if (!walkSource.isPlaying) walkSource.Play();
        }
        else
        {
            if (walkSource.isPlaying) walkSource.Stop();
        }
    }

    public void PlayDamageSFX()
    {
        if (damageClip == null || sfxSource == null) return;

        sfxSource.pitch = Random.Range(0.9f, 1.1f); 
        sfxSource.PlayOneShot(damageClip);
        sfxSource.pitch = 1f; 
    }
}