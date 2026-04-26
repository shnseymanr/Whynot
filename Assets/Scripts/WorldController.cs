using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    [SerializeField] private bool rotateCameraWithTarget = true;
    [SerializeField] private bool rotateFollowOffsetWithTarget;

    [Header("Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.75f;
    [SerializeField] private bool startFadeClear = true;

    [Header("HUD Manual Layout")]
    [SerializeField] private bool rebuildHudOnAwake;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image waterFillImage;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider waterSlider;
    [SerializeField] private TMP_Text questText;
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private TMP_Text waterCountText;
    [SerializeField] private TMP_Text inventoryText;
    [SerializeField] private TMP_Text modeText;
    [SerializeField] private TMP_Text iceText;
    [SerializeField] private TMP_Text ultimateText;
    [SerializeField] private Image ultimateFillImage;
    [SerializeField] private float healthBarChangeSpeed = 0.55f;
    [SerializeField] private float waterBarChangeSpeed = 1.8f;

    private static WorldController hudController;
    private static Sprite circleSprite;
    private static Sprite inventoryPanelSprite;

    private readonly List<Button> inventorySlots = new List<Button>();
    private readonly List<Image> healthFillImages = new List<Image>();
    private readonly List<Image> waterFillImages = new List<Image>();
    private readonly List<Slider> healthSliders = new List<Slider>();
    private readonly List<Slider> waterSliders = new List<Slider>();

    private EnemyController spawnedBoss;
    private Vector3 followVelocity;
    private bool hasCameraFollowBaseY;
    private float cameraFollowBaseY;
    private Quaternion cameraFollowStartRotation;
    private Vector3 cameraFollowStartEuler;
    private Vector3 cameraFollowInitialOffset;
    private float targetHealthFill = 1f;
    private float targetWaterFill;
    private float displayedHealthFill = -1f;
    private float displayedWaterFill = -1f;
    private bool isPersistentHud;

    private void Awake()
    {
        if (role == WorldRole.Hud)
        {
            if (!RegisterPersistentHud())
            {
                return;
            }

            EnsureEventSystem();
            EnsureScreenSpaceHud();
            if (rebuildHudOnAwake)
            {
                BuildCleanHud();
            }
            else
            {
                TryBindExistingHud();
            }
        }

        if (role == WorldRole.CameraCleanup && targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (role == WorldRole.CameraFollow)
        {
            rotateFollowOffsetWithTarget = false;
            cameraFollowStartRotation = transform.rotation;
            cameraFollowStartEuler = transform.eulerAngles;
            AutoAssignCameraFollowTarget();
            if (followTarget != null)
            {
                cameraFollowInitialOffset = transform.position - followTarget.position;
                followOffset = cameraFollowInitialOffset;
            }
        }

        if (role == WorldRole.Fade)
        {
            SetupFade();
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

        if (role == WorldRole.Hud)
        {
            AnimateHudBars();
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

    private bool RegisterPersistentHud()
    {
        if (hudController != null && hudController != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return false;
        }

        hudController = this;
        if (!isPersistentHud)
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            isPersistentHud = true;
        }

        return true;
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

        hudController.EnsureHudReady();
        if (hudController.questText != null)
        {
            hudController.questText.text = text;
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
        EnsureHudReady();

        GameManager gameManager = GameManager.Instance;
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (gameManager == null)
        {
            return;
        }

        float currentHealth = player != null ? player.CurrentHealth : gameManager.CurrentHealth;
        float maxHealth = player != null ? player.MaxHealth : gameManager.MaxHealth;
        float currentWater = player != null ? player.CurrentWater : gameManager.CurrentWater;
        float maxWater = player != null ? player.MaxWater : gameManager.MaxWater;

        bool hasHealthBar = healthFillImage != null || healthSlider != null;
        bool hasWaterBar = waterFillImage != null || waterSlider != null;

        if (hasHealthBar)
        {
            targetHealthFill = Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth));
            if (displayedHealthFill < 0f)
            {
                displayedHealthFill = targetHealthFill;
                SetHudBarFill(healthFillImage, healthSlider, healthFillImages, healthSliders, displayedHealthFill);
            }
        }

        if (hasWaterBar)
        {
            targetWaterFill = Mathf.Clamp01(currentWater / Mathf.Max(1f, maxWater));
            if (displayedWaterFill < 0f)
            {
                displayedWaterFill = targetWaterFill;
                SetHudBarFill(waterFillImage, waterSlider, waterFillImages, waterSliders, displayedWaterFill);
            }
        }

        string quest = gameManager.GetQuestText();
        if (questText != null)
        {
            questText.text = quest;
        }

        if (stageText != null)
        {
            stageText.text = $"Koy Asamasi: {(int)gameManager.CurrentVillageStage}";
        }

        if (waterCountText != null)
        {
            waterCountText.text = $"Toplanan Su: {currentWater:0}/{maxWater:0}";
        }

        if (inventoryText != null)
        {
            inventoryText.text = GetInventoryHudText(gameManager);
        }
        RefreshInventorySlots(gameManager);

        if (player == null)
        {
            if (modeText != null)
            {
                modeText.text = "Mod: -";
            }

            if (iceText != null)
            {
                iceText.text = "Buz Atis: -";
            }

            if (ultimateText != null)
            {
                ultimateText.text = "-";
            }

            if (ultimateFillImage != null)
            {
                ultimateFillImage.fillAmount = 0f;
            }
            return;
        }

        if (modeText != null)
        {
            modeText.text = $"Mod: {player.ActiveProjectileMode}";
        }

        if (iceText != null)
        {
            iceText.text = player.IsIceModeActive ? $"Buz Atis: {player.IceShotsRemaining}" : "Buz Atis: -";
        }

        if (ultimateText != null)
        {
            ultimateText.text = player.IsUltimateReady ? "V" : $"{Mathf.CeilToInt(player.UltimateCharge01 * 100f)}%";
        }

        if (ultimateFillImage != null)
        {
            ultimateFillImage.fillAmount = player.UltimateCharge01;
            ultimateFillImage.color = player.IsUltimateReady
                ? new Color(0.42f, 0.94f, 1f, 1f)
                : new Color(0.12f, 0.45f, 0.7f, 1f);
        }
    }

    private void AnimateHudBars()
    {
        if ((healthFillImage != null || healthSlider != null) && displayedHealthFill >= 0f)
        {
            displayedHealthFill = Mathf.MoveTowards(
                displayedHealthFill,
                targetHealthFill,
                Mathf.Max(0.01f, healthBarChangeSpeed) * Time.deltaTime);
            SetHudBarFill(healthFillImage, healthSlider, healthFillImages, healthSliders, displayedHealthFill);
        }

        if ((waterFillImage != null || waterSlider != null) && displayedWaterFill >= 0f)
        {
            displayedWaterFill = Mathf.MoveTowards(
                displayedWaterFill,
                targetWaterFill,
                Mathf.Max(0.01f, waterBarChangeSpeed) * Time.deltaTime);
            SetHudBarFill(waterFillImage, waterSlider, waterFillImages, waterSliders, displayedWaterFill);
        }
    }

    private void SetHudBarFill(Image fillImage, Slider slider, List<Image> fillImages, List<Slider> sliders, float fillAmount)
    {
        fillAmount = Mathf.Clamp01(fillAmount);

        SetSingleSliderFill(slider, fillAmount);
        SetSingleImageFill(fillImage, fillAmount);

        foreach (Slider candidateSlider in sliders)
        {
            SetSingleSliderFill(candidateSlider, fillAmount);
        }

        foreach (Image candidateImage in fillImages)
        {
            SetSingleImageFill(candidateImage, fillAmount);
        }
    }

    private void SetSingleSliderFill(Slider slider, float fillAmount)
    {
        if (slider == null)
        {
            return;
        }

        slider.SetValueWithoutNotify(Mathf.Lerp(slider.minValue, slider.maxValue, fillAmount));
        SetFillRectWidth(slider.fillRect, fillAmount);
    }

    private void SetSingleImageFill(Image fillImage, float fillAmount)
    {
        if (fillImage == null)
        {
            return;
        }

        fillImage.fillAmount = fillAmount;
        SetFillRectWidth(fillImage.rectTransform, fillAmount);
    }

    private void SetFillRectWidth(RectTransform fillRect, float fillAmount)
    {
        if (fillRect == null)
        {
            return;
        }

        fillAmount = Mathf.Clamp01(fillAmount);
        fillRect.pivot = new Vector2(0f, fillRect.pivot.y);
        fillRect.anchorMin = new Vector2(0f, fillRect.anchorMin.y);
        fillRect.anchorMax = new Vector2(fillAmount, fillRect.anchorMax.y);
        fillRect.offsetMin = new Vector2(0f, fillRect.offsetMin.y);
        fillRect.offsetMax = new Vector2(0f, fillRect.offsetMax.y);
        fillRect.localScale = new Vector3(1f, fillRect.localScale.y, fillRect.localScale.z);
    }

    private void EnsureHudReady()
    {
        EnsureScreenSpaceHud();
        TryBindExistingHud();

        bool hasHealthBar = healthFillImage != null || healthSlider != null;
        bool hasWaterBar = waterFillImage != null || waterSlider != null;
        if (questText != null && hasHealthBar && hasWaterBar && inventorySlots.Count > 0)
        {
            return;
        }

        if (rebuildHudOnAwake && (questText == null || !hasHealthBar || !hasWaterBar || inventorySlots.Count == 0))
        {
            BuildCleanHud();
        }
    }

    private void BuildCleanHud()
    {
        ClearOldHudChildren();
        inventorySlots.Clear();

        RectTransform root = GetComponent<RectTransform>();
        if (root == null)
        {
            return;
        }

        Image panel = GetComponent<Image>();
        if (panel != null)
        {
            panel.color = Color.clear;
            panel.raycastTarget = false;
        }

        healthFillImage = CreateBar(root, "Can", Anchor.TopLeft, new Vector2(16f, -14f), new Color(0.9f, 0.16f, 0.14f, 1f), 1f);
        waterFillImage = CreateBar(root, "Su", Anchor.TopRight, new Vector2(-16f, -14f), new Color(0.08f, 0.55f, 0.95f, 1f), 0f);

        RectTransform questCard = CreatePanel(root, "HUD_Quest", Anchor.TopCenter, new Vector2(0f, -14f), new Vector2(500f, 82f), 0.72f);
        questText = CreateText(questCard, "Quest_Text", Anchor.TopCenter, new Vector2(0f, -12f), new Vector2(450f, 28f), 20f, TextAlignmentOptions.Center, Color.white);
        stageText = CreateText(questCard, "Stage_Text", Anchor.BottomLeft, new Vector2(14f, 9f), new Vector2(210f, 18f), 12f, TextAlignmentOptions.Left, new Color(0.75f, 0.82f, 0.88f, 1f));
        waterCountText = CreateText(questCard, "WaterCount_Text", Anchor.BottomRight, new Vector2(-14f, 9f), new Vector2(210f, 18f), 12f, TextAlignmentOptions.Right, new Color(0.75f, 0.82f, 0.88f, 1f));

        RectTransform inventory = CreatePanel(root, "HUD_Inventory", Anchor.TopLeft, new Vector2(0f, -94f), new Vector2(190f, 420f), 0.78f);
        ApplyPanelSprite(inventory, GetInventoryPanelSprite());
        inventoryText = CreateText(inventory, "Inventory_Text", Anchor.TopLeft, new Vector2(14f, -14f), new Vector2(160f, 58f), 13f, TextAlignmentOptions.TopLeft, new Color(0.84f, 1f, 0.82f, 1f));

        RectTransform slotRoot = CreateEmptyRect(inventory, "Inventory_Slots", Anchor.TopLeft, new Vector2(12f, -86f), new Vector2(166f, 190f));
        VerticalLayoutGroup layout = slotRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        for (int i = 0; i < 4; i++)
        {
            inventorySlots.Add(CreateInventorySlot(slotRoot, i));
        }

        RectTransform ultimate = CreatePanel(root, "HUD_Ultimate", Anchor.BottomRight, new Vector2(-24f, 24f), new Vector2(220f, 86f), 0.74f);
        modeText = CreateText(ultimate, "Mode_Text", Anchor.LeftCenter, new Vector2(12f, 18f), new Vector2(132f, 22f), 14f, TextAlignmentOptions.Left, new Color(0.86f, 0.94f, 1f, 1f));
        iceText = CreateText(ultimate, "Ice_Text", Anchor.LeftCenter, new Vector2(12f, -8f), new Vector2(132f, 22f), 13f, TextAlignmentOptions.Left, new Color(0.76f, 0.86f, 0.92f, 1f));

        Image ultimateBackground = CreateCircleImage(ultimate, "Ultimate_Background", Anchor.RightCenter, new Vector2(-40f, 0f), new Vector2(64f, 64f), new Color(0.02f, 0.024f, 0.03f, 0.95f));
        ultimateBackground.raycastTarget = false;
        ultimateFillImage = CreateCircleImage(ultimate, "Ultimate_Fill", Anchor.RightCenter, new Vector2(-40f, 0f), new Vector2(64f, 64f), new Color(0.12f, 0.45f, 0.7f, 1f));
        ultimateFillImage.type = Image.Type.Filled;
        ultimateFillImage.fillMethod = Image.FillMethod.Radial360;
        ultimateFillImage.fillOrigin = 2;
        ultimateFillImage.fillClockwise = true;
        ultimateText = CreateText(ultimate, "Ultimate_Text", Anchor.RightCenter, new Vector2(-40f, 0f), new Vector2(64f, 32f), 16f, TextAlignmentOptions.Center, Color.white);
    }

    private void TryBindExistingHud()
    {
        healthFillImages.Clear();
        waterFillImages.Clear();
        healthSliders.Clear();
        waterSliders.Clear();

        CollectImagesUnder("Health_Slider", "Fill", healthFillImages);
        CollectImagesUnder("HUD_HealthCard", "Fill", healthFillImages);
        CollectImagesUnder("HUD_Can", "Fill", healthFillImages);
        CollectSliders("Health_Slider", healthSliders);

        CollectImagesUnder("Water_Slider", "Fill", waterFillImages);
        CollectImagesUnder("HUD_WaterCard", "Fill", waterFillImages);
        CollectImagesUnder("HUD_Su", "Fill", waterFillImages);
        CollectSliders("Water_Slider", waterSliders);

        if (healthFillImages.Count > 0)
        {
            healthFillImage = healthFillImages[0];
        }

        if (healthSliders.Count > 0)
        {
            healthSlider = healthSliders[0];
        }

        if (waterFillImages.Count > 0)
        {
            waterFillImage = waterFillImages[0];
        }

        if (waterSliders.Count > 0)
        {
            waterSlider = waterSliders[0];
        }

        PrepareBarFillImage(healthFillImage, displayedHealthFill < 0f ? 1f : -1f);
        PrepareBarFillImage(waterFillImage, displayedWaterFill < 0f ? 0f : -1f);
        PrepareBarSlider(healthSlider, displayedHealthFill < 0f ? 1f : -1f);
        PrepareBarSlider(waterSlider, displayedWaterFill < 0f ? 0f : -1f);

        if (questText == null)
        {
            questText = FindText("Quest_Text");
        }

        if (stageText == null)
        {
            stageText = FindText("Stage_Text");
            if (stageText == null)
            {
                stageText = FindText("VillageStage_Text");
            }
        }

        if (waterCountText == null)
        {
            waterCountText = FindText("WaterCount_Text");
            if (waterCountText == null)
            {
                waterCountText = FindText("CollectedWater_Text");
            }
        }

        if (inventoryText == null)
        {
            inventoryText = FindText("Inventory_Text");
            if (inventoryText == null)
            {
                inventoryText = FindText("PlantInventory_Text");
            }
        }

        if (modeText == null)
        {
            modeText = FindText("Mode_Text");
            if (modeText == null)
            {
                modeText = FindText("ProjectileMode_Text");
            }
        }

        if (iceText == null)
        {
            iceText = FindText("Ice_Text");
            if (iceText == null)
            {
                iceText = FindText("IceShots_Text");
            }
        }

        if (ultimateText == null)
        {
            ultimateText = FindText("Ultimate_Text");
        }

        if (ultimateFillImage == null)
        {
            ultimateFillImage = FindImage("Ultimate_Fill");
        }

        if (inventorySlots.Count == 0)
        {
            BindInventorySlots();
        }
    }

    private void PrepareBarFillImage(Image fillImage, float initialFallback)
    {
        if (fillImage == null)
        {
            return;
        }

        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillClockwise = true;

        if (initialFallback >= 0f && Mathf.Approximately(fillImage.fillAmount, 1f) && initialFallback <= 0f)
        {
            fillImage.fillAmount = initialFallback;
        }
    }

    private void PrepareBarSlider(Slider slider, float initialFallback)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        if (initialFallback >= 0f && Mathf.Approximately(slider.normalizedValue, 1f) && initialFallback <= 0f)
        {
            slider.SetValueWithoutNotify(initialFallback);
        }
    }

    private TMP_Text FindText(string objectName)
    {
        Transform target = FindChild(objectName);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private Image FindImage(string objectName)
    {
        Transform target = FindChild(objectName);
        return target != null ? target.GetComponent<Image>() : null;
    }

    private Slider FindSlider(string objectName)
    {
        Transform target = FindChild(objectName);
        return target != null ? target.GetComponent<Slider>() : null;
    }

    private void CollectImagesUnder(string parentName, string imageName, List<Image> results)
    {
        foreach (Transform candidateParent in GetComponentsInChildren<Transform>(true))
        {
            if (candidateParent.name != parentName)
            {
                continue;
            }

            foreach (Image image in candidateParent.GetComponentsInChildren<Image>(true))
            {
                if (image.name == imageName && !results.Contains(image))
                {
                    results.Add(image);
                }
            }
        }
    }

    private void CollectSliders(string objectName, List<Slider> results)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name != objectName)
            {
                continue;
            }

            Slider slider = child.GetComponent<Slider>();
            if (slider != null && !results.Contains(slider))
            {
                results.Add(slider);
            }
        }
    }

    private Image FindImageUnder(string parentName, string imageName)
    {
        Transform parent = FindChild(parentName);
        if (parent == null)
        {
            return null;
        }

        foreach (Image image in parent.GetComponentsInChildren<Image>(true))
        {
            if (image.name == imageName)
            {
                return image;
            }
        }

        return null;
    }

    private Transform FindChild(string objectName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
            {
                return child;
            }
        }

        return null;
    }

    private void BindInventorySlots()
    {
        Transform inventory = FindChild("HUD_Inventory");
        if (inventory == null)
        {
            inventory = transform;
        }

        List<Button> buttons = new List<Button>();
        foreach (Button button in inventory.GetComponentsInChildren<Button>(true))
        {
            buttons.Add(button);
        }

        buttons.Sort((left, right) => left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex()));
        inventorySlots.AddRange(buttons);
    }

    private void ClearOldHudChildren()
    {
        List<Transform> toDestroy = new List<Transform>();
        foreach (Transform child in transform)
        {
            WorldController childController = child.GetComponent<WorldController>();
            if (childController != null && childController.role == WorldRole.Fade)
            {
                continue;
            }

            toDestroy.Add(child);
        }

        foreach (Transform child in toDestroy)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    private Image CreateBar(RectTransform parent, string labelText, Anchor anchor, Vector2 position, Color fillColor, float initialFillAmount)
    {
        RectTransform card = CreatePanel(parent, $"HUD_{labelText}", anchor, position, new Vector2(300f, 58f), 0.78f);
        TextAlignmentOptions alignment = anchor == Anchor.TopLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
        Anchor labelAnchor = anchor == Anchor.TopLeft ? Anchor.TopLeft : Anchor.TopRight;
        Vector2 labelPosition = anchor == Anchor.TopLeft ? new Vector2(16f, -8f) : new Vector2(-16f, -8f);
        CreateText(card, $"{labelText}_Label", labelAnchor, labelPosition, new Vector2(120f, 18f), 17f, alignment, Color.white).text = labelText;

        RectTransform barRoot = CreateEmptyRect(card, $"{labelText}_Bar", Anchor.BottomLeft, new Vector2(18f, 12f), new Vector2(264f, 14f));
        Image background = CreateImage(barRoot, "Background", Anchor.Stretch, Vector2.zero, Vector2.zero, new Color(0.02f, 0.024f, 0.03f, 0.95f));
        background.raycastTarget = false;

        Image fill = CreateImage(barRoot, "Fill", Anchor.Stretch, new Vector2(2f, 2f), new Vector2(-2f, -2f), fillColor);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = Mathf.Clamp01(initialFillAmount);
        fill.raycastTarget = false;
        return fill;
    }

    private RectTransform CreatePanel(RectTransform parent, string name, Anchor anchor, Vector2 position, Vector2 size, float alpha)
    {
        GameObject target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        target.transform.SetParent(parent, false);
        RectTransform rect = target.GetComponent<RectTransform>();
        ApplyAnchor(rect, anchor, position, size);

        Image image = target.GetComponent<Image>();
        image.color = new Color(0.015f, 0.018f, 0.024f, alpha);
        image.raycastTarget = false;
        return rect;
    }

    private Button CreateInventorySlot(RectTransform parent, int index)
    {
        GameObject target = new GameObject($"PlantSlot_{index + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        target.transform.SetParent(parent, false);

        RectTransform rect = target.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 38f);

        LayoutElement layout = target.GetComponent<LayoutElement>();
        layout.minHeight = 38f;
        layout.preferredHeight = 38f;

        Image image = target.GetComponent<Image>();
        image.color = new Color(0.07f, 0.14f, 0.12f, 0.94f);

        Button button = target.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;

        TMP_Text label = CreateText(rect, "Label", Anchor.Stretch, new Vector2(10f, 0f), new Vector2(-20f, 0f), 13.5f, TextAlignmentOptions.MidlineLeft, new Color(0.84f, 0.96f, 0.86f, 1f));
        label.raycastTarget = false;
        target.SetActive(false);
        return button;
    }

    private TMP_Text CreateText(RectTransform parent, string name, Anchor anchor, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        target.transform.SetParent(parent, false);

        RectTransform rect = target.GetComponent<RectTransform>();
        ApplyAnchor(rect, anchor, position, size);

        TMP_Text text = target.GetComponent<TMP_Text>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private Image CreateImage(RectTransform parent, string name, Anchor anchor, Vector2 position, Vector2 size, Color color)
    {
        GameObject target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        target.transform.SetParent(parent, false);
        RectTransform rect = target.GetComponent<RectTransform>();
        ApplyAnchor(rect, anchor, position, size);

        Image image = target.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private Image CreateCircleImage(RectTransform parent, string name, Anchor anchor, Vector2 position, Vector2 size, Color color)
    {
        Image image = CreateImage(parent, name, anchor, position, size, color);
        image.sprite = GetCircleSprite();
        return image;
    }

    private void ApplyPanelSprite(RectTransform panel, Sprite sprite)
    {
        if (panel == null || sprite == null)
        {
            return;
        }

        Image image = panel.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.color = Color.white;
    }

    private RectTransform CreateEmptyRect(RectTransform parent, string name, Anchor anchor, Vector2 position, Vector2 size)
    {
        GameObject target = new GameObject(name, typeof(RectTransform));
        target.transform.SetParent(parent, false);
        RectTransform rect = target.GetComponent<RectTransform>();
        ApplyAnchor(rect, anchor, position, size);
        return rect;
    }

    private void RefreshInventorySlots(GameManager gameManager)
    {
        List<PlantInventoryEntry> entries = GetVisiblePlantEntries(gameManager);
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            Button slot = inventorySlots[i];
            bool hasEntry = i < entries.Count;
            slot.gameObject.SetActive(hasEntry);
            if (!hasEntry)
            {
                continue;
            }

            PlantInventoryEntry entry = entries[i];
            string displayName = string.IsNullOrWhiteSpace(entry.displayName) ? entry.plantId : entry.displayName;
            bool selected = entry.plantId == gameManager.SelectedPlantId;
            SetButtonLabel(slot, $"{(selected ? "> " : string.Empty)}{displayName} x{entry.count}");
            StyleInventorySlot(slot, selected);

            string idToSelect = entry.plantId;
            slot.onClick.RemoveAllListeners();
            slot.onClick.AddListener(() => GameManager.Instance?.SelectPlantForPlanting(idToSelect));
            slot.interactable = true;
        }
    }

    private List<PlantInventoryEntry> GetVisiblePlantEntries(GameManager gameManager)
    {
        List<PlantInventoryEntry> entries = new List<PlantInventoryEntry>();
        foreach (PlantInventoryEntry entry in gameManager.PlantInventory)
        {
            if (entry != null && entry.count > 0)
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    private void SetButtonLabel(Button button, string label)
    {
        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = label;
            return;
        }

        Text legacyText = button.GetComponentInChildren<Text>(true);
        if (legacyText != null)
        {
            legacyText.text = label;
        }
    }

    private void StyleInventorySlot(Button button, bool selected)
    {
        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = selected
                ? new Color(0.18f, 0.42f, 0.28f, 0.98f)
                : new Color(0.07f, 0.14f, 0.12f, 0.94f);
        }

        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.color = selected ? new Color(0.96f, 1f, 0.94f, 1f) : new Color(0.84f, 0.96f, 0.86f, 1f);
        }
    }

    private string GetInventoryHudText(GameManager gameManager)
    {
        if (!gameManager.HasSelectedPlant)
        {
            return "ENVANTER";
        }

        return $"ENVANTER\nSecili: {gameManager.GetSelectedPlantDisplayName()}";
    }

    private enum Anchor
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        BottomRight,
        LeftCenter,
        RightCenter,
        BottomLeft,
        BottomRightLocal,
        TopStretch,
        BottomStretch,
        Stretch
    }

    private void ApplyAnchor(RectTransform rect, Anchor anchor, Vector2 position, Vector2 size)
    {
        switch (anchor)
        {
            case Anchor.TopLeft:
                SetAnchor(rect, Vector2.up, Vector2.up, Vector2.up, position, size);
                break;
            case Anchor.TopCenter:
                SetAnchor(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, size);
                break;
            case Anchor.TopRight:
                SetAnchor(rect, Vector2.one, Vector2.one, Vector2.one, position, size);
                break;
            case Anchor.MiddleLeft:
                SetAnchor(rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), position, size);
                break;
            case Anchor.BottomRight:
                SetAnchor(rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), position, size);
                break;
            case Anchor.LeftCenter:
                SetAnchor(rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), position, size);
                break;
            case Anchor.RightCenter:
                SetAnchor(rect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
                break;
            case Anchor.BottomLeft:
                SetAnchor(rect, Vector2.zero, Vector2.zero, Vector2.zero, position, size);
                break;
            case Anchor.BottomRightLocal:
                SetAnchor(rect, Vector2.right, Vector2.right, Vector2.right, position, size);
                break;
            case Anchor.TopStretch:
                SetStretch(rect, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f), position, size);
                break;
            case Anchor.BottomStretch:
                SetStretch(rect, Vector2.zero, Vector2.right, Vector2.zero, position, size);
                break;
            case Anchor.Stretch:
                SetStretch(rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), position, size);
                break;
        }
    }

    private void SetAnchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void SetStretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
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
                    DestroyEnemyIfOutsideAfterSeen(enemy);
                }
            }
        }
    }

    private void DestroyEnemyIfOutsideAfterSeen(EnemyController enemy)
    {
        Vector3 viewport = targetCamera.WorldToViewportPoint(enemy.transform.position);
        bool outside = viewport.z < 0f || viewport.x < -viewportPadding || viewport.x > 1f + viewportPadding || viewport.y < -viewportPadding || viewport.y > 1f + viewportPadding;

        if (!outside)
        {
            enemy.MarkVisibleByCamera();
            return;
        }

        if (enemy.HasBeenVisibleByCamera)
        {
            Destroy(enemy.gameObject);
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

        if (transform.IsChildOf(followTarget))
        {
            return;
        }

        if (!hasCameraFollowBaseY)
        {
            cameraFollowInitialOffset = transform.position - followTarget.position;
            followOffset = cameraFollowInitialOffset;
            cameraFollowBaseY = followTarget.position.y + cameraFollowInitialOffset.y;
            hasCameraFollowBaseY = true;
        }

        Vector3 rotatedOffset = rotateFollowOffsetWithTarget
            ? Quaternion.Euler(0f, followTarget.eulerAngles.y, 0f) * cameraFollowInitialOffset
            : cameraFollowInitialOffset;

        Vector3 targetPosition = new Vector3(
            followTarget.position.x + rotatedOffset.x,
            followTarget.position.y + rotatedOffset.y,
            followTarget.position.z + rotatedOffset.z);

        transform.position = followSmoothTime <= 0f
            ? targetPosition
            : Vector3.SmoothDamp(transform.position, targetPosition, ref followVelocity, followSmoothTime);

        if (rotateCameraWithTarget)
        {
            transform.rotation = Quaternion.Euler(cameraFollowStartEuler.x, followTarget.eulerAngles.y, cameraFollowStartEuler.z);
        }
        else
        {
            transform.rotation = cameraFollowStartRotation;
        }
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

    private void SetupFade()
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

    private void EnsureScreenSpaceHud()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
        canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 50);

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
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

    private static Sprite GetInventoryPanelSprite()
    {
        if (inventoryPanelSprite != null)
        {
            return inventoryPanelSprite;
        }

        Texture2D texture = Resources.Load<Texture2D>("UI/inventory_panel");
        if (texture == null)
        {
            return null;
        }

        inventoryPanelSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(80f, 80f, 80f, 80f));
        return inventoryPanelSprite;
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

    private void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}
