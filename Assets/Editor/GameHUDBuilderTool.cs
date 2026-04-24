#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class GameHUDBuilderTool
{
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

        GameObject panel = CreateUIObject("HUD_Panel", canvasObject.transform, typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -24f);
        panelRect.sizeDelta = new Vector2(420f, 270f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.45f);

        TMP_Text healthLabel = CreateText("Health_Label", panel.transform, "Can", 24, TextAlignmentOptions.Left);
        SetRect(healthLabel.rectTransform, new Vector2(24f, -22f), new Vector2(100f, 30f));

        Slider healthSlider = CreateSlider("Health_Slider", panel.transform, new Color(0.85f, 0.16f, 0.16f));
        SetRect(healthSlider.GetComponent<RectTransform>(), new Vector2(24f, -58f), new Vector2(360f, 28f));

        TMP_Text waterLabel = CreateText("Water_Label", panel.transform, "Su", 24, TextAlignmentOptions.Left);
        SetRect(waterLabel.rectTransform, new Vector2(24f, -96f), new Vector2(100f, 30f));

        Slider waterSlider = CreateSlider("Water_Slider", panel.transform, new Color(0.1f, 0.55f, 0.95f));
        SetRect(waterSlider.GetComponent<RectTransform>(), new Vector2(24f, -132f), new Vector2(360f, 28f));

        TMP_Text questText = CreateText("Quest_Text", panel.transform, "Magaraya git ve su topla.", 22, TextAlignmentOptions.Left);
        SetRect(questText.rectTransform, new Vector2(24f, -170f), new Vector2(360f, 34f));

        TMP_Text villageStageText = CreateText("VillageStage_Text", panel.transform, "Koy Asamasi: 0", 18, TextAlignmentOptions.Left);
        SetRect(villageStageText.rectTransform, new Vector2(24f, -204f), new Vector2(360f, 24f));

        TMP_Text collectedWaterText = CreateText("CollectedWater_Text", panel.transform, "Toplanan Su: 0/100", 18, TextAlignmentOptions.Left);
        SetRect(collectedWaterText.rectTransform, new Vector2(24f, -228f), new Vector2(360f, 24f));

        TMP_Text activeTaskText = CreateText("ActiveTask_Text", panel.transform, "Aktif Gorev: Magaraya git ve su topla.", 18, TextAlignmentOptions.Left);
        SetRect(activeTaskText.rectTransform, new Vector2(24f, -252f), new Vector2(360f, 24f));

        EnsureEventSystem();
        Selection.activeGameObject = canvasObject;
        EditorUtility.DisplayDialog("Game HUD", "HUD basariyla olusturuldu.", "Tamam");
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
