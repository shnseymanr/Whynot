using UnityEngine;
using UnityEngine.UI; // Slider bileşeni için gerekli

public class pausemenu : MonoBehaviour
{
    [Header("--- Audio Sources ---")]
    [SerializeField] private AudioSource musicSource; 
    [SerializeField] private AudioSource clickSource; 
    
    [Header("--- Audio Clips ---")]
    public AudioClip backgroundMusic;
    public AudioClip clickClip;

    [Header("--- UI Panels & Buttons ---")]
    [SerializeField] GameObject darkPanel;
    [SerializeField] GameObject pauseMenus;
    [SerializeField] GameObject settingsMenu;
    [SerializeField] GameObject pauseButton; // Ekranda hep duran buton

    [Header("--- Slider Settings ---")]
    [SerializeField] private Slider volumeSlider; // Inspector'dan Slider'ı buraya sürükle

    void Start()
    {
        // Başlangıç ayarları
        Time.timeScale = 1f;
        pauseButton.SetActive(true);
        pauseMenus.SetActive(false);
        darkPanel.SetActive(false);
        settingsMenu.SetActive(false);

        // Müzik ayarı
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.Stop(); 
        }

        // Kayıtlı ses seviyesini yükle (Yoksa %100 aç)
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.volume = savedVolume;

        // Slider'ın çubuğunu mevcut ses seviyesine eşitle
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = savedVolume;
        }
    }

    // --- GENEL SES KONTROLÜ (SLIDER İÇİN) ---
    public void SetMasterVolume(float value)
    {
        // Slider kaydırıldığında bu fonksiyon çalışır
        AudioListener.volume = value;
        
        // Ses seviyesini kalıcı olarak kaydet
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    private void PlayClickSound()
    {
        if (clickSource != null && clickClip != null)
            clickSource.PlayOneShot(clickClip);
    }

    // --- MENÜ NAVİGASYONU ---

    public void Pause()
    {
        PlayClickSound();
        pauseMenus.SetActive(true);
        darkPanel.SetActive(true);
        pauseButton.SetActive(false); 

        if (musicSource != null) musicSource.Play(); 
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        PlayClickSound();
        pauseMenus.SetActive(false);
        darkPanel.SetActive(false);
        settingsMenu.SetActive(false);
        pauseButton.SetActive(true); 

        if (musicSource != null) musicSource.Stop(); 
        Time.timeScale = 1f;
    }

    public void Settings()
    {
        PlayClickSound();
        settingsMenu.SetActive(true);
        pauseMenus.SetActive(false);
    }

    public void Back()
    {
        PlayClickSound();
        settingsMenu.SetActive(false);
        pauseMenus.SetActive(true);
    }

    public void Quit()
    {
        PlayClickSound();
        Debug.Log("Oyundan çıkılıyor...");
        Application.Quit(); 
    }
}