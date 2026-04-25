using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("--- Audio Sources ---")]
    [SerializeField] private AudioSource musicSource;  // Müzik için
    [SerializeField] private AudioSource clickSource;  // Click efekti için
    [SerializeField] private AudioSource effectSource1; // Diğer efekt için
    [SerializeField] private AudioSource effectSource2; // Diğer efekt için
    [SerializeField] private AudioSource effectSource3; // Diğer efekt için
    [SerializeField] private AudioSource effectSource4; // Diğer efekt için
    [SerializeField] private AudioSource effectSource5; // Diğer efekt için

    [Header("--- Audio Clips ---")]
    public AudioClip backgroundMusic;
    public AudioClip clickClip;
    public AudioClip specialEffectClip1;
    public AudioClip specialEffectClip2;
    public AudioClip specialEffectClip3;
    public AudioClip specialEffectClip4;
    public AudioClip specialEffectClip5;


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
    public void PlaySpecialEffect1()
    {
        effectSource1.PlayOneShot(specialEffectClip1);
    }
    public void PlaySpecialEffect2()
    {
        effectSource2.PlayOneShot(specialEffectClip2);
    }
    public void PlaySpecialEffect3()
    {
        effectSource3.PlayOneShot(specialEffectClip3);
    }
    public void PlaySpecialEffect4()
    {
        effectSource4.PlayOneShot(specialEffectClip4);
    }
    public void PlaySpecialEffect5()
    {
        effectSource5.PlayOneShot(specialEffectClip5);
    }
}
