using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("--- Audio Sources ---")]
    [SerializeField] private AudioSource musicSource;  // Müzik için
    [SerializeField] private AudioSource clickSource;  // Click efekti için
    [SerializeField] private AudioSource effectSource; // Diğer efekt için

    [Header("--- Audio Clips ---")]
    public AudioClip backgroundMusic;
    public AudioClip clickClip;
    public AudioClip specialEffectClip;

    void Start()
    {
        // 1. Arka plan müziğini ayarla ve başlat
        musicSource.clip = backgroundMusic;
        musicSource.loop = true; // Müziğin bitince tekrar etmesi için
        musicSource.playOnAwake = true; 
        musicSource.Play();
    }

    // Start butonu veya genel tıklamalar için fonksiyon
    public void PlayClickSound()
    {
        clickSource.PlayOneShot(clickClip);
    }

    // Diğer özel buton efekti için fonksiyon
    public void PlaySpecialEffect()
    {
        effectSource.PlayOneShot(specialEffectClip);
    }
}
