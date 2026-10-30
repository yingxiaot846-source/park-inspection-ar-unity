using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 程序化搭建提交场景并保存：Assets/Scenes/Submit.unity。
/// 菜单：Tools/Build Submit Scene；自动化：
/// Unity.exe -batchmode -quit -executeMethod S4SceneBuilder.Build -logFile -
/// 成功时输出约定标记：SCENE_SAVED / S4_BUILD_OK。
/// </summary>
public static class S4SceneBuilder
{
    private const string SceneDir = "Assets/Scenes";
    private const string ScenePath = SceneDir + "/Submit.unity";

    // Windows 系统字体回退链：中文必须用含字形的字体，内置 LegacyRuntime.ttf 不含中文。
    private static readonly string[] OsFontNames = { "Microsoft YaHei", "SimHei", "Arial" };

    [MenuItem("Tools/Build Submit Scene")]
    public static void Build()
    {
        Font font = GetUiFont();

        if (!Directory.Exists(SceneDir))
        {
            Directory.CreateDirectory(SceneDir);
        }

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 事件系统：没有它 uGUI 控件无法交互。
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // 根面板：承载 SubmitPanel 组件。
        GameObject panelRoot = new GameObject("SubmitPanel");
        SubmitPanel panel = panelRoot.AddComponent<SubmitPanel>();

        // 画布。
        GameObject canvasGo = new GameObject("Canvas");
        canvasGo.transform.SetParent(panelRoot.transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // 标题。
        Text titleLabel = CreateText(canvasGo.transform, "TitleLabel", "标题*", font, 24, TextAnchor.MiddleLeft);
        Place(titleLabel.rectTransform, new Vector2(100f, 640f), new Vector2(160f, 40f));
        InputField titleInput = CreateInputField(canvasGo.transform, "TitleInput", font, "请输入问题标题");
        Place(titleInput.GetComponent<RectTransform>(), new Vector2(420f, 640f), new Vector2(560f, 40f));

        // 描述。
        Text descLabel = CreateText(canvasGo.transform, "DescLabel", "描述", font, 24, TextAnchor.MiddleLeft);
        Place(descLabel.rectTransform, new Vector2(100f, 580f), new Vector2(160f, 40f));
        InputField descInput = CreateInputField(canvasGo.transform, "DescInput", font, "请描述问题（可留空）");
        Place(descInput.GetComponent<RectTransform>(), new Vector2(420f, 580f), new Vector2(560f, 40f));

        // 优先级。
        Text priorityLabel = CreateText(canvasGo.transform, "PriorityLabel", "优先级", font, 24, TextAnchor.MiddleLeft);
        Place(priorityLabel.rectTransform, new Vector2(100f, 520f), new Vector2(160f, 40f));
        Dropdown priorityDropdown = CreateDropdown(canvasGo.transform, "PriorityDropdown", font);
        priorityDropdown.AddOptions(new List<string> { "低", "中", "高" });
        priorityDropdown.value = 1;
        Place(priorityDropdown.GetComponent<RectTransform>(), new Vector2(420f, 520f), new Vector2(240f, 40f));

        // 假坐标标注。
        Text fakeNote = CreateText(canvasGo.transform, "FakePosNote", "本阶段为假坐标 (1.0, 0.0, 1.0)", font, 18, TextAnchor.MiddleLeft);
        fakeNote.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        Place(fakeNote.rectTransform, new Vector2(420f, 478f), new Vector2(420f, 28f));

        // 提交按钮。
        Button submitButton = CreateButton(canvasGo.transform, "SubmitButton", "提交", font);
        Place(submitButton.GetComponent<RectTransform>(), new Vector2(420f, 410f), new Vector2(180f, 52f));

        // 结果提示。
        Text resultText = CreateText(canvasGo.transform, "ResultText", string.Empty, font, 24, TextAnchor.MiddleLeft);
        Place(resultText.rectTransform, new Vector2(100f, 340f), new Vector2(880f, 40f));

        // 字段绑定。
        panel.titleInput = titleInput;
        panel.descInput = descInput;
        panel.priorityDropdown = priorityDropdown;
        panel.submitButton = submitButton;
        panel.resultText = resultText;
        submitButton.onClick.AddListener(panel.OnClickSubmit);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        Debug.Log("SCENE_SAVED: " + ScenePath);

        // 加入 Build Settings，保证真机阶段可直接构建。
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        Debug.Log("S4_BUILD_OK");
    }

    private static Font GetUiFont()
    {
        Font osFont = Font.CreateDynamicFontFromOSFont(OsFontNames, 20);
        if (osFont != null)
        {
            return osFont;
        }
        // 兜底：内置字体（不含中文，仅保证不为空引用）。
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static void Place(RectTransform rect, Vector2 center, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = center - new Vector2(640f, 360f);
        rect.sizeDelta = size;
    }

    private static Text CreateText(Transform parent, string name, string content, Font font, int size, TextAnchor anchor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.text = content;
        text.color = Color.white;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static InputField CreateInputField(Transform parent, string name, Font font, string placeholder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image background = go.AddComponent<Image>();
        background.color = new Color(0.15f, 0.15f, 0.18f, 1f);

        InputField input = go.AddComponent<InputField>();

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        Text text = textGo.AddComponent<Text>();
        text.font = font;
        text.fontSize = 22;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.supportRichText = false;
        Stretch(text.rectTransform, 10f);

        GameObject placeholderGo = new GameObject("Placeholder");
        placeholderGo.transform.SetParent(go.transform, false);
        Text placeholderText = placeholderGo.AddComponent<Text>();
        placeholderText.font = font;
        placeholderText.fontSize = 22;
        placeholderText.fontStyle = FontStyle.Italic;
        placeholderText.color = new Color(1f, 1f, 1f, 0.35f);
        placeholderText.alignment = TextAnchor.MiddleLeft;
        placeholderText.text = placeholder;
        Stretch(placeholderText.rectTransform, 10f);

        input.textComponent = text;
        input.placeholder = placeholderText;
        return input;
    }

    private static Button CreateButton(Transform parent, string name, string label, Font font)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image background = go.AddComponent<Image>();
        background.color = new Color(0.2f, 0.45f, 0.8f, 1f);
        Button button = go.AddComponent<Button>();

        Text labelText = CreateText(go.transform, "Text", label, font, 24, TextAnchor.MiddleCenter);
        labelText.color = Color.white;
        Stretch(labelText.rectTransform, 0f);

        return button;
    }

    // 复刻 Unity 默认 Dropdown 预制体的最小可用结构（Template 必须为 inactive）。
    private static Dropdown CreateDropdown(Transform parent, string name, Font font)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image background = go.AddComponent<Image>();
        background.color = new Color(0.15f, 0.15f, 0.18f, 1f);
        Dropdown dropdown = go.AddComponent<Dropdown>();

        Text caption = CreateText(go.transform, "Label", string.Empty, font, 22, TextAnchor.MiddleLeft);
        caption.color = Color.white;
        Stretch(caption.rectTransform, 10f);

        // Template（inactive）：ScrollRect + Viewport(Mask) + Content + Item(Toggle)。
        GameObject template = new GameObject("Template");
        template.transform.SetParent(go.transform, false);
        Image templateImage = template.AddComponent<Image>();
        templateImage.color = new Color(0.12f, 0.12f, 0.15f, 1f);
        ScrollRect scroll = template.AddComponent<ScrollRect>();
        RectTransform templateRect = template.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.anchoredPosition = new Vector2(0f, -2f);
        templateRect.sizeDelta = new Vector2(0f, 120f);

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(template.transform, false);
        viewport.AddComponent<Image>().color = Color.white;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 0f);

        GameObject item = new GameObject("Item");
        item.transform.SetParent(content.transform, false);
        Image itemBackground = item.AddComponent<Image>();
        itemBackground.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        Toggle itemToggle = item.AddComponent<Toggle>();
        RectTransform itemRect = item.GetComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0f, 0.5f);
        itemRect.anchorMax = new Vector2(1f, 0.5f);
        itemRect.pivot = new Vector2(0.5f, 0.5f);
        itemRect.sizeDelta = new Vector2(0f, 28f);

        GameObject checkmarkGo = new GameObject("Item Checkmark");
        checkmarkGo.transform.SetParent(item.transform, false);
        Image checkmark = checkmarkGo.AddComponent<Image>();
        checkmark.color = new Color(0.3f, 0.65f, 1f, 1f);
        RectTransform checkRect = checkmarkGo.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0f, 0.25f);
        checkRect.anchorMax = new Vector2(0f, 0.75f);
        checkRect.pivot = new Vector2(0f, 0.5f);
        checkRect.anchoredPosition = new Vector2(6f, 0f);
        checkRect.sizeDelta = new Vector2(14f, 0f);

        Text itemLabel = CreateText(item.transform, "Item Label", string.Empty, font, 20, TextAnchor.MiddleLeft);
        itemLabel.color = Color.white;
        RectTransform labelRect = itemLabel.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(28f, 2f);
        labelRect.offsetMax = new Vector2(-8f, -2f);

        itemToggle.targetGraphic = itemBackground;
        itemToggle.graphic = checkmark;

        scroll.content = contentRect;
        scroll.viewport = viewportRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 18f;

        dropdown.template = templateRect;
        dropdown.captionText = caption;
        dropdown.itemText = itemLabel;

        template.SetActive(false);
        return dropdown;
    }

    private static void Stretch(RectTransform rect, float padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }
}
