using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MvpArSubmitter
{
    /// <summary>
    /// 运行时引导：进入 Play 模式后自动搭建整个提交界面。
    /// 零场景资产、零 Editor 脚本——任意空场景（默认 SampleScene）即可运行。
    /// </summary>
    public static class SubmitUIBootstrap
    {
        // Windows 系统字体回退链：中文必须用含字形的字体，内置 LegacyRuntime.ttf 不含中文。
        private static readonly string[] OsFontNames = { "Microsoft YaHei", "SimHei", "Arial" };

        /// <summary>放置引导提示（原"假坐标"标注位置），由 SubmitPanel 经 SetHint 更新。</summary>
        private static Text hintText;

        /// <summary>统一更新放置引导提示文本。</summary>
        public static void SetHint(string message)
        {
            if (hintText != null)
            {
                hintText.text = message;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Build()
        {
            // 防重复：场景里已有 SubmitPanel 就不再创建。
            if (Object.FindObjectOfType<SubmitPanel>() != null)
            {
                return;
            }

            Font font = GetUiFont();

            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }

            // 根面板：承载 SubmitPanel 组件。
            GameObject panelRoot = new GameObject("SubmitPanel", typeof(RectTransform));
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
            // 优先级：三个互斥按钮，替代 Dropdown（手写 Dropdown 结构过于脆弱）。
            string[] priorityLabels = { "低", "中", "高" };
            Button[] priorityButtons = new Button[priorityLabels.Length];
            for (int i = 0; i < priorityLabels.Length; i++)
            {
                int index = i; // 闭包捕获必须复制到局部变量
                Button button = CreateButton(canvasGo.transform, "PriorityButton" + i, priorityLabels[i], font);
                Place(button.GetComponent<RectTransform>(), new Vector2(420f + i * 110f, 520f), new Vector2(100f, 40f));
                button.onClick.AddListener(() => panel.SetPriority(index));
                priorityButtons[i] = button;
            }
            panel.priorityButtons = priorityButtons;
            panel.SetPriorityInitial(1);

            // 放置引导提示（原"本阶段为假坐标"标注位置）。
            hintText = CreateText(canvasGo.transform, "PlacementHint", "请点击地面放置标记", font, 18, TextAnchor.MiddleLeft);
            hintText.color = new Color(0.6f, 0.9f, 0.6f, 1f);
            Place(hintText.rectTransform, new Vector2(420f, 478f), new Vector2(560f, 28f));

            // Bonus 2：本地规则智能判级——建议等级 + 原因（只展示，"采用"才生效）。
            Text suggestionText = CreateText(canvasGo.transform, "SuggestionText", "输入标题或描述后给出建议", font, 18, TextAnchor.MiddleLeft);
            suggestionText.color = new Color(0.95f, 0.85f, 0.55f, 1f);
            Place(suggestionText.rectTransform, new Vector2(420f, 450f), new Vector2(560f, 28f));
            Button adoptButton = CreateButton(canvasGo.transform, "AdoptSuggestionButton", "采用", font);
            Place(adoptButton.GetComponent<RectTransform>(), new Vector2(1010f, 450f), new Vector2(120f, 28f));
            adoptButton.interactable = false; // 无建议时禁用，由 SubmitPanel 按评估结果开关。

            // 提交按钮。
            Button submitButton = CreateButton(canvasGo.transform, "SubmitButton", "提交", font);
            Place(submitButton.GetComponent<RectTransform>(), new Vector2(420f, 410f), new Vector2(180f, 52f));

            // 结果提示。
            Text resultText = CreateText(canvasGo.transform, "ResultText", string.Empty, font, 24, TextAnchor.MiddleLeft);
            Place(resultText.rectTransform, new Vector2(100f, 340f), new Vector2(880f, 40f));

            // 输入回显：与输入框本身的渲染互相独立，用于证明键盘输入确实进入了 InputField。
            Text echoText = CreateText(canvasGo.transform, "EchoText", string.Empty, font, 18, TextAnchor.MiddleLeft);
            echoText.color = new Color(0.75f, 0.85f, 1f, 1f);
            Place(echoText.rectTransform, new Vector2(100f, 380f), new Vector2(880f, 28f));
            titleInput.onValueChanged.AddListener(v => { echoText.text = "输入回显: [" + v + "]"; });
            descInput.onValueChanged.AddListener(v => { echoText.text = "输入回显: [" + v + "]"; });

            // 字段绑定（代码内完成，不依赖 Inspector 拖拽）。
            panel.titleInput = titleInput;
            panel.descInput = descInput;
            panel.submitButton = submitButton;
            panel.resultText = resultText;
            panel.suggestionText = suggestionText;
            panel.adoptButton = adoptButton;
            submitButton.onClick.AddListener(panel.OnClickSubmit);
            adoptButton.onClick.AddListener(panel.OnAdoptSuggestion);
            titleInput.onValueChanged.AddListener(v => panel.OnInputChanged());
            descInput.onValueChanged.AddListener(v => panel.OnInputChanged());

            // 点击放置器：手指点平面放标记。
            GameObject placerGo = new GameObject("PlaneTapPlacer");
            placerGo.AddComponent<PlaneTapPlacer>();

            Debug.Log("[SubmitUIBootstrap] 提交界面已创建（AR 点击放置标记阶段）");
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
            text.fontSize = 20;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            StretchAsym(text.rectTransform, 12f, 6f);

            GameObject placeholderGo = new GameObject("Placeholder");
            placeholderGo.transform.SetParent(go.transform, false);
            Text placeholderText = placeholderGo.AddComponent<Text>();
            placeholderText.font = font;
            placeholderText.fontSize = 20;
            placeholderText.fontStyle = FontStyle.Italic;
            placeholderText.color = new Color(1f, 1f, 1f, 0.35f);
            placeholderText.alignment = TextAnchor.MiddleLeft;
            placeholderText.text = placeholder;
            placeholderText.verticalOverflow = VerticalWrapMode.Overflow;
            StretchAsym(placeholderText.rectTransform, 12f, 6f);

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

        private static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        private static void StretchAsym(RectTransform rect, float paddingX, float paddingY)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(paddingX, paddingY);
            rect.offsetMax = new Vector2(-paddingX, -paddingY);
        }
    }
}
