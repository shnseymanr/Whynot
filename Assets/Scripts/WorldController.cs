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
    Fade,
    CameraFollow
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
    [SerializeField] private TMP_Text plantInventoryText;
    [SerializeField] private TMP_Text projectileModeText;
    [SerializeField] private TMP_Text iceShotsText;
    [SerializeField] private TMP_Text ultimateText;
    [SerializeField] private Image ultimateFillImage;
    private Image ultimateBackgroundImage;

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

    [Header("Camera Follow")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 7f, -10f);
    [SerializeField] private float followSmoothTime = 0.03f;

    [Header("Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.75f;
    [SerializeField] private bool startFadeClear = true;

    private static WorldController hudController;
    private static Sprite circleSprite;
    private EnemyController spawnedBoss;
    private Vector3 followVelocity;

    private void Awake()
    {
        if (role == WorldRole.Hud)
        {
            hudController = this;
            EnsureScreenSpaceHud();
            AutoAssignHud();
            ArrangeHudLayout();
        }

        if (role == WorldRole.CameraCleanup && targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (role == WorldRole.CameraFollow)
        {
            AutoAssignCameraFollowTarget();
        }

        if (role == WorldRole.Fade)
        {
            if (fadeImage == null)
            {
                GameObject found = GameObject.Find("Fade_Image");
                fadeImage = found != null ? found.GetComponent<Image>() : null;
            }

            if (fadeImage != null && !fadeImage.gameObject.activeSelf)
            {
                fadeImage.gameObject.SetActive(true);
            }

            if (startFadeClear)
            {
                SetFadeAlpha(0f);
                if (fadeImage != null)
                {
                    fadeImage.raycastTarget = false;
                }
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

    private void LateUpdate()
    {
        if (role == WorldRole.CameraFollow)
        {
            FollowPlayer();
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
        hudController.ArrangeHudLayout();

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
        ArrangeHudLayout();

        GameManager gameManager = GameManager.Instance;
        PlayerController player = FindFirstObjectByType<PlayerController>();
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

        if (plantInventoryText != null)
        {
            plantInventoryText.text = gameManager.GetPlantInventoryText();
        }

        if (player != null)
        {
            if (projectileModeText != null)
            {
                projectileModeText.text = $"Mod: {player.ActiveProjectileMode}";
            }

            if (iceShotsText != null)
            {
                iceShotsText.text = player.IsIceModeActive ? $"Buz Atis: {player.IceShotsRemaining}" : "Buz Atis: -";
            }

            if (ultimateText != null)
            {
                ultimateText.text = player.IsUltimateReady ? "V" : $"{Mathf.CeilToInt(player.UltimateCharge01 * 100f)}%";
            }

            if (ultimateFillImage != null)
            {
                ultimateFillImage.fillAmount = player.UltimateCharge01;
                ultimateFillImage.color = player.IsUltimateReady
                    ? new Color(0.45f, 0.95f, 1f, 1f)
                    : new Color(0.18f, 0.55f, 0.75f, 1f);
            }
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

    private void FollowPlayer()
    {
        AutoAssignCameraFollowTarget();
        if (followTarget == null)
        {
            return;
        }

        Vector3 targetPosition = followTarget.position + followOffset;
        transform.position = followSmoothTime <= 0f
            ? targetPosition
            : Vector3.SmoothDamp(transform.position, targetPosition, ref followVelocity, followSmoothTime);

        if (followOffset.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(-followOffset.normalized, Vector3.up);
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
        EnsureHudCards();
        healthSlider ??= FindComponentByName<Slider>("Health_Slider");
        waterSlider ??= FindComponentByName<Slider>("Water_Slider");
        questText ??= FindComponentByName<TMP_Text>("Quest_Text");
        villageStageText ??= FindComponentByName<TMP_Text>("VillageStage_Text");
        collectedWaterText ??= FindComponentByName<TMP_Text>("CollectedWater_Text");
        activeTaskText ??= FindComponentByName<TMP_Text>("ActiveTask_Text");
        plantInventoryText ??= FindComponentByName<TMP_Text>("PlantInventory_Text");
        EnsureUltimateHud();
    }

    private void EnsureHudCards()
    {
        EnsureHudCard("HUD_HealthCard");
        EnsureHudCard("HUD_QuestCard");
        EnsureHudCard("HUD_WaterCard");
        EnsureHudCard("HUD_StatusCard");
        EnsureHudCard("HUD_PlantCard");
    }

    private void EnsureHudCard(string objectName)
    {
        Transform existing = transform.Find(objectName);
        if (existing != null)
        {
            existing.SetAsFirstSibling();
            return;
        }

        GameObject card = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(transform, false);
        card.transform.SetAsFirstSibling();
    }

    private void ArrangeHudLayout()
    {
        ConfigureRect("HUD_Panel", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));

        ConfigureRect("HUD_HealthCard", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -14f), new Vector2(350f, 64f));
        ConfigureRect("HUD_QuestCard", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(460f, 64f));
        ConfigureRect("HUD_WaterCard", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -14f), new Vector2(350f, 64f));
        ConfigureRect("HUD_StatusCard", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f), Vector2.zero);
        ConfigureRect("HUD_PlantCard", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(220f, 420f));

        ConfigureRect("Health_Label", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -22f), new Vector2(140f, 20f));
        ConfigureRect("Health_Slider", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -46f), new Vector2(300f, 16f));

        ConfigureRect("Water_Label", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -22f), new Vector2(140f, 20f));
        ConfigureRect("Water_Slider", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -46f), new Vector2(300f, 16f));

        ConfigureRect("Quest_Text", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(420f, 24f));
        ConfigureRect("ActiveTask_Text", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(420f, 18f));

        ConfigureRect("VillageStage_Text", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -82f), new Vector2(260f, 18f));
        ConfigureRect("CollectedWater_Text", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -82f), new Vector2(280f, 18f));
        ConfigureRect("PlantInventory_Text", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -16f), new Vector2(190f, 380f));
        ConfigureRect("UltimateHUD_Group", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 28f), new Vector2(230f, 86f));
        ConfigureRect("ProjectileMode_Text", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 54f), new Vector2(140f, 22f));
        ConfigureRect("IceShots_Text", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 30f), new Vector2(140f, 22f));
        ConfigureRect("Ultimate_Text", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-38f, 38f), new Vector2(64f, 40f));

        StyleHudControls();
    }

    private void EnsureScreenSpaceHud()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
        canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 10);
    }

    private void ConfigureRect(string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject found = GameObject.Find(objectName);
        RectTransform rect = found != null ? found.GetComponent<RectTransform>() : null;
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void StyleHudControls()
    {
        Image panelImage = FindComponentByName<Image>("HUD_Panel");
        if (panelImage != null)
        {
            panelImage.color = new Color(0f, 0f, 0f, 0f);
        }

        StyleCard("HUD_HealthCard");
        StyleCard("HUD_QuestCard");
        StyleCard("HUD_WaterCard");
        StyleCard("HUD_StatusCard", 0f);
        StyleCard("HUD_PlantCard", 0.62f);

        StyleSlider(healthSlider, new Color(0.9f, 0.18f, 0.16f, 1f));
        StyleSlider(waterSlider, new Color(0.1f, 0.55f, 0.95f, 1f));

        StyleText(questText, 22f, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 1f));
        StyleText(activeTaskText, 15f, TextAlignmentOptions.Center, new Color(0.78f, 0.82f, 0.86f, 1f));
        StyleText(villageStageText, 15f, TextAlignmentOptions.Left, new Color(0.78f, 0.82f, 0.86f, 1f));
        StyleText(collectedWaterText, 15f, TextAlignmentOptions.Right, new Color(0.78f, 0.82f, 0.86f, 1f));
        StyleText(plantInventoryText, 14f, TextAlignmentOptions.TopLeft, new Color(0.78f, 1f, 0.78f, 1f));

        StyleText(FindComponentByName<TMP_Text>("Health_Label"), 18f, TextAlignmentOptions.Left, new Color(1f, 0.9f, 0.9f, 1f));
        StyleText(FindComponentByName<TMP_Text>("Water_Label"), 18f, TextAlignmentOptions.Right, new Color(0.86f, 0.94f, 1f, 1f));
        StyleText(projectileModeText, 16f, TextAlignmentOptions.Left, new Color(0.86f, 0.94f, 1f, 1f));
        StyleText(iceShotsText, 14f, TextAlignmentOptions.Left, new Color(0.78f, 0.86f, 0.92f, 1f));
        StyleText(ultimateText, 16f, TextAlignmentOptions.Center, Color.white);
    }

    private void StyleCard(string objectName, float alpha = 0.7f)
    {
        Image image = FindComponentByName<Image>(objectName);
        if (image == null)
        {
            return;
        }

        image.color = new Color(0.015f, 0.018f, 0.024f, alpha);
        image.raycastTarget = false;
    }

    private void StyleSlider(Slider slider, Color fillColor)
    {
        if (slider == null)
        {
            return;
        }

        Image background = slider.targetGraphic as Image;
        if (background != null)
        {
            background.color = new Color(0.02f, 0.024f, 0.03f, 0.95f);
        }

        Image fill = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
        if (fill != null)
        {
            fill.color = fillColor;
        }
    }

    private void StyleText(TMP_Text text, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        if (text == null)
        {
            return;
        }

        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = text == plantInventoryText;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void AutoAssignCameraFollowTarget()
    {
        if (followTarget != null)
        {
            return;
        }

        PlayerController player = FindFirstObjectByType<PlayerController>();
        followTarget = player != null ? player.transform : null;
    }

    private void EnsureUltimateHud()
    {
        Transform plantParent = transform.Find("HUD_PlantCard") ?? transform;
        plantInventoryText ??= EnsureText(plantParent, "PlantInventory_Text");
        if (plantInventoryText != null && plantInventoryText.transform.parent != plantParent)
        {
            plantInventoryText.transform.SetParent(plantParent, false);
        }

        if (projectileModeText != null && iceShotsText != null && ultimateText != null && ultimateFillImage != null)
        {
            return;
        }

        Transform root = transform.Find("UltimateHUD_Group");
        if (root == null)
        {
            GameObject rootObject = new GameObject("UltimateHUD_Group", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rootObject.transform.SetParent(transform, false);
            root = rootObject.transform;
        }

        StyleCard("UltimateHUD_Group", 0.72f);
        ultimateFillImage ??= EnsureImage(root, "Ultimate_Fill", new Color(0.18f, 0.55f, 0.75f, 1f), true);
        ultimateBackgroundImage ??= EnsureImage(root, "Ultimate_Background", new Color(0.02f, 0.024f, 0.03f, 0.95f), false);
        projectileModeText ??= EnsureText(root, "ProjectileMode_Text");
        iceShotsText ??= EnsureText(root, "IceShots_Text");
        ultimateText ??= EnsureText(root, "Ultimate_Text");

        if (ultimateBackgroundImage != null)
        {
            ultimateBackgroundImage.transform.SetAsFirstSibling();
        }

        if (ultimateFillImage != null)
        {
            ultimateFillImage.transform.SetSiblingIndex(1);
        }

        if (ultimateText != null)
        {
            ultimateText.transform.SetAsLastSibling();
        }
    }

    private Image EnsureImage(Transform parent, string objectName, Color color, bool filled)
    {
        Transform existing = parent.Find(objectName);
        GameObject target = existing != null ? existing.gameObject : new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        target.transform.SetParent(parent, false);

        RectTransform rect = target.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-38f, 38f);
        rect.sizeDelta = new Vector2(64f, 64f);

        Image image = target.GetComponent<Image>();
        image.sprite = GetCircleSprite();
        image.color = color;
        image.type = filled ? Image.Type.Filled : Image.Type.Simple;
        image.fillMethod = Image.FillMethod.Radial360;
        image.fillOrigin = 2;
        image.fillClockwise = true;
        image.fillAmount = filled ? 1f : 1f;
        image.raycastTarget = false;
        return image;
    }

    private TMP_Text EnsureText(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        GameObject target = existing != null ? existing.gameObject : new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        target.transform.SetParent(parent, false);
        return target.GetComponent<TMP_Text>();
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
        {
            return circleSprite;
        }

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? Color.white : clear);
            }
        }

        texture.Apply();
        circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return circleSprite;
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
