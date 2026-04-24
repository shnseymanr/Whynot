using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum WorldRole
{
    Hud,
    VillageProgress,
    VillageEntrance,
    CaveExit,
    CameraCleanup,
    Fade
}

public class WorldController : MonoBehaviour
{
    [Header("Role")]
    [SerializeField] private WorldRole role = WorldRole.Hud;

    [Header("HUD")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider waterSlider;
    [SerializeField] private TMP_Text questText;
    [SerializeField] private TMP_Text villageStageText;
    [SerializeField] private TMP_Text collectedWaterText;
    [SerializeField] private TMP_Text activeTaskText;

    [Header("Village Progress")]
    [SerializeField] private GameObject dryField;
    [SerializeField] private GameObject wateredField;
    [SerializeField] private GameObject emptyDam;
    [SerializeField] private GameObject fullDam;
    [SerializeField] private GameObject electricityLights;
    [SerializeField] private GameObject raidedVillageObjects;
    [SerializeField] private EnemyController bossPrefab;
    [SerializeField] private Transform bossSpawnPoint;
    [SerializeField] private bool spawnBossOnlyOnce = true;

    [Header("Cave Exit")]
    [SerializeField] private bool requireFullWaterBar = true;
    [SerializeField] private float requiredWaterOverride = -1f;

    [Header("Camera Cleanup")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float viewportPadding = 0.2f;
    [SerializeField] private bool destroyProjectiles = true;
    [SerializeField] private bool destroyEnemies = true;

    [Header("Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.75f;
    [SerializeField] private bool startFadeClear = true;

    private static WorldController hudController;
    private EnemyController spawnedBoss;

    private void Awake()
    {
        if (role == WorldRole.Hud)
        {
            hudController = this;
            AutoAssignHud();
        }

        if (role == WorldRole.CameraCleanup && targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (role == WorldRole.Fade)
        {
            if (fadeImage == null)
            {
                GameObject found = GameObject.Find("Fade_Image");
                fadeImage = found != null ? found.GetComponent<Image>() : null;
            }

            if (startFadeClear)
            {
                SetFadeAlpha(0f);
            }
        }
    }

    private void Start()
    {
        ApplyCurrentState();
    }

    private void Update()
    {
        if (role == WorldRole.CameraCleanup)
        {
            CleanupOutsideCamera();
        }
    }

    private void OnDestroy()
    {
        if (hudController == this)
        {
            hudController = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        if (role == WorldRole.VillageEntrance)
        {
            GameManager.Instance?.GoToCave();
        }
        else if (role == WorldRole.CaveExit)
        {
            float requiredWater = requiredWaterOverride >= 0f ? requiredWaterOverride : player.RequiredWaterForExit;
            bool canExit = !requireFullWaterBar || player.CurrentWater >= requiredWater;

            if (canExit)
            {
                GameManager.Instance?.ReturnToVillage(player);
            }
            else
            {
                SetQuestText("Cikis icin daha fazla su gerekiyor.");
            }
        }
    }

    public void ApplyCurrentState()
    {
        if (role == WorldRole.Hud)
        {
            RefreshHud();
        }
        else if (role == WorldRole.VillageProgress && GameManager.Instance != null)
        {
            ApplyVillageStage(GameManager.Instance.CurrentVillageStage);
        }
    }

    public static void RefreshHud()
    {
        if (hudController == null)
        {
            hudController = FindHudController();
        }

        hudController?.RefreshHudInternal();
    }

    public static void SetQuestText(string text)
    {
        if (hudController == null)
        {
            hudController = FindHudController();
        }

        if (hudController == null)
        {
            return;
        }

        hudController.AutoAssignHud();

        if (hudController.questText != null)
        {
            hudController.questText.text = text;
        }

        if (hudController.activeTaskText != null)
        {
            hudController.activeTaskText.text = $"Aktif Gorev: {text}";
        }
    }

    public static WorldController FindFadeController()
    {
        foreach (WorldController controller in FindObjectsByType<WorldController>(FindObjectsSortMode.None))
        {
            if (controller.role == WorldRole.Fade)
            {
                return controller;
            }
        }

        return null;
    }

    public IEnumerator FadeOut()
    {
        yield return FadeTo(1f);
    }

    public IEnumerator FadeIn()
    {
        yield return FadeTo(0f);
    }

    private void RefreshHudInternal()
    {
        AutoAssignHud();

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            return;
        }

        if (healthSlider != null)
        {
            healthSlider.maxValue = Mathf.Max(1f, gameManager.MaxHealth);
            healthSlider.value = Mathf.Clamp(gameManager.CurrentHealth, 0f, healthSlider.maxValue);
        }

        if (waterSlider != null)
        {
            waterSlider.maxValue = Mathf.Max(1f, gameManager.MaxWater);
            waterSlider.value = Mathf.Clamp(gameManager.CurrentWater, 0f, waterSlider.maxValue);
        }

        string quest = gameManager.GetQuestText();

        if (questText != null)
        {
            questText.text = quest;
        }

        if (villageStageText != null)
        {
            villageStageText.text = $"Koy Asamasi: {(int)gameManager.CurrentVillageStage}";
        }

        if (collectedWaterText != null)
        {
            collectedWaterText.text = $"Toplanan Su: {gameManager.CurrentWater:0}/{gameManager.MaxWater:0}";
        }

        if (activeTaskText != null)
        {
            activeTaskText.text = $"Aktif Gorev: {quest}";
        }
    }

    private void ApplyVillageStage(VillageStage stage)
    {
        SetActive(dryField, stage == VillageStage.Dry);
        SetActive(wateredField, stage >= VillageStage.FieldWatered && stage != VillageStage.Raided);
        SetActive(emptyDam, stage < VillageStage.DamFilled);
        SetActive(fullDam, stage >= VillageStage.DamFilled && stage != VillageStage.Raided);
        SetActive(electricityLights, stage >= VillageStage.DamFilled && stage != VillageStage.Raided);
        SetActive(raidedVillageObjects, stage == VillageStage.Raided);

        if (stage == VillageStage.Raided)
        {
            SpawnBoss();
        }
    }

    private void SpawnBoss()
    {
        if (bossPrefab == null)
        {
            return;
        }

        if (spawnBossOnlyOnce && spawnedBoss != null)
        {
            return;
        }

        Vector3 position = bossSpawnPoint != null ? bossSpawnPoint.position : transform.position;
        Quaternion rotation = bossSpawnPoint != null ? bossSpawnPoint.rotation : Quaternion.identity;
        spawnedBoss = Instantiate(bossPrefab, position, rotation);
    }

    private void CleanupOutsideCamera()
    {
        if (targetCamera == null)
        {
            return;
        }

        if (destroyProjectiles)
        {
            foreach (WaterProjectile projectile in FindObjectsByType<WaterProjectile>(FindObjectsSortMode.None))
            {
                DestroyIfOutside(projectile.transform);
            }
        }

        if (destroyEnemies)
        {
            foreach (EnemyController enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            {
                if (enemy.CanTakeDamage)
                {
                    DestroyIfOutside(enemy.transform);
                }
            }
        }
    }

    private void DestroyIfOutside(Transform target)
    {
        Vector3 viewport = targetCamera.WorldToViewportPoint(target.position);
        bool outside = viewport.z < 0f || viewport.x < -viewportPadding || viewport.x > 1f + viewportPadding || viewport.y < -viewportPadding || viewport.y > 1f + viewportPadding;

        if (outside)
        {
            Destroy(target.gameObject);
        }
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (fadeImage == null)
        {
            yield break;
        }

        fadeImage.raycastTarget = true;
        float startAlpha = fadeImage.color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetFadeAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration));
            yield return null;
        }

        SetFadeAlpha(targetAlpha);
        fadeImage.raycastTarget = targetAlpha > 0.01f;
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null)
        {
            return;
        }

        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }

    private void AutoAssignHud()
    {
        healthSlider ??= FindComponentByName<Slider>("Health_Slider");
        waterSlider ??= FindComponentByName<Slider>("Water_Slider");
        questText ??= FindComponentByName<TMP_Text>("Quest_Text");
        villageStageText ??= FindComponentByName<TMP_Text>("VillageStage_Text");
        collectedWaterText ??= FindComponentByName<TMP_Text>("CollectedWater_Text");
        activeTaskText ??= FindComponentByName<TMP_Text>("ActiveTask_Text");
    }

    private static WorldController FindHudController()
    {
        foreach (WorldController controller in FindObjectsByType<WorldController>(FindObjectsSortMode.None))
        {
            if (controller.role == WorldRole.Hud)
            {
                return controller;
            }
        }

        return null;
    }

    private T FindComponentByName<T>(string objectName) where T : Component
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.GetComponent<T>() : null;
    }

    private void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}
