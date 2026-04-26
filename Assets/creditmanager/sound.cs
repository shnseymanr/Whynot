using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // Sahne yönetimi için gerekli

public class CreditsAudioManager : MonoBehaviour
{
    [Header("--- Audio Setup ---")]
    [SerializeField] private AudioSource creditsSource;
    [SerializeField] private AudioClip creditsClip;

    [Header("--- Timing Settings ---")]
    [SerializeField] private float startDelay = 3f;   // Başta 3 saniye boşluk
    [SerializeField] private float musicDuration = 12f; // Müzik 12 saniye
    [SerializeField] private float endDelay = 5f;     // Bitişte 5 saniye boşluk

[Header("--- Scene Management ---")]
    [SerializeField] private string nextSceneName = "MainMenu"; // Geçilecek sahnenin tam adı

    void Start()
    {
        // Sahne açıldığında zamanlama mekanizmasını başlat
        StartCoroutine(PlayCreditsSequence());
    }

   IEnumerator PlayCreditsSequence()
    {
        // 1. Başlangıçtaki boşluk (3 saniye)
        yield return new WaitForSeconds(startDelay);

        // 2. Müziği Başlat
        if (creditsSource != null && creditsClip != null)
        {
            creditsSource.clip = creditsClip;
            creditsSource.Play();
        }

        // 3. Müziğin çalma süresi (12 saniye)
        yield return new WaitForSeconds(musicDuration);

        // 4. Müziği Durdur
        if (creditsSource != null)
        {
            creditsSource.Stop();
        }

        // 5. Bitişteki son boşluk (5 saniye)
        yield return new WaitForSeconds(endDelay);

        // 6. SAHNE GEÇİŞİ
        // Belirlediğin sahneye yönlendirir
        SceneManager.LoadScene(nextSceneName);
    }
}