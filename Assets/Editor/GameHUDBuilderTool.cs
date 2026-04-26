#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class GameHUDBuilderTool
{
    private const string GameHudPrefabPath = "Assets/Prefabs/GameHUD_Canvas.prefab";

    [InitializeOnLoadMethod]
    private static void AutoMaterializeGameHUDPrefabHierarchy()
    {
        if (SessionState.GetBool("GameHUDHierarchyMaterialized", false))
        {
            return;
        }

        SessionState.SetBool("GameHUDHierarchyMaterialized", true);
        EditorApplication.delayCall += MaterializeGameHUDPrefabHierarchy;
    }

    [MenuItem("Tools/GameJam/Materialize Game HUD Prefab Hierarchy")]
    public static void MaterializeGameHUDPrefabHierarchy()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(GameHudPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"GameHUD prefab bulunamadi: {GameHudPrefabPath}");
            return;
        }

        try
        {
            MaterializeHudHierarchy(prefabRoot.transform);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, GameHudPrefabPath);
            Debug.Log("GameHUD prefab hiyerarsisi guncellendi.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    [MenuItem("Tools/GameJam/Build Game HUD")]
    public static void BuildGameHUD()
    {
        if (GameObject.Find("GameHUD_Canvas") != null)
        {
            EditorUtility.DisplayDialog("Game HUD", "GameHUD_Canvas sahnede zaten var. Yeni HUD olusturulmadi.", "Tamam");
            return;
        }

        GameObject canvasObject = new GameObject("GameHUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(WorldController));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        MaterializeHudHierarchy(canvasObject.transform);

        EnsureEventSystem();
        Selection.activeGameObject = canvasObject;
        EditorUtility.DisplayDialog("Game HUD", "HUD basariyla olusturuldu.", "Tamam");
    }

    private static void MaterializeHudHierarchy(Transform root)
    {
        GameObject panel = EnsureUIObject("HUD_Panel", root, typeof(Image));
        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = Color.clear;
        SetRect(panel.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));

        GameObject healthCard = EnsureCard("HUD_HealthCard", root, new Color(0.015f, 0.018f, 0.024f, 0.7f));
        GameObject questCard = EnsureCard("HUD_QuestCard", root, new Color(0.015f, 0.018f, 0.024f, 0.7f));
        GameObject waterCard = EnsureCard("HUD_WaterCard", root, new Color(0.015f, 0.018f, 0.024f, 0.7f));
        GameObject statusCard = EnsureCard("HUD_StatusCard", root, Color.clear);
        GameObject plantCard = EnsureCard("HUD_PlantCard", root, new Color(0.015f, 0.018f, 0.024f, 0.62f));
        GameObject ultimateGroup = EnsureCard("UltimateHUD_Group", root, new Color(0.015f, 0.018f, 0.024f, 0.72f));

        SetRect(healthCard.GetComponent<RectTransform>(), new Vector2(16f, -14f), new Vector2(350f, 64f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        SetRect(questCard.GetComponent<RectTransform>(), new Vector2(0f, -14f), new Vector2(460f, 64f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        SetRect(waterCard.GetComponent<RectTransform>(), new Vector2(-16f, -14f), new Vector2(350f, 64f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        SetRect(statusCard.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        SetRect(plantCard.GetComponent<RectTransform>(), Vector2.zero, new Vector2(220f, 420f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        SetRect(ultimateGroup.GetComponent<RectTransform>(), new Vector2(-28f, 28f), new Vector2(230f, 86f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));

        Transform healthLabel = EnsureTextObject("Health_Label", healthCard.transform, "Can", 18, TextAlignmentOptions.Left).transform;
        Transform healthSlider = EnsureSliderObject("Health_Slider", healthCard.transform, new Color(0.9f, 0.18f, 0.16f), 100f).transform;
        Transform questText = EnsureTextObject("Quest_Text", questCard.transform, "Magaraya git ve su topla.", 22, TextAlignmentOptions.Center).transform;
        Transform activeTaskText = EnsureTextObject("ActiveTask_Text", questCard.transform, "Aktif Gorev: Magaraya git ve su topla.", 15, TextAlignmentOptions.Center).transform;
        Transform waterLabel = EnsureTextObject("Water_Label", waterCard.transform, "Su", 18, TextAlignmentOptions.Right).transform;
        Transform waterSlider = EnsureSliderObject("Water_Slider", waterCard.transform, new Color(0.1f, 0.55f, 0.95f), 0f).transform;
        Transform stageText = EnsureTextObject("VillageStage_Text", healthCard.transform, "Koy Asamasi: 0", 13, TextAlignmentOptions.Left).transform;
        Transform collectedWaterText = EnsureTextObject("CollectedWater_Text", waterCard.transform, "Toplanan Su: 0/100", 13, TextAlignmentOptions.Right).transform;
        TMP_Text plantInventory = EnsureTextObject("PlantInventory_Text", plantCard.transform, "Bitkiler: -", 14, TextAlignmentOptions.TopLeft);
        plantInventory.enableWordWrapping = true;
        plantInventory.overflowMode = TextOverflowModes.Ellipsis;
        Transform plantInventoryText = plantInventory.transform;

        SetRect(healthLabel.GetComponent<RectTransform>(), new Vector2(18f, -8f), new Vector2(140f, 20f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        SetRect(healthSlider.GetComponent<RectTransform>(), new Vector2(18f, -34f), new Vector2(300f, 16f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        SetRect(questText.GetComponent<RectTransform>(), new Vector2(0f, -9f), new Vector2(420f, 24f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        SetRect(activeTaskText.GetComponent<RectTransform>(), new Vector2(0f, -36f), new Vector2(420f, 18f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        SetRect(waterLabel.GetComponent<RectTransform>(), new Vector2(-18f, -8f), new Vector2(140f, 20f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        SetRect(waterSlider.GetComponent<RectTransform>(), new Vector2(-18f, -34f), new Vector2(300f, 16f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        SetRect(stageText.GetComponent<RectTransform>(), new Vector2(18f, -76f), new Vector2(260f, 18f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        SetRect(collectedWaterText.GetComponent<RectTransform>(), new Vector2(-18f, -76f), new Vector2(280f, 18f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        SetRect(plantInventoryText.GetComponent<RectTransform>(), new Vector2(14f, -16f), new Vector2(190f, 380f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));

        Image ultimateBackground = EnsureCircleImage("Ultimate_Background", ultimateGroup.transform, new Color(0.02f, 0.024f, 0.03f, 0.95f), false);
        Image ultimateFill = EnsureCircleImage("Ultimate_Fill", ultimateGroup.transform, new Color(0.45f, 0.95f, 1f, 1f), true);
        TMP_Text projectileModeText = EnsureTextObject("ProjectileMode_Text", ultimateGroup.transform, "Mod: Su", 16, TextAlignmentOptions.Left);
        TMP_Text iceShotsText = EnsureTextObject("IceShots_Text", ultimateGroup.transform, "Buz Atis: -", 14, TextAlignmentOptions.Left);
        TMP_Text ultimateText = EnsureTextObject("Ultimate_Text", ultimateGroup.transform, "V", 16, TextAlignmentOptions.Center);

        SetRect(ultimateBackground.rectTransform, new Vector2(-38f, 38f), new Vector2(64f, 64f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f));
        SetRect(ultimateFill.rectTransform, new Vector2(-38f, 38f), new Vector2(64f, 64f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f));
        SetRect(projectileModeText.rectTransform, new Vector2(0f, 54f), new Vector2(140f, 22f), Vector2.zero, Vector2.zero, Vector2.zero);
        SetRect(iceShotsText.rectTransform, new Vector2(0f, 30f), new Vector2(140f, 22f), Vector2.zero, Vector2.zero, Vector2.zero);
        SetRect(ultimateText.rectTransform, new Vector2(-38f, 38f), new Vector2(64f, 40f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f));

        ultimateBackground.transform.SetAsFirstSibling();
        ultimateFill.transform.SetSiblingIndex(1);
        ultimateText.transform.SetAsLastSibling();

        EnsureFadeObjects(root);
    }

    private static void EnsureFadeObjects(Transform root)
    {
        GameObject fadeImage = EnsureUIObject("Fade_Image", root, typeof(CanvasRenderer), typeof(Image));
        RectTransform fadeRect = fadeImage.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;

        Image image = fadeImage.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = false;

        EnsureUIObject("FadeController", root, typeof(WorldController));
    }

    private static GameObject EnsureCard(string name, Transform parent, Color color)
    {
        GameObject card = EnsureUIObject(name, parent, typeof(CanvasRenderer), typeof(Image));
        Image image = card.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return card;
    }

    private static GameObject EnsureUIObject(string name, Transform parent, params System.Type[] components)
    {
        Transform existing = FindDeepChild(parent, name);
        GameObject obj = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        foreach (System.Type component in components)
        {
            if (obj.GetComponent(component) == null)
            {
                obj.AddComponent(component);
            }
        }

        return obj;
    }

    private static TMP_Text EnsureTextObject(string name, Transform parent, string value, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject obj = EnsureUIObject(name, parent, typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        TMP_Text text = obj.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static Slider EnsureSliderObject(string name, Transform parent, Color fillColor, float initialValue)
    {
        GameObject existing = EnsureUIObject(name, parent, typeof(Slider));
        Slider slider = existing.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = Mathf.Clamp(initialValue, slider.minValue, slider.maxValue);
        slider.transition = Selectable.Transition.None;

        GameObject background = EnsureUIObject("Background", existing.transform, typeof(CanvasRenderer), typeof(Image));
        background.GetComponent<Image>().color = new Color(0.02f, 0.024f, 0.03f, 0.95f);
        Stretch(background.GetComponent<RectTransform>());

        GameObject fillArea = EnsureUIObject("Fill Area", existing.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(3f, 3f);
        fillAreaRect.offsetMax = new Vector2(-3f, -3f);

        GameObject fill = EnsureUIObject("Fill", fillArea.transform, typeof(CanvasRenderer), typeof(Image));
        Image fillImage = fill.GetComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = slider.normalizedValue;
        Stretch(fill.GetComponent<RectTransform>());

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = background.GetComponent<Image>();
        return slider;
    }

    private static Image EnsureCircleImage(string name, Transform parent, Color color, bool filled)
    {
        GameObject obj = EnsureUIObject(name, parent, typeof(CanvasRenderer), typeof(Image));
        Image image = obj.GetComponent<Image>();
        image.sprite = GetEditorCircleSprite();
        image.color = color;
        image.raycastTarget = false;
        image.type = filled ? Image.Type.Filled : Image.Type.Simple;
        image.fillMethod = Image.FillMethod.Radial360;
        image.fillOrigin = 2;
        image.fillClockwise = true;
        image.fillAmount = 1f;
        return image;
    }

    private static Sprite GetEditorCircleSprite()
    {
        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);
        Vector2 center = new Vector2(31.5f, 31.5f);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                texture.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), center) <= 30f ? Color.white : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform found = FindDeepChild(child, childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static GameObject CreateUIObject(string name, Transform parent, params System.Type[] components)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        foreach (System.Type component in components)
        {
            obj.AddComponent(component);
        }

        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject obj = CreateUIObject(name, parent, typeof(TextMeshProUGUI));
        TMP_Text text = obj.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.enableWordWrapping = false;
        return text;
    }

    private static Slider CreateSlider(string name, Transform parent, Color fillColor)
    {
        GameObject sliderObject = CreateUIObject(name, parent, typeof(Slider));
        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 100f;
        slider.transition = Selectable.Transition.None;

        GameObject background = CreateUIObject("Background", sliderObject.transform, typeof(Image));
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
        Stretch(background.GetComponent<RectTransform>());

        GameObject fillArea = CreateUIObject("Fill Area", sliderObject.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(3f, 3f);
        fillAreaRect.offsetMax = new Vector2(-3f, -3f);

        GameObject fill = CreateUIObject("Fill", fillArea.transform, typeof(Image));
        Image fillImage = fill.GetComponent<Image>();
        fillImage.color = fillColor;
        Stretch(fill.GetComponent<RectTransform>());

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = fillImage;
        return slider;
    }

    private static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
#endif
